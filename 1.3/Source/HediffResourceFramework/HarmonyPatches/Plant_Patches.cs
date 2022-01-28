﻿using HarmonyLib;
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

namespace HediffResourceFramework
{
    [HarmonyPatch(typeof(WorkGiver_GrowerSow), "JobOnCell")]
    public static class Patch_JobOnCell
    {
        public static void Postfix(ref Job __result, WorkGiver_GrowerSow __instance, Pawn pawn, IntVec3 c, bool forced = false)
        {
            if (__result != null)
            {
                var compProperties = __result.plantDefToSow.GetCompProperties<CompProperties_ThingInUse>();
                if (compProperties != null)
                {
                    foreach (var useProps in compProperties.useProperties)
                    {
                        if (!pawn.CanUseIt(__result.plantDefToSow.label, useProps, useProps.resourceOnSow, useProps.cannotSowMessageKey, out var failMessage))
                        {
                            JobFailReason.Is(failMessage);
                            __result = null;
                            return;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(JobDriver_PlantSow), "MakeNewToils")]
    public static class JobDriver_PlantSow_MakeNewToils
    {
        private static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_PlantSow __instance)
        {
            foreach (var toil in __result)
            {
                yield return toil;
            }
            yield return new Toil
            {
                initAction = delegate
                {
                    var plant = __instance.job.targetA.Thing;
                    var comp = plant.TryGetComp<CompThingInUse>();
                    if (comp != null)
                    {
                        foreach (var useProps in comp.Props.useProperties)
                        {
                            if (useProps.resourceOnSow != 0)
                            {
                                HediffResourceUtils.AdjustResourceAmount(__instance.pawn, useProps.hediff, useProps.resourceOnSow, useProps.addHediffIfMissing, null);
                            }
                        }
                    }
                }
            };
        }
    }

    [HarmonyPatch(typeof(WorkGiver_GrowerHarvest), "HasJobOnCell")]
    public static class Patch_WorkGiver_GrowerHarvest_HasJobOnCell
    {
        public static void Postfix(ref bool __result, WorkGiver_GrowerHarvest __instance, Pawn pawn, IntVec3 c, bool forced = false)
        {
            Plant plant = c.GetPlant(pawn.Map);
            if (plant != null)
            {
                var comp = plant.GetComp<CompThingInUse>();
                if (comp != null)
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (!pawn.CanUseIt(plant.Label, useProps, useProps.resourceOnHarvest, useProps.cannotHarvestMessageKey, out var failMessage))
                        {
                            JobFailReason.Is(failMessage);
                            __result = false;
                            return;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Plant), "PlantCollected")]
    public static class Plant_PlantCollected
    {
        private static void Postfix(Plant __instance, Pawn by)
        {
            var comp = __instance.TryGetComp<CompThingInUse>();
            if (comp != null)
            {
                foreach (var useProps in comp.Props.useProperties)
                {
                    if (useProps.resourceOnHarvest != 0)
                    {
                        var resourceOnHarvest = useProps.scaleWithGrowthRate ? useProps.resourceOnHarvest * __instance.Growth : useProps.resourceOnHarvest;
                        HediffResourceUtils.AdjustResourceAmount(by, useProps.hediff, resourceOnHarvest, useProps.addHediffIfMissing, null);
                    }
                }
            }

        }
    }
}
