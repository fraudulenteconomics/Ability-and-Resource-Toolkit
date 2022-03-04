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

namespace ART
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
				ARTManager.Instance.RegisterAndRecheckForPolicies(__instance);
			}
		}
	}
	public class HediffGenerationData
	{
		public HediffDef hediff;

		public float initialSeverity;
	}

	public class PawnKindExtension : DefModExtension
	{
		public List<HediffGenerationData> hediffsOnGeneration;
	}

	[HarmonyPatch(typeof(PawnGenerator))]
	[HarmonyPatch("GeneratePawn", new Type[]
	{
		typeof(PawnGenerationRequest)
	})]
	[StaticConstructorOnStartup]
	public static class PawnGenerator_GeneratePawn
	{
		private static void Postfix(ref Pawn __result)
		{
			if (__result?.Faction == Faction.OfPlayer && __result.RaceProps.Humanlike)
			{
				ARTManager.Instance.RegisterAndRecheckForPolicies(__result);
			}
			if (__result.skills?.skills != null)
			{
				foreach (var skill in __result.skills.skills)
				{
					HediffResourceUtils.TryAssignNewSkillRelatedHediffs(skill, __result);
				}
			}

			var extension = __result.kindDef.GetModExtension<PawnKindExtension>();
			if (extension != null)
			{
				if (extension.hediffsOnGeneration != null)
                {
					foreach (var hediffData in extension.hediffsOnGeneration)
                    {
						if (hediffData.hediff is HediffResourceDef resourceDef)
                        {
							HediffResourceUtils.AdjustResourceAmount(__result, resourceDef, hediffData.initialSeverity, true, null, null);
                        }
						else
                        {
							HealthUtility.AdjustSeverity(__result, hediffData.hediff, hediffData.initialSeverity);
                        }
                    }
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
