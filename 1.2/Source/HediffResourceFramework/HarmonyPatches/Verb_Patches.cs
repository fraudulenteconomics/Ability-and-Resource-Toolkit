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
                var options = __instance.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
                if (options != null)
                {
                    foreach (var option in options.hediffOptions)
                    {
                        if (HediffResourceUtils.VerbMatches(__instance, option))
                        {
                            HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                        }
                        if (option.postUseDelay != 0)
                        {
                            var comp = __instance.EquipmentSource.GetComp<CompWeaponAdjustHediffs>();
                            if (comp != null)
                            {
                                if (comp.postUseDelayTicks is null)
                                {
                                    comp.postUseDelayTicks = new Dictionary<Verb, VerbDisable>();
                                }
                                comp.postUseDelayTicks[__instance] = new VerbDisable(option.postUseDelay, comp.Props.disableWeaponPostUse);
                            }
                        }
                    }
                }
            }
        }
	}
}
