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

    [HarmonyPatch(typeof(Verb), "IsStillUsableBy")]
    public static class Patch_IsStillUsableBy
    {
        private static void Postfix(ref bool __result, Verb __instance, Pawn pawn)
        {
            if (__result)
            {
                __result = HediffResourceUtils.IsUsableBy(__instance, out string disableReason);
            }
        }
    }

    [HarmonyPatch(typeof(Verb), "Available")]
    public static class Patch_Available
    {
        private static void Postfix(ref bool __result, Verb __instance)
        {
            if (__result)
            {
                __result = HediffResourceUtils.IsUsableBy(__instance, out string disableReason);
            }
        }
    }

    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply")]
    public static class Patch_DamageInfosToApply
    {
        private static void Postfix(ref IEnumerable<DamageInfo> __result, Verb __instance, LocalTargetInfo target)
        {
            Log.Message("Patch_DamageInfosToApply - Postfix - if (__instance.tool is IResourceProps props && props.ChargeSettings != null && __instance.CasterPawn != null) - 1", true);
            if (__instance.tool is IResourceProps props && props.ChargeSettings != null && __instance.CasterPawn != null)
            {
                Log.Message("Patch_DamageInfosToApply - Postfix - var list = __result.ToList(); - 2", true);
                var list = __result.ToList();
                Log.Message("Patch_DamageInfosToApply - Postfix - foreach (var chargeSettings in props.ChargeSettings) - 3", true);
                foreach (var chargeSettings in props.ChargeSettings)
                {
                    Log.Message("chargeSettings: " + chargeSettings.resourcePerCharge);
                    Log.Message("Patch_DamageInfosToApply - Postfix - var hediffResource = __instance.CasterPawn.health.hediffSet.GetFirstHediffOfDef(chargeSettings.hediffResource) as HediffResource; - 4", true);
                    var hediffResource = __instance.CasterPawn.health.hediffSet.GetFirstHediffOfDef(chargeSettings.hediffResource) as HediffResource;
                    Log.Message("Patch_DamageInfosToApply - Postfix - foreach (var damageInfo in list) - 5", true);
                    foreach (var damageInfo in list)
                    {
                        var chargeResources = new ChargeResources();
                        chargeResources.chargeResources = new List<ChargeResource> { new ChargeResource(hediffResource.ResourceAmount, chargeSettings) };
                        Log.Message("Patch_DamageInfosToApply - Postfix - var chargeResources = new ChargeResources(); - 6", true);
                        Log.Message("Patch_DamageInfosToApply - Postfix - chargeResources.chargeResources = new List<ChargeResource> { new ChargeResource(hediffResource.ResourceAmount, chargeSettings) }; - 7", true);
                        Log.Message("Patch_DamageInfosToApply - Postfix - var amount = damageInfo.Amount; - 8", true);
                        var amount = damageInfo.Amount;
                        Log.Message("Patch_DamageInfosToApply - Postfix - HediffResourceUtils.ApplyChargeResource(ref amount, chargeResources); - 9", true);
                        HediffResourceUtils.ApplyChargeResource(ref amount, chargeResources);
                        Log.Message("Patch_DamageInfosToApply - Postfix - if (amount != damageInfo.Amount) - 10", true);
                        if (amount != damageInfo.Amount)
                        {
                            Log.Message("Patch_DamageInfosToApply - Postfix - damageInfo.SetAmount(amount); - 11", true);
                            damageInfo.SetAmount(amount);
                            hediffResource.ResourceAmount = 0;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Patch_TryCastShot
    {
        public static Verb verbSource;
        private static void Prefix(Verb __instance)
        {
            verbSource = __instance;
        }
        private static void Postfix(Verb __instance)
        {
            verbSource = null;
        }
    }

    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_TryCastNextBurstShot
    {
        private static void Prefix(Verb __instance, out bool __state)
        {
            if (__instance.Available())
            {
                __state = true;
            }
            else
            {
                __state = false;
            }
        }

        private static void Postfix(Verb __instance, bool __state)
        {
            Log.Message(" - Postfix - if (__state && __instance.CasterIsPawn) - 1", true);
            if (__state && __instance.CasterIsPawn)
            {
                Log.Message(" - Postfix - if (__instance.verbProps is IResourceProps verbProps) - 2", true);
                if (__instance.verbProps is IResourceProps verbProps)
                {
                    Log.Message(" - Postfix - HediffResourceUtils.ApplyResourceSettings(__instance, verbProps); - 3", true);
                    HediffResourceUtils.ApplyResourceSettings(__instance, verbProps);
                }
                else if (__instance.tool is IResourceProps toolProps)
                {
                    Log.Message(" - Postfix - HediffResourceUtils.ApplyResourceSettings(__instance, toolProps); - 5", true);
                    HediffResourceUtils.ApplyResourceSettings(__instance, toolProps);
                }
            }
        }
    }
}