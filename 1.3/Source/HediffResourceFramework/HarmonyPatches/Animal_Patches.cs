using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using VFECore;

namespace HediffResourceFramework
{
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), "Interacted")]
    public static class Patch_Interacted
    {
        public static void Postfix(Pawn initiator, Pawn recipient)
        {
            if (initiator != null && recipient != null)
            {
                var comp = recipient.TryGetComp<CompThingInUse>();
                if (comp != null)
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (useProps.resourceOnTaming != 0)
                        {
                            HediffResourceUtils.AdjustResourceAmount(initiator, useProps.hediff, useProps.resourceOnTaming, useProps.addHediffIfMissing, null);
                        }
                    }
                }
            }
        }
    }


    [HarmonyPatch(typeof(WorkGiver_Tame), "JobOnThing")]
    public static class Patch_WorkGiver_Tame_JobOnThing
    {
        public static bool Prefix(ref Job __result, WorkGiver_Tame __instance, Pawn pawn, Thing t, bool forced = false)
        {
            var pawn2 = t as Pawn;
            if (pawn2 != null)
            {
                var comp = pawn2.GetComp<CompThingInUse>();
                if (comp != null)
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (!pawn.CanUseIt(pawn2.Label, useProps, useProps.resourceOnTaming, useProps.cannotTameMessageKey, out var failMessage))
                        {
                            JobFailReason.Is(failMessage);
                            __result = null;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractWith")]
    public static class TryInteractWith_Patch
    {
        public static void Postfix(bool __result, Pawn ___pawn, Pawn recipient, InteractionDef intDef)
        {
            if (__result && intDef == InteractionDefOf.TrainAttempt)
            {
                var comp = recipient.TryGetComp<CompThingInUse>();
                if (comp != null)
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (useProps.resourceOnTraining != 0)
                        {
                            HediffResourceUtils.AdjustResourceAmount(___pawn, useProps.hediff, useProps.resourceOnTraining, useProps.addHediffIfMissing, null);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_Train), "JobOnThing")]
    public static class Patch_WorkGiver_Train_JobOnThing
    {
        public static bool Prefix(ref Job __result, WorkGiver_Tame __instance, Pawn pawn, Thing t, bool forced = false)
        {
            var pawn2 = t as Pawn;
            if (pawn2 != null)
            {
                var comp = pawn2.GetComp<CompThingInUse>();
                if (comp != null)
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (!pawn.CanUseIt(pawn2.Label, useProps, useProps.resourceOnTraining, useProps.cannotTrainMessageKey, out var failMessage))
                        {
                            JobFailReason.Is(failMessage);
                            __result = null;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(CompHasGatherableBodyResource), "Gathered")]
    public static class Gathered_Patch
    {
        public static void Postfix(CompHasGatherableBodyResource __instance, Pawn doer)
        {
            var pawnShearing = __instance.parent as Pawn;
            if (pawnShearing != null)
            {
                var comp = pawnShearing.TryGetComp<CompThingInUse>();
                if (comp != null)
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (useProps.resourceOnGather != 0)
                        {
                            HediffResourceUtils.AdjustResourceAmount(doer, useProps.hediff, useProps.resourceOnGather, useProps.addHediffIfMissing, null);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_GatherAnimalBodyResources), "HasJobOnThing")]
    public static class Patch_WorkGiver_GatherAnimalBodyResources_HasJobOnThing
    {
        public static bool Prefix(ref bool __result, WorkGiver_Tame __instance, Pawn pawn, Thing t, bool forced = false)
        {
            var pawn2 = t as Pawn;
            if (pawn2 != null)
            {
                var comp = pawn2.GetComp<CompThingInUse>();
                if (comp != null)
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (!pawn.CanUseIt(pawn2.Label, useProps, useProps.resourceOnGather, useProps.cannotGatherMessageKey, out var failMessage))
                        {
                            JobFailReason.Is(failMessage);
                            __result = false;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
