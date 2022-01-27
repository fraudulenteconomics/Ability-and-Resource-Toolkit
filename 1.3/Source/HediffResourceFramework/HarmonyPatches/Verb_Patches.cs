﻿using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

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
            if (__instance.tool is IResourceProps props && props.ChargeSettings != null && __instance.CasterPawn != null && target.HasThing)
            {
                var list = __result.ToList();
                foreach (var chargeSettings in props.ChargeSettings)
                {
                    var hediffResource = __instance.CasterPawn.health.hediffSet.GetFirstHediffOfDef(chargeSettings.hediffResource) as HediffResource;
                    foreach (var damageInfo in list)
                    {
                        var chargeResources = new ChargeResources();
                        chargeResources.chargeResources = new List<ChargeResource> { new ChargeResource(hediffResource.ResourceAmount, chargeSettings) };
                        var amount = damageInfo.Amount;
                        HediffResourceUtils.ApplyChargeResource(ref amount, chargeResources);
                        if (amount != damageInfo.Amount)
                        {
                            damageInfo.SetAmount(amount);
                            hediffResource.ResourceAmount = 0;
                        }
                    }
                }
            }
            var extension = __instance.EquipmentSource?.def.GetModExtension<ResourceOnActionExtension>();
            if (extension != null)
            {
                foreach (var resourceOnAction in extension.resourcesOnAction)
                {
                    if (resourceOnAction.onSelf)
                    {
                        resourceOnAction.TryApplyOn(__instance.CasterPawn);
                    }
                    else if (target.Pawn != null)
                    {
                        resourceOnAction.TryApplyOn(target.Pawn);
                    }
                }
            }

            var dinfoSource = __result.First();
            if (__instance.caster is Pawn pawn)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    var extension2 = hediff.def.GetModExtension<ResourceOnActionExtension>();
                    if (extension2 != null)
                    {
                        foreach (var resourceOnAction in extension2.resourcesOnAction)
                        {
                            if (resourceOnAction.onSelf)
                            {
                                resourceOnAction.TryApplyOn(__instance.CasterPawn);
                            }
                            else if (target.Pawn != null)
                            {
                                resourceOnAction.TryApplyOn(target.Pawn);
                            }
                        }
                    }

                    if (hediff is HediffResource hediffResource && hediffResource.CurStage is HediffStageResource hediffStageResource && hediffStageResource.additionalDamages != null)
                    {
                        foreach (var additionalDamage in hediffStageResource.additionalDamages)
                        {
                            if (additionalDamage.damageRange)
                            {
                                var damageAmount = additionalDamage.amount.RandomInRange;
                                var damage = new DamageInfo(additionalDamage.damage, damageAmount, instigator: dinfoSource.Instigator, hitPart: dinfoSource.HitPart, weapon: dinfoSource.Weapon);
                                Log.Message("Damaging " + target.Thing + " with " + damage);
                                target.Thing.TakeDamage(damage);
                            }
                        }
                    }
                }
            }

            var stuffExtension = __instance.EquipmentSource?.Stuff?.GetModExtension<StuffExtension>();
            if (stuffExtension != null)
            {
                stuffExtension.DamageThing(__instance.caster, target.Thing, dinfoSource, false, true);
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
            if (__state && __instance.CasterIsPawn)
            {
                if (__instance.verbProps is IResourceProps verbProps)
                {
                    HediffResourceUtils.ApplyResourceSettings(__instance.CurrentTarget.Thing, __instance.CasterPawn, verbProps);
                }
                else if (__instance.tool is IResourceProps toolProps)
                {
                    HediffResourceUtils.ApplyResourceSettings(__instance.CurrentTarget.Thing, __instance.CasterPawn, toolProps);
                }
                var comp = __instance.EquipmentSource?.def.GetModExtension<ResourceOnActionExtension>();
                if (comp != null)
                {
                    foreach (var resourceOnAction in comp.resourcesOnAction)
                    {
                        if (resourceOnAction.onSelf)
                        {
                            resourceOnAction.TryApplyOn(__instance.CasterPawn);
                        }
                    }
                }

                if (__instance.caster is Pawn pawn)
                {
                    foreach (var hediff in pawn.health.hediffSet.hediffs)
                    {
                        var extension2 = hediff.def.GetModExtension<ResourceOnActionExtension>();
                        if (extension2 != null)
                        {
                            foreach (var resourceOnAction in extension2.resourcesOnAction)
                            {
                                if (resourceOnAction.onSelf)
                                {
                                    resourceOnAction.TryApplyOn(__instance.CasterPawn);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}