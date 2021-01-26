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
using Verse.AI;

namespace HediffResourceFramework
{
	[StaticConstructorOnStartup]
	internal static class HarmonyInit
	{
		static HarmonyInit()
		{
			Harmony harmony = new Harmony("Fraudecon.HediffResourceFramework");
			harmony.PatchAll();
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
							Log.Message("Adjusting hediff: " + option.hediff + " - " + option.resourceOffset + " - " + option.verbIndex);
							HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourceOffset, option);
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
							var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
							if (option.disableOnEmptyOrMissingHediff)
							{
								bool manaIsEmptyOrNull = manaHediff != null ? manaHediff.ResourceAmount <= 0 : false;
								if (manaIsEmptyOrNull)
								{
									HediffResourceUtils.DisableGizmoOnEmptyOrMissingHediff(option, __result);
								}
							}
							if (option.minimumResourceCastRequirement != -1f)
							{
								if (manaHediff != null && manaHediff.ResourceAmount < option.minimumResourceCastRequirement)
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
							var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
							if (option.disableOnEmptyOrMissingHediff)
							{
								bool manaIsEmptyOrNull = manaHediff != null ? manaHediff.ResourceAmount <= 0 : true;
								if (manaIsEmptyOrNull)
								{
									foreach (var g in list)
									{
										HediffResourceUtils.DisableGizmoOnEmptyOrMissingHediff(option, g);
									}
								}
							}
							if (option.minimumResourceCastRequirement != -1f)
							{
								if (manaHediff != null && manaHediff.ResourceAmount < option.minimumResourceCastRequirement)
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
				__result = HediffResourceUtils.IsUsableBy(__instance, out bool verbIsFromHediffResource);
				if (verbIsFromHediffResource)
                {
					pawn.jobs.StopAll();
                }
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
				__result = HediffResourceUtils.IsUsableBy(__instance, out bool verbIsFromHediffResource);
				if (verbIsFromHediffResource)
				{
					__instance.CasterPawn?.jobs.StopAll();
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(JobDriver_Wear), "TryMakePreToilReservations")]
	public static class Patch_TryMakePreToilReservations
	{
		private static bool Prefix(JobDriver_Wear __instance)
		{
			var apparel = (Apparel)__instance.job.GetTarget(TargetIndex.A).Thing;
			var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
			if (hediffComp?.Props.hediffOptions != null)
			{
				foreach (var option in hediffComp.Props.hediffOptions)
				{
					if (option.disallowEquippingIfEmptyNullHediff)
					{
						var hediff = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
						if (hediff is null || hediff.ResourceAmount <= 0)
                        {
							return false;
                        }
					}
					if (option.blackListHediffsPreventEquipping != null)
                    {
						foreach (var hediffDef in option.blackListHediffsPreventEquipping)
                        {
							var hediff = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff);
							if (hediff != null)
                            {
								return false;
                            }
						}
					}
				}
			}
			return true;
		}
	}
}
