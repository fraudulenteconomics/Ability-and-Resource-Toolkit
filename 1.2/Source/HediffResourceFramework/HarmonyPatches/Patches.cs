using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
	[StaticConstructorOnStartup]
	internal static class HarmonyInit
	{
		static HarmonyInit()
		{
			Harmony harmony = new Harmony("Fraudecon.HediffResourceFramework");
			harmony.PatchAll();
			//MethodInfo method_Postfix = AccessTools.Method(typeof(HarmonyInit), "Postfix");
			//foreach (Type type in GenTypes.AllSubclassesNonAbstract(typeof(Verb)))
			//{
			//	MethodInfo methodToPatch = AccessTools.Method(type, "TryCastShot");
			//	try
			//	{
			//		harmony.Patch(methodToPatch, null, new HarmonyMethod(method_Postfix), null);
			//	}
			//	catch (Exception ex)
			//	{
			//	};
			//}
		}
	}

	[HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
	public static class Patch_TryCastNextBurstShot
	{
		private static void Postfix(Verb __instance)
		{
			if (__instance.Available() && __instance.CasterIsPawn && __instance.EquipmentSource != null)
			{
				Log.Message("Postfix: " + __instance);
				var options = __instance.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
				{
					foreach (var option in options.hediffOptions)
					{
						if (HediffResourceUtils.VerbMatches(__instance, option))
						{
							Log.Message("Adjusting hediff: " + option.hediff + " - " + option.severityOffset + " - " + option.verbIndex);
							HealthUtility.AdjustSeverity(__instance.CasterPawn, option.hediff, option.severityOffset);
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(CompReloadable), "CreateVerbTargetCommand")]
	public static class Patch_CreateVerbTargetCommand
    {
		private static void Postfix(ref Command_Reloadable __result, Thing gear, Verb verb)
		{
			if (__result != null && verb.CasterIsPawn && verb.EquipmentSource != null)
			{
				var options = verb.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
				{
					foreach (var option in options.hediffOptions)
                    {
						if (HediffResourceUtils.VerbMatches(verb, option))
                        {
							var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff);
							if (option.disableOnEmptyOrMissingHediff)
							{
								bool manaIsEmptyOrNull = manaHediff != null ? manaHediff.Severity <= 0 : false;
								if (manaIsEmptyOrNull)
								{
									HediffResourceUtils.DisableGizmoOnEmptyOrMissingHediff(option, __result);
								}
							}
							if (option.minimumSeverityCastRequirement != -1f)
							{
								if (manaHediff != null && manaHediff.Severity < option.minimumSeverityCastRequirement)
								{
									HediffResourceUtils.DisableGizmoOnEmptyOrMissingHediff(option, __result);
								}
							}
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(PawnVerbGizmoUtility), "GetGizmosForVerb")]
	public static class Patch_GetGizmosForVerb
	{
		private static void Postfix(Verb verb, ref IEnumerable<Gizmo> __result)
		{
			if (verb.CasterIsPawn && verb.EquipmentSource != null)
			{
				var options = verb.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
                {
					var list = __result.ToList();
					foreach (var option in options.hediffOptions)
					{
						if (HediffResourceUtils.VerbMatches(verb, option))
						{
							var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff);
							if (option.disableOnEmptyOrMissingHediff)
							{
								bool manaIsEmptyOrNull = manaHediff != null ? manaHediff.Severity <= 0 : true;
								if (manaIsEmptyOrNull)
								{
									foreach (var g in list)
									{
										HediffResourceUtils.DisableGizmoOnEmptyOrMissingHediff(option, g);
									}
								}
							}
							if (option.minimumSeverityCastRequirement != -1f)
							{
								if (manaHediff != null && manaHediff.Severity < option.minimumSeverityCastRequirement)
								{
									foreach (var g in list)
									{
										HediffResourceUtils.DisableGizmoOnEmptyOrMissingHediff(option, g);
									}
								}
							}
						}
					}
					__result = list;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Verb), "IsStillUsableBy")]
	public static class Patch_IsStillUsableBy
	{
		private static void Postfix(ref bool __result, Verb __instance, Pawn pawn)
		{
			if (__result)
			{
				__result = HediffResourceUtils.IsUsableBy(__instance);
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
				__result = HediffResourceUtils.IsUsableBy(__instance);
			}
		}
	}
}
