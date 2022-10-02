using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Linq;
using System.Text;
using Verse;
using VFECore.Abilities;

namespace ART
{
    [HotSwappable]
    [HarmonyPatch(typeof(Ability), "IsEnabledForPawn")]
    public static class Patch_IsEnabledForPawn
    {
        private static void Postfix(ref bool __result, Ability __instance, ref string reason)
        {
            var extension = __instance.def.GetModExtension<AbilityResourceProps>();
            if (extension != null && __result)
            {
                bool isUsable = Utils.IsUsableForProps(__instance.pawn, extension, out string reason2);
                if (!isUsable)
                {
                    reason = reason2;
                    __result = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Ability), "Cast", new Type[] { typeof(GlobalTargetInfo[]) })]
    public static class Patch_Cast
    {
        private static void Postfix(Ability __instance, GlobalTargetInfo[] targets)
        {
            var extension = __instance.def.GetModExtension<AbilityResourceProps>();
            if (extension != null)
            {
                var targetThings = targets.Where(x => x.HasThing).Select(x => x.Thing).ToList();
                Utils.ApplyResourceSettings(targetThings, __instance.pawn, extension);
            }
        }
    }

    [HarmonyPatch(typeof(Ability), "GetDescriptionForPawn")]
    public static class Patch_GetDescriptionForPawn
    {
        private static void Postfix(Ability __instance, ref string __result)
        {
            var extension = __instance.def.GetModExtension<AbilityResourceProps>();
            if (extension != null)
            {
                var sb = new StringBuilder(__result);
                sb.AppendLine(Utils.GetPropsDescriptions(__instance.pawn, extension));
                __result = sb.ToString().TrimEndNewlines();
            }
        }
    }
}