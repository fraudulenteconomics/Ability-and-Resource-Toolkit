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

namespace HediffResourceFramework
{
    public static class ReservationHelper
    {
        public static bool PawnCanUseIt(Pawn pawn, Thing thing)
        {
            if (CompFacilityInUse.thingBoosters.TryGetValue(thing, out var comp))
            {
                foreach (var statBooster in comp.Props.statBoosters)
                {
                    if (comp.StatBoosterIsEnabled(statBooster) && statBooster.preventUseIfHediffMissing)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(statBooster.hediff) as HediffResource;
                        if (hediffResource is null || !hediffResource.CanApplyStatBooster(statBooster))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ReservationManager), "CanReserve")]
    public static class Patch_CanReserve
    {
        private static void Postfix(ref bool __result, Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            if (__result)
            {
                var thing = target.Thing;
                if (thing != null && !ReservationHelper.PawnCanUseIt(claimant, thing))
                {
                    __result = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ForbidUtility), "IsForbidden", new Type[] { typeof(Thing), typeof(Pawn) })]
    public static class Patch_IsForbidden
    {
        private static void Postfix(ref bool __result, Thing t, Pawn pawn)
        {
            if (!__result)
            {
                if (!ReservationHelper.PawnCanUseIt(pawn, t))
                {
                    __result = true;
                }
            }
        }
    }
}
