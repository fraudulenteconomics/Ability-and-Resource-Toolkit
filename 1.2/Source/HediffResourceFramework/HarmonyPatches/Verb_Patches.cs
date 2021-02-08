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
        private static void Postfix(Verb __instance)
        {
            if (__instance.Available() && __instance.CasterIsPawn && __instance.EquipmentSource != null)
            {
                var verbProps = __instance.verbProps as VerbResourceProps;
                if (verbProps != null && verbProps.resourceSettings != null)
                {
                    foreach (var option in verbProps.resourceSettings)
                    {
                        HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                        if (option.postUseDelay != 0)
                        {
                            var comp = __instance.EquipmentSource.GetComp<CompWeaponAdjustHediffs>();

                            if (comp != null)
                            {
                                if (comp.postUseDelayTicks is null)
                                {
                                    comp.postUseDelayTicks = new Dictionary<Verb, VerbDisable>();
                                }
                                comp.postUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + option.postUseDelay) * comp.Props.postUseDelayMultiplier), comp.Props.disableWeaponPostUse);
                            }

                            var hediffResource = __instance.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                            Log.Message($"__instance.EquipmentSource: {__instance.EquipmentSource}, hediffResource: {hediffResource}, option.hediff: {option.hediff}");
                            if (hediffResource != null && hediffResource.CanHaveDelay(option.postUseDelay))
                            {
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffResource.AddDelay(option.postUseDelay); - 16", true);
                                hediffResource.AddDelay(option.postUseDelay);
                            }
                        }
                    }
                }
            }
        }
    }
}
