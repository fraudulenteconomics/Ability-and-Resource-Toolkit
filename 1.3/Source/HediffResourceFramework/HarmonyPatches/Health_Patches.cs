using HarmonyLib;
using System;
using System.Linq;
using Verse;

namespace HediffResourceFramework
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
	public static class Patch_ShouldBeDowned
	{
		private static bool Prefix(Pawn ___pawn)
		{
			if (___pawn?.health?.hediffSet?.hediffs != null)
			{
				foreach (var hediff in ___pawn.health.hediffSet.hediffs)
				{
					if (hediff is HediffResource hediffResource && hediff.CurStage is HediffStageResource hediffStageResource && hediffStageResource.preventDowning)
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(HediffSet), "GetPartHealth")]
	public static class Patch_GetPartHealth
	{
		private static void Postfix(ref float __result, HediffSet __instance, BodyPartRecord part)
		{
			if (__result <= 0 && __instance.hediffs != null)
			{
				foreach (var hediff in __instance.hediffs)
				{
					if (hediff is HediffResource hediffResource && hediff.CurStage is HediffStageResource hediffStageResource && hediffStageResource.preventDeath)
					{
						__result = 1f;
						var hediffInjury = __instance.hediffs.OfType<Hediff_Injury>().FirstOrDefault(x => x.part == part);
						if (hediffInjury != null)
                        {
							hediffInjury.Severity -= 1;
                        }
					}
				}
			}
		}
	}
}
