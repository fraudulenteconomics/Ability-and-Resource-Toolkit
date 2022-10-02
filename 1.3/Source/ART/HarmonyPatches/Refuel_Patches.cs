using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ART
{

    [HarmonyPatch(typeof(RefuelWorkGiverUtility), "CanRefuel")]
    public static class Patch_CanRefuel
    {
        private static bool Prefix(ref bool __result, Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn.HasEnoughResourceToRefuel(t, out _))
            {
                ARTLog.Message("Has enough resource to refuel");
                __result = CanRefuel(pawn, t, forced);
                return false;
            }
            return true;
        }

        public static bool CanRefuel(Pawn pawn, Thing t, bool forced = false)
        {
            var compRefuelable = t.TryGetComp<CompRefuelable>();
            if (compRefuelable == null || compRefuelable.IsFull || (!forced && !compRefuelable.allowAutoRefuel))
            {
                return false;
            }
            if (compRefuelable.FuelPercentOfMax > 0f && !compRefuelable.Props.allowRefuelIfNotEmpty)
            {
                return false;
            }
            if (!forced && !compRefuelable.ShouldAutoRefuelNow)
            {
                return false;
            }
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            return true;
        }

        public static bool HasEnoughResourceToRefuel(this Pawn refueler, Thing refuelable, out (HediffResource, ThingDefCountClassFloat) toConsume)
        {
            var compRefuelable = refuelable.TryGetComp<CompRefuelable>();
            int fuelCountToFullyRefuel = compRefuelable.GetFuelCountToFullyRefuel();
            var filter = compRefuelable.Props.fuelFilter;
            foreach (var hediff in refueler.health.hediffSet.hediffs)
            {
                if (hediff is HediffResource hediffResource && hediffResource.CurStage is HediffStageResource hediffStageResource && hediffStageResource.refuelProperties != null)
                {
                    foreach (var li in hediffStageResource.refuelProperties)
                    {
                        hediffResource = li.hediffResource != null
                            ? refueler.health.hediffSet.GetFirstHediffOfDef(li.hediffResource) as HediffResource
                            : hediffResource;

                        if (hediffResource != null)
                        {
                            foreach (var resource in li.resourcesPerFuelUnit)
                            {
                                if (filter.Allows(resource.thingDef))
                                {
                                    bool canFuel = hediffResource.ResourceAmount >= resource.rate * fuelCountToFullyRefuel;
                                    if (canFuel)
                                    {
                                        toConsume = (hediffResource, resource);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            toConsume = default;
            return false;
        }
    }

    [HarmonyPatch(typeof(RefuelWorkGiverUtility), "RefuelJob")]
    public static class Patch_RefuelJob
    {
        private static bool Prefix(ref Job __result, Pawn pawn, Thing t, bool forced = false, JobDef customRefuelJob = null, JobDef customAtomicRefuelJob = null)
        {
            if (pawn.HasEnoughResourceToRefuel(t, out _))
            {
                ARTLog.Message("Has enough resource to refuel");
                __result = RefuelJob(pawn, t, forced, customRefuelJob, customAtomicRefuelJob);
                return false;
            }
            return true;
        }

        public static Job RefuelJob(Pawn pawn, Thing t, bool forced = false, JobDef customRefuelJob = null, JobDef customAtomicRefuelJob = null)
        {
            if (!t.TryGetComp<CompRefuelable>().Props.atomicFueling)
            {
                return JobMaker.MakeJob(customRefuelJob ?? JobDefOf.Refuel, t);
            }
            var job = JobMaker.MakeJob(customAtomicRefuelJob ?? JobDefOf.RefuelAtomic, t);
            return job;
        }
    }

    [HarmonyPatch(typeof(JobDriver_Refuel), "TryMakePreToilReservations")]
    public static class Patch_JobDriver_Refuel_TryMakePreToilReservations
    {
        private static bool Prefix(ref bool __result, JobDriver_Refuel __instance, bool errorOnFailed)
        {
            if (__instance.pawn.HasEnoughResourceToRefuel(__instance.job.targetA.Thing, out _))
            {
                ARTLog.Message("Has enough resource to refuel");
                __result = __instance.pawn.Reserve(__instance.job.GetTarget(TargetIndex.A).Thing, __instance.job, 1, -1, null, errorOnFailed);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(JobDriver_Refuel), "MakeNewToils")]
    public static class Patch_JobDriver_Refuel_MakeNewToils
    {
        private static bool Prefix(ref IEnumerable<Toil> __result, JobDriver_Refuel __instance)
        {
            if (__instance.pawn.HasEnoughResourceToRefuel(__instance.job.targetA.Thing, out _))
            {
                ARTLog.Message("Has enough resource to refuel");
                __result = MakeNewToils(__instance).ToList();
                return false;
            }
            return true;
        }

        public static IEnumerable<Toil> MakeNewToils(JobDriver_Refuel __instance)
        {
            var refuelableComp = __instance.job.targetA.Thing.TryGetComp<CompRefuelable>();
            __instance.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            __instance.AddEndCondition(() => (!refuelableComp.IsFull) ? JobCondition.Ongoing : JobCondition.Succeeded);
            __instance.AddFailCondition(() => !__instance.job.playerForced && !refuelableComp.ShouldAutoRefuelNowIgnoringFuelPct);
            __instance.AddFailCondition(() => !refuelableComp.allowAutoRefuel && !__instance.job.playerForced);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(240).FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A);
            yield return FinalizeRefueling(TargetIndex.A);
        }

        public static Toil FinalizeRefueling(TargetIndex refuelableInd)
        {
            var toil = new Toil();
            toil.initAction = delegate
            {
                var curJob = toil.actor.CurJob;
                var thing = curJob.GetTarget(refuelableInd).Thing;
                if (toil.actor.HasEnoughResourceToRefuel(thing, out var refuelData))
                {
                    var compRefuelable = thing.TryGetComp<CompRefuelable>();
                    int toFuel = compRefuelable.GetFuelCountToFullyRefuel();
                    refuelData.Item1.ChangeResourceAmount(-(refuelData.Item2.rate * toFuel));
                    var fuelThing = ThingMaker.MakeThing(refuelData.Item2.thingDef);
                    fuelThing.stackCount = toFuel;
                    compRefuelable.Refuel(new List<Thing> { fuelThing });
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}