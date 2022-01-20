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
	[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal", new Type[]
	{
		typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(RotDrawMode), typeof(PawnRenderFlags)
	})]
	public static class Patch_RenderPawnInternal
	{
		public static void Postfix(Pawn ___pawn)
		{
			foreach (var hediff in HediffResourceUtils.GetHediffResourcesFor(___pawn))
			{
				hediff.Draw();
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
				HediffResourceManager.Instance.RegisterAndRecheckForPolicies(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
	public static class Patch_SpawnSetup
	{
		private static void Postfix(Pawn __instance)
		{
			if (__instance?.Faction == Faction.OfPlayer && __instance.RaceProps.Humanlike)
			{
				HediffResourceManager.Instance.RegisterAndRecheckForPolicies(__instance);
			}
			if (__instance.skills?.skills != null)
            {
				foreach (var skill in __instance.skills.skills)
				{
					HediffResourceUtils.TryAssignNewSkillRelatedHediffs(skill, __instance);
				}
			}
		}
	}

	[HarmonyPatch(typeof(SkillRecord), "Learn")]
	public static class Patch_Learn
	{
		private static void Postfix(SkillRecord __instance, Pawn ___pawn)
		{
			HediffResourceUtils.TryAssignNewSkillRelatedHediffs(__instance, ___pawn);
		}
	}
}
