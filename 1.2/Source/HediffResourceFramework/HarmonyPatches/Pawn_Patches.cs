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
	[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
	public static class Patch_SpawnSetup
	{
		private static void Postfix(Pawn __instance)
		{
			if (__instance?.Faction == Faction.OfPlayer && __instance.RaceProps.Humanlike)
            {
				HediffResourceUtils.HediffResourceManager.RegisterAndRecheckForPolicies(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(Pawn), "SetFaction")]
	public static class Patch_SetFaction
	{
		private static void Postfix(Pawn __instance)
		{
			if (__instance?.Faction == Faction.OfPlayer && __instance.RaceProps.Humanlike)
			{
				HediffResourceUtils.HediffResourceManager.RegisterAndRecheckForPolicies(__instance);
			}
		}
	}
}
