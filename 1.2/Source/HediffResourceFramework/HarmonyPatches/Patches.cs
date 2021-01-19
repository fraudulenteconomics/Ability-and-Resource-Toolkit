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
			MethodInfo method_Postfix = AccessTools.Method(typeof(HarmonyInit), "Postfix");
			foreach (Type type in GenTypes.AllSubclassesNonAbstract(typeof(Verb)))
			{
				MethodInfo methodToPatch = AccessTools.Method(type, "TryCastShot");
				try
				{
					harmony.Patch(methodToPatch, null, new HarmonyMethod(method_Postfix), null);
				}
				catch (Exception ex)
				{
				};
			}
		}

		private static void Postfix(Verb __instance, bool __result)
		{
			if (__result && __instance.CasterIsPawn && __instance.EquipmentSource != null)
            {
				var options = __instance.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
                {
					foreach (var option in options.hediffOptions)
                    {
						if (!option.verbLabel.NullOrEmpty() && __instance.ReportLabel == option.verbLabel)
                        {
							HealthUtility.AdjustSeverity(__instance.CasterPawn, option.hediff, option.severityOffset);
                        }
						else if (option.verbIndex != -1 && __instance.EquipmentSource.def.Verbs.IndexOf(__instance.verbProps) == option.verbIndex)
                        {
							HealthUtility.AdjustSeverity(__instance.CasterPawn, option.hediff, option.severityOffset);
						}
						else
                        {
							HealthUtility.AdjustSeverity(__instance.CasterPawn, option.hediff, option.severityOffset);
						}
					}
                }
			}
		}

		public static void DisableGizmoOnEmptyOrMissingHediff(Verb verb, HediffOption option, Gizmo gizmo)
        {
			if (option.disableOnEmptyOrMissingHediff)
			{
				if (!option.verbLabel.NullOrEmpty() && verb.ReportLabel == option.verbLabel)
				{
					gizmo.Disable(option.disableReason);
				}
				else if (option.verbIndex != -1 && verb.EquipmentSource.def.Verbs.IndexOf(verb.verbProps) == option.verbIndex)
				{
					gizmo.Disable(option.disableReason);
				}
				else
				{
					gizmo.Disable(option.disableReason);
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
						var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff);
						if (option.disableOnEmptyOrMissingHediff)
                        {
							bool manaIsEmptyOrNull = manaHediff != null ? manaHediff.Severity <= 0 : false;
							if (manaIsEmptyOrNull)
							{
								HarmonyInit.DisableGizmoOnEmptyOrMissingHediff(verb, option, __result);
							}
						}
						if (option.minimumSeverityCastRequirement != -1f)
						{
							Log.Message("minimumSeverityCastRequirement: " + verb.CasterPawn + " - " + option.hediff + " - manaHediff: " + manaHediff + " - " + " - " + manaHediff?.Severity);
							if (manaHediff.Severity < option.minimumSeverityCastRequirement)
							{
								HarmonyInit.DisableGizmoOnEmptyOrMissingHediff(verb, option, __result);
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
						var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff);
						if (option.disableOnEmptyOrMissingHediff)
						{
							bool manaIsEmptyOrNull = manaHediff != null ? manaHediff.Severity <= 0 : true;
							if (manaIsEmptyOrNull)
							{
								foreach (var g in list)
								{
									HarmonyInit.DisableGizmoOnEmptyOrMissingHediff(verb, option, g);
								}
							}
						}
						if (option.minimumSeverityCastRequirement != -1f)
						{
							Log.Message("minimumSeverityCastRequirement: " + verb.CasterPawn + " - " + option.hediff + " - manaHediff: " + manaHediff + " - " + " - " + manaHediff?.Severity);
							if (manaHediff.Severity < option.minimumSeverityCastRequirement)
							{
								foreach (var g in list)
								{
									HarmonyInit.DisableGizmoOnEmptyOrMissingHediff(verb, option, g);
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
