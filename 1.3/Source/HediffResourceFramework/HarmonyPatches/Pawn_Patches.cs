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
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
	public static class Patch_RenderPawnAt
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

	[HarmonyPatch(typeof(Pawn), "Kill")]
	public static class Patch_Kill
	{
		private static bool Prefix(Pawn __instance)
		{
			if (__instance?.health?.hediffSet?.hediffs != null)
            {
				foreach (var hediff in __instance.health.hediffSet.hediffs)
                {
					if (hediff is HediffResource hediffResource && hediff.CurStage is HediffStageResource hediffStageResource && hediffStageResource.preventDeath)
                    {
						return false;
                    }
                }
            }
			return true;
		}
	}
}
