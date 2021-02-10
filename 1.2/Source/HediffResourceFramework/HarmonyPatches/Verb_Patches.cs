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
                if (verbProps != null && verbProps.resourceSettings != null)
                {
                    var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>();

                    var verbPostUseDelay = new List<int>();
                    var verbPostUseDelayMultipliers = new List<float>();

                    var hediffPostUse = new Dictionary<HediffResource, List<int>>();
                    var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>();

                    var disablePostUseString = "";
                    var comps = GetAllWeaponAndApparelAdjustHediffsComps(__instance.CasterPawn);

                    foreach (var option in verbProps.resourceSettings)
                    {
                        var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                        foreach (var comp in comps)
                        {
                            var compResourseSettings = comp.Props.resourceSettings?.FirstOrDefault(x => x.hediff == option.hediff);
                            if (compResourseSettings != null)
                            {
                                if (option.postUseDelay != 0)
                                {
                                    verbPostUseDelay.Add(option.postUseDelay);
                                    disablePostUseString += comp.Props.disablePostUse + "\n";
                                    if (compResourseSettings.postUseDelayMultiplier != 1)
                                    {
                                        verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier);
                                    }
                                }
                                Log.Message($"Adding postUseDelayMultiplier from comp {comp}: {compResourseSettings.postUseDelayMultiplier}");
                                Log.Message("Adding postUseDelay: " + option.postUseDelay);
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
                            if (comp.postUseDelayTicks is null)
                            {
                                comp.postUseDelayTicks = new Dictionary<Verb, VerbDisable>();
                            }
                            comp.postUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average()) * verbPostUseDelayMultipliers.Average()), disablePostUseString);
                            Log.Message($"Adding delay {comp.postUseDelayTicks[__instance]} {__instance}");
                        }
                    }
                    else if (verbPostUseDelay.Any())
                    {
                        foreach (var comp in comps)
                        {
                            if (comp.postUseDelayTicks is null)
                            {
                                comp.postUseDelayTicks = new Dictionary<Verb, VerbDisable>();
                            }
                            comp.postUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average())), disablePostUseString);
                            Log.Message($"Adding delay {comp.postUseDelayTicks[__instance]} {__instance}");
                        }
                    }
                    else
                    {
                        Log.Message($"Can't add new delay for verb: {__instance} due to missing verbPostUseDelay values");

                    }
                    foreach (var hediffData in hediffPostUse)
                    {
                        if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList))
                        {
                            int newDelayTicks;
                            if (hediffPostUseDelayMultipliers.TryGetValue(hediffData.Key, out List<float> hediffPostUseMultipliers) && hediffPostUseMultipliers.Any())
                            {
                                newDelayTicks = (int)(hediffPostUseList.Average() * hediffPostUseMultipliers.Average());
                                Log.Message($"newDelayTicks: {newDelayTicks}, with multipliers for {hediffData.Key}");
                            }
                            else
                            {
                                newDelayTicks = (int)(hediffPostUseList.Average());
                                Log.Message($"newDelayTicks: {newDelayTicks} for {hediffData.Key}");
                            }
                            if (hediffData.Key.CanHaveDelay(newDelayTicks))
                            {
                                hediffData.Key.AddDelay(newDelayTicks);
                                Log.Message($"Adding delay {newDelayTicks} for {hediffData.Key}");

                            }
                            else
                            {
                                Log.Message($"{hediffData.Key} can't have new delay in {newDelayTicks}");
                            }
                        }
                    }
                }
            }
        }

        public static List<CompAdjustHediffs> GetAllWeaponAndApparelAdjustHediffsComps(Pawn pawn)
        {
            List<CompAdjustHediffs> compAdjustHediffs = new List<CompAdjustHediffs>();
            var apparels = pawn.apparel?.WornApparel?.ToList();
            if (apparels != null)
            {
                foreach (var apparel in apparels)
                {
                    var comp = apparel.GetComp<CompApparelAdjustHediffs>();
                    if (comp != null)
                    {
                        compAdjustHediffs.Add(comp);
                    }
                }
            }

            var equipments = pawn.equipment?.AllEquipmentListForReading;
            if (equipments != null)
            {
                foreach (var equipment in equipments)
                {
                    var comp = equipment.GetComp<CompWeaponAdjustHediffs>();
                    if (comp != null)
                    {
                        compAdjustHediffs.Add(comp);
                    }
                }
            }
            return compAdjustHediffs;
        }
    }
}