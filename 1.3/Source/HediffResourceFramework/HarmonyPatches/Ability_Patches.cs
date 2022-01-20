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

    [HarmonyPatch(typeof(VFECore.Abilities.Ability), "IsEnabledForPawn")]
    public static class Patch_IsEnabledForPawn
    {
        private static void Postfix(ref bool __result, VFECore.Abilities.Ability __instance, ref string reason)
        {
            var extension = __instance.def.GetModExtension<AbilityResourceProps>();
            if (extension != null)
            {
                var isUsable = HediffResourceUtils.IsUsableForProps(__instance.pawn, extension, out reason);
                if (!isUsable)
                {
                    __result = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(VFECore.Abilities.Ability), "Cast")]
    public static class Patch_Cast
    {
        private static void Postfix(VFECore.Abilities.Ability __instance, LocalTargetInfo target)
        {
            var extension = __instance.def.GetModExtension<AbilityResourceProps>();
            if (extension != null)
            {
                HediffResourceUtils.ApplyResourceSettings(target.Thing, __instance.pawn, extension);
            }
        }
    }

    [HarmonyPatch(typeof(VFECore.Abilities.Ability), "GetDescriptionForPawn")]
    public static class Patch_GetDescriptionForPawn
    {
        private static void Postfix(VFECore.Abilities.Ability __instance, ref string __result)
        {
            var extension = __instance.def.GetModExtension<AbilityResourceProps>();
            if (extension != null)
            {
                StringBuilder sb = new StringBuilder(__result);
                sb.AppendLine(HediffResourceUtils.GetPropsDescriptions(__instance.pawn, extension));
                __result = sb.ToString().TrimEndNewlines();
            }
        }
    }
}