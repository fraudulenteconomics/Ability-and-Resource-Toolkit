using HarmonyLib;
using MVCF.Utilities;
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
				Log.Message(method_Postfix + " - Type: " + type + " - Method: " + methodToPatch);
				try
				{
					harmony.Patch(methodToPatch, null, new HarmonyMethod(method_Postfix), null);
					Log.Message("Patching: " + methodToPatch);
				}
				catch (Exception ex)
				{
					Log.Message("FAiled to patch: " + methodToPatch + " - " + ex);
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
	}

	[HarmonyPatch(typeof(PawnVerbGizmoUtility), "GetGizmosForVerb")]
	public static class Patch_CreateVerbTargetCommand
	{
		private static void Postfix(Verb verb, ref IEnumerable<Gizmo> __result)
		{
			if (verb.CasterIsPawn && verb.EquipmentSource != null)
			{
				var options = verb.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
                {
					var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ManaTestHediff"));
					bool manaIsEmpty = manaHediff != null ? manaHediff.Severity <= 0 : true;
					if (manaIsEmpty)
                    {
						var list = __result.ToList();
						foreach (var g in list)
						{
							foreach (var option in options.hediffOptions)
							{
								if (option.disableOnEmptyMana)
								{
									if (!option.verbLabel.NullOrEmpty() && verb.ReportLabel == option.verbLabel)
									{
										g.Disable("Mana is Empty");
									}
									else if (option.verbIndex != -1 && verb.EquipmentSource.def.Verbs.IndexOf(verb.verbProps) == option.verbIndex)
									{
										g.Disable("Mana is Empty");
									}
									else
									{
										g.Disable("Mana is Empty");
									}
								}
							}
						}
						__result = list;
					}
				}
			}

		}
	}
}
