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
            if (__state && __instance.CasterIsPawn && __instance.EquipmentSource != null)
            {
                var verbProps = __instance.verbProps as VerbResourceProps;
                if (verbProps != null)
                {
                    if (verbProps.targetResourceSettings != null)
                    {
                        var target = __instance.CurrentTarget.Thing as Pawn;
                        if (target != null)
                        {
                            foreach (var option in verbProps.targetResourceSettings)
                            {
                                if (option.resetLifetimeTicks)
                                {
                                    var targetHediff = target.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                                    if (targetHediff != null)
                                    {
                                        targetHediff.duration = 0;
                                    }
                                }
                            }
                        }

                    }

                    if (verbProps.resourceSettings != null)
                    {
                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>(); - 11", true);
                        var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>();

                        var verbPostUseDelay = new List<int>();
                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - var verbPostUseDelayMultipliers = new List<float>(); - 13", true);
                        var verbPostUseDelayMultipliers = new List<float>();

                        var hediffPostUse = new Dictionary<HediffResource, List<int>>();
                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>(); - 15", true);
                        var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>();

                        var disablePostUseString = "";
                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - var comps = HediffResourceUtils.GetAllAdjustHediffsComps(__instance.CasterPawn); - 17", true);
                        var comps = HediffResourceUtils.GetAllAdjustHediffsComps(__instance.CasterPawn);

                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var option in verbProps.resourceSettings) - 18", true);
                        foreach (var option in verbProps.resourceSettings)
                        {
                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing); - 19", true);
                            var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 20", true);
                            foreach (var comp in comps)
                            {
                                HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff); - 21", true);
                                var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff);
                                HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings != null) - 22", true);
                                if (compResourseSettings != null)
                                {
                                    HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (option.postUseDelay != 0) - 23", true);
                                    if (option.postUseDelay != 0)
                                    {
                                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - verbPostUseDelay.Add(option.postUseDelay); - 24", true);
                                        verbPostUseDelay.Add(option.postUseDelay);
                                        disablePostUseString += comp.DisablePostUse + "\n";
                                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings.postUseDelayMultiplier != 1) - 25", true);
                                        if (compResourseSettings.postUseDelayMultiplier != 1)
                                        {
                                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier); - 26", true);
                                            verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                    }
                                }

                                HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffResource != null && option.postUseDelay != 0) - 27", true);
                                if (hediffResource != null && option.postUseDelay != 0)
                                {
                                    HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUse.ContainsKey(hediffResource)) - 28", true);
                                    if (hediffPostUse.ContainsKey(hediffResource))
                                    {
                                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUse[hediffResource].Add(option.postUseDelay); - 29", true);
                                        hediffPostUse[hediffResource].Add(option.postUseDelay);
                                    }
                                    else
                                    {
                                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUse[hediffResource] = new List<int> { option.postUseDelay }; - 30", true);
                                        hediffPostUse[hediffResource] = new List<int> { option.postUseDelay };
                                    }
                                    HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1) - 31", true);
                                    if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1)
                                    {
                                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource)) - 32", true);
                                        if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource))
                                        {
                                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier); - 33", true);
                                            hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                        else
                                        {
                                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier }; - 34", true);
                                            hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier };
                                        }
                                    }
                                }
                            }
                        }

                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any()) - 35", true);
                        if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any())
                        {
                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 36", true);
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average()) * verbPostUseDelayMultipliers.Average()), disablePostUseString);
                            }
                        }
                        else if (verbPostUseDelay.Any())
                        {
                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 39", true);
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average())), disablePostUseString);
                            }
                        }
                        HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var hediffData in hediffPostUse) - 41", true);
                        foreach (var hediffData in hediffPostUse)
                        {
                            HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList)) - 42", true);
                            if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList))
                            {
                                int newDelayTicks;
                                HRFLog.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUseDelayMultipliers.TryGetValue(hediffData.Key, out List<float> hediffPostUseMultipliers) && hediffPostUseMultipliers.Any()) - 44", true);
                                if (hediffPostUseDelayMultipliers.TryGetValue(hediffData.Key, out List<float> hediffPostUseMultipliers) && hediffPostUseMultipliers.Any())
                                {
                                    newDelayTicks = (int)(hediffPostUseList.Average() * hediffPostUseMultipliers.Average());
                                }
                                else
                                {
                                    newDelayTicks = (int)(hediffPostUseList.Average());
                                }
                                if (hediffData.Key.CanHaveDelay(newDelayTicks))
                                {
                                    hediffData.Key.AddDelay(newDelayTicks);
                                }
                            }
                        }
                    }
                } 
            }
        }
    }
}