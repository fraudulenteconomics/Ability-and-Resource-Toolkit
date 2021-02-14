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
            Log.Message("Patch_TryCastNextBurstShot - Prefix - if (__instance.Available()) - 1", true);
            if (__instance.Available())
            {
                Log.Message("Patch_TryCastNextBurstShot - Prefix - __state = true; - 2", true);
                __state = true;
            }
            else
            {
                Log.Message("Patch_TryCastNextBurstShot - Prefix - __state = false; - 3", true);
                __state = false;
            }
        }

        private static void Postfix(Verb __instance, bool __state)
        {
            if (__state && __instance.CasterIsPawn)
            {
                Log.Message("Patch_TryCastNextBurstShot - Postfix - var verbProps = __instance.verbProps as VerbResourceProps; - 5", true);
                var verbProps = __instance.verbProps as VerbResourceProps;
                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (verbProps != null) - 6", true);
                if (verbProps != null)
                {
                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (verbProps.targetResourceSettings != null) - 7", true);
                    if (verbProps.targetResourceSettings != null)
                    {
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var target = __instance.CurrentTarget.Thing as Pawn; - 8", true);
                        var target = __instance.CurrentTarget.Thing as Pawn;
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - if (target != null) - 9", true);
                        if (target != null)
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var option in verbProps.targetResourceSettings) - 10", true);
                            foreach (var option in verbProps.targetResourceSettings)
                            {
                                if (option.resetLifetimeTicks)
                                {
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - var targetHediff = target.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource; - 12", true);
                                    var targetHediff = target.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (targetHediff != null) - 13", true);
                                    if (targetHediff != null)
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - targetHediff.duration = 0; - 14", true);
                                        targetHediff.duration = 0;
                                    }
                                }
                            }
                        }

                    }

                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (verbProps.resourceSettings != null) - 15", true);
                    if (verbProps.resourceSettings != null)
                    {
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>(); - 16", true);
                        var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>();

                        var verbPostUseDelay = new List<int>();
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var verbPostUseDelayMultipliers = new List<float>(); - 18", true);
                        var verbPostUseDelayMultipliers = new List<float>();

                        var hediffPostUse = new Dictionary<HediffResource, List<int>>();
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>(); - 20", true);
                        var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>();

                        var disablePostUseString = "";
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var comps = HediffResourceUtils.GetAllAdjustHediffsComps(__instance.CasterPawn); - 22", true);
                        var comps = HediffResourceUtils.GetAllAdjustHediffsComps(__instance.CasterPawn);

                        Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var option in verbProps.resourceSettings) - 23", true);
                        foreach (var option in verbProps.resourceSettings)
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing); - 24", true);
                            var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 25", true);
                            foreach (var comp in comps)
                            {
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff); - 26", true);
                                var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff);
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings != null) - 27", true);
                                if (compResourseSettings != null)
                                {
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (option.postUseDelay != 0) - 28", true);
                                    if (option.postUseDelay != 0)
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - verbPostUseDelay.Add(option.postUseDelay); - 29", true);
                                        verbPostUseDelay.Add(option.postUseDelay);
                                        disablePostUseString += comp.DisablePostUse + "\n";
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings.postUseDelayMultiplier != 1) - 30", true);
                                        if (compResourseSettings.postUseDelayMultiplier != 1)
                                        {
                                            Log.Message("Patch_TryCastNextBurstShot - Postfix - verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier); - 31", true);
                                            verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                    }
                                }

                                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffResource != null && option.postUseDelay != 0) - 32", true);
                                if (hediffResource != null && option.postUseDelay != 0)
                                {
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUse.ContainsKey(hediffResource)) - 33", true);
                                    if (hediffPostUse.ContainsKey(hediffResource))
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUse[hediffResource].Add(option.postUseDelay); - 34", true);
                                        hediffPostUse[hediffResource].Add(option.postUseDelay);
                                    }
                                    else
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUse[hediffResource] = new List<int> { option.postUseDelay }; - 35", true);
                                        hediffPostUse[hediffResource] = new List<int> { option.postUseDelay };
                                    }
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1) - 36", true);
                                    if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1)
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource)) - 37", true);
                                        if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource))
                                        {
                                            Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier); - 38", true);
                                            hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                        else
                                        {
                                            Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier }; - 39", true);
                                            hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier };
                                        }
                                    }
                                }
                            }
                        }

                        Log.Message("Patch_TryCastNextBurstShot - Postfix - if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any()) - 40", true);
                        if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any())
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 41", true);
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average()) * verbPostUseDelayMultipliers.Average()), disablePostUseString);
                            }
                        }
                        else if (verbPostUseDelay.Any())
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 44", true);
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average())), disablePostUseString);
                            }
                        }
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var hediffData in hediffPostUse) - 46", true);
                        foreach (var hediffData in hediffPostUse)
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList)) - 47", true);
                            if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList))
                            {
                                int newDelayTicks;
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUseDelayMultipliers.TryGetValue(hediffData.Key, out List<float> hediffPostUseMultipliers) && hediffPostUseMultipliers.Any()) - 49", true);
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