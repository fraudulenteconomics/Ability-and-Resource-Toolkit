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
                        var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>();

                        var verbPostUseDelay = new List<int>();
                        var verbPostUseDelayMultipliers = new List<float>();

                        var hediffPostUse = new Dictionary<HediffResource, List<int>>();
                        var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>();

                        var disablePostUseString = "";
                        var comps = HediffResourceUtils.GetAllAdjustHediffsComps(__instance.CasterPawn);

                        foreach (var option in verbProps.resourceSettings)
                        {
                            var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                            foreach (var comp in comps)
                            {
                                var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff);
                                if (compResourseSettings != null)
                                {
                                    if (option.postUseDelay != 0)
                                    {
                                        verbPostUseDelay.Add(option.postUseDelay);
                                        disablePostUseString += comp.DisablePostUse + "\n";
                                        if (compResourseSettings.postUseDelayMultiplier != 1)
                                        {
                                            verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                    }
                                }

                                if (hediffResource != null && option.postUseDelay != 0)
                                {
                                    if (hediffPostUse.ContainsKey(hediffResource))
                                    {
                                        hediffPostUse[hediffResource].Add(option.postUseDelay);
                                    }
                                    else
                                    {
                                        hediffPostUse[hediffResource] = new List<int> { option.postUseDelay };
                                    }
                                    if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1)
                                    {
                                        if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource))
                                        {
                                            hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier);
                                        }
                                        else
                                        {
                                            hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier };
                                        }
                                    }
                                }
                            }
                        }

                        if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any())
                        {
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average()) * verbPostUseDelayMultipliers.Average()), disablePostUseString);
                            }
                        }
                        else if (verbPostUseDelay.Any())
                        {
                            foreach (var comp in comps)
                            {
                                comp.PostUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average())), disablePostUseString);
                            }
                        }
                        foreach (var hediffData in hediffPostUse)
                        {
                            if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList))
                            {
                                int newDelayTicks;
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