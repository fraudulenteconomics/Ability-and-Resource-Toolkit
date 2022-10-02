using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace ART
{

    [HarmonyPatch(typeof(ReservationManager), "CanReserve")]
    public static class Patch_CanReserve
    {
        private static void Postfix(ref bool __result, Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
        {
            if (__result)
            {
                var thing = target.Thing;
                if (thing != null && !claimant.CanUseIt(thing, out string failReason))
                {
                    JobFailReason.Is(failReason);
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
                if (!pawn.CanUseIt(t, out string failReason))
                {
                    JobFailReason.Is(failReason);
                    __result = true;
                }
            }
        }
    }
}
