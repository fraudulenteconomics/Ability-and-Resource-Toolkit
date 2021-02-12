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
            Log.Message("Patch_IsStillUsableBy - Postfix - if (__result) - 1", true);
            if (__result)
            {
                Log.Message("Patch_IsStillUsableBy - Postfix - __result = HediffResourceUtils.IsUsableBy(__instance, out string disableReason); - 2", true);
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
                        foreach (var option in verbProps.targetResourceSettings)
                        {
                            if (option.extendLifetime != -1 && target != null)
                            {
                                var targetHediff = target.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                                if (targetHediff != null)
                                {
                                    targetHediff.LifetimeDuration -= option.extendLifetime;
                                }
                            }
                        }
                    }

                    if (verbProps.resourceSettings != null)
                    {
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>(); - 11", true);
                        var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>();

                        var verbPostUseDelay = new List<int>();
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var verbPostUseDelayMultipliers = new List<float>(); - 13", true);
                        var verbPostUseDelayMultipliers = new List<float>();

                        var hediffPostUse = new Dictionary<HediffResource, List<int>>();
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>(); - 15", true);
                        var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>();

                        var disablePostUseString = "";
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var comps = HediffResourceUtils.GetAllAdjustHediffsComps(__instance.CasterPawn); - 17", true);
                        var comps = HediffResourceUtils.GetAllAdjustHediffsComps(__instance.CasterPawn);

                        Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var option in verbProps.resourceSettings) - 18", true);
                        foreach (var option in verbProps.resourceSettings)
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing); - 19", true);
                            var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 20", true);
                            foreach (var comp in comps)
                            {
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff); - 21", true);
                                var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff);
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings != null) - 22", true);
                                if (compResourseSettings != null)
                                {
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (option.postUseDelay != 0) - 23", true);
                                    if (option.postUseDelay != 0)
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - verbPostUseDelay.Add(option.postUseDelay); - 24", true);
                                        verbPostUseDelay.Add(option.postUseDelay);
                                        disablePostUseString += comp.DisablePostUse + "\n";
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings.postUseDelayMultiplier != 1) - 25", true);
                                        if (compResourseSettings.postUseDelayMultiplier != 1)
                                        {
                                            Log.Message("Patch_TryCastNextBurstShot - Postfix - verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier); - 26", true);
                                            verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                    }
                                }

                                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffResource != null && option.postUseDelay != 0) - 27", true);
                                if (hediffResource != null && option.postUseDelay != 0)
                                {
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUse.ContainsKey(hediffResource)) - 28", true);
                                    if (hediffPostUse.ContainsKey(hediffResource))
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUse[hediffResource].Add(option.postUseDelay); - 29", true);
                                        hediffPostUse[hediffResource].Add(option.postUseDelay);
                                    }
                                    else
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUse[hediffResource] = new List<int> { option.postUseDelay }; - 30", true);
                                        hediffPostUse[hediffResource] = new List<int> { option.postUseDelay };
                                    }
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1) - 31", true);
                                    if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1)
                                    {
                                        Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource)) - 32", true);
                                        if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource))
                                        {
                                            Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier); - 33", true);
                                            hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                        else
                                        {
                                            Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier }; - 34", true);
                                            hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier };
                                        }
                                    }
                                }
                            }
                        }

                        Log.Message("Patch_TryCastNextBurstShot - Postfix - if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any()) - 35", true);
                        if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any())
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 36", true);
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average()) * verbPostUseDelayMultipliers.Average()), disablePostUseString);
                            }
                        }
                        else if (verbPostUseDelay.Any())
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var comp in comps) - 39", true);
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average())), disablePostUseString);
                            }
                        }
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var hediffData in hediffPostUse) - 41", true);
                        foreach (var hediffData in hediffPostUse)
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList)) - 42", true);
                            if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList))
                            {
                                int newDelayTicks;
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffPostUseDelayMultipliers.TryGetValue(hediffData.Key, out List<float> hediffPostUseMultipliers) && hediffPostUseMultipliers.Any()) - 44", true);
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