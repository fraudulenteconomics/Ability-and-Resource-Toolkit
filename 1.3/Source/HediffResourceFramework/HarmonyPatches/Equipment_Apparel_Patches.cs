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

	[HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
	public static class Patch_TryDropEquipment
	{
		private static void Prefix(ThingWithComps eq)
		{
			var comp = eq.TryGetComp<CompWeaponAdjustHediffs>();
			if (comp != null)
            {
				comp.Notify_Removed();
            }
		}
	}

	[HarmonyPatch(typeof(Pawn_ApparelTracker), "TryDrop", 
		new Type[] { typeof(Apparel), typeof(Apparel), typeof(IntVec3), typeof(bool)}, 
		new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
	public static class Patch_TryDrop
	{
		private static void Prefix(Apparel ap)
		{
			var comp = ap.TryGetComp<CompApparelAdjustHediffs>();
			if (comp != null)
			{
				comp.Notify_Removed();
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain")]
	public static class Patch_ApparelScoreGain
	{
		private static bool Prefix(ref float __result, Pawn pawn, Apparel ap, List<float> wornScoresCache)
		{
			if (!HediffResourceUtils.CanWear(pawn, ap, out string tmp))
            {
				__result = -1000f;
				return false;
			}
			return true;
		}
	}
}
