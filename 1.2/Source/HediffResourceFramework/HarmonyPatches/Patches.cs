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
				var options = __instance.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
				{
					foreach (var option in options.hediffOptions)
					{
						if (HediffResourceUtils.VerbMatches(__instance, option))
						{
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
			}
		}
	}

	[HarmonyPatch(typeof(Hediff), "PostAdd")]
	public static class Patch_PostAdd
	{
		private static void Postfix(Hediff __instance)
		{
			var pawn = __instance.pawn;
			var apparels = pawn.apparel?.WornApparel?.ToList();
			if (apparels != null)
			{
				foreach (var apparel in apparels)
				{
					var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
					if (hediffComp?.Props.hediffOptions != null)
					{
						foreach (var option in hediffComp.Props.hediffOptions)
						{
							if (option.dropWeaponOrApparelIfBlacklistHediff?.Contains(__instance.def) ?? false)
							{
								pawn.apparel.TryDrop(apparel);
							}
						}
					}
				}
			}

			var equipments = pawn.equipment.AllEquipmentListForReading;
			if (equipments != null)
			{
				foreach (var equipment in equipments)
				{
					var hediffComp = equipment.GetComp<CompWeaponAdjustHediffs>();
					if (hediffComp?.Props.hediffOptions != null)
					{
						foreach (var option in hediffComp.Props.hediffOptions)
						{
							if (option.dropWeaponOrApparelIfBlacklistHediff?.Contains(__instance.def) ?? false)
							{
								pawn.equipment.TryDropEquipment(equipment, out ThingWithComps result, pawn.Position);
							}
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
	public static class Patch_PreApplyDamage
	{
		private static void Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;
			foreach (var hediff in __instance?.health?.hediffSet?.hediffs?.Where(x => x is HediffResource).Cast<HediffResource>())
			{
				if (dinfo.Amount > 0 && hediff.def.shieldProperties != null)
				{
					var shieldProps = hediff.def.shieldProperties;
					if (shieldProps.absorbRangeDamage && (dinfo.Weapon?.IsRangedWeapon ?? false))
					{
						ProcessDamage(ref dinfo, hediff, shieldProps);
					}
					else if (shieldProps.absorbMeleeDamage && (dinfo.Weapon is null || dinfo.Weapon.IsMeleeWeapon))
					{
						ProcessDamage(ref dinfo, hediff, shieldProps);
					}
					if (dinfo.Amount <= 0)
					{
						Log.Message(" - Prefix - absorbed = true; - 10", true);
						absorbed = true;
					}
				}
			}
		}

		private static void ProcessDamage(ref DamageInfo dinfo, HediffResource hediff, ShieldProperties shieldProps)
		{
			Log.Message("Pre: " + hediff.def.defName + " - hediff.ResourceAmount: " + hediff.ResourceAmount + " - Damage amount: " + dinfo.Amount);
			if (shieldProps.resourceConsumptionPerDamage.HasValue && hediff.ResourceAmount >= shieldProps.resourceConsumptionPerDamage.Value)
			{
				if (shieldProps.maxDamageToAbsorb.HasValue)
                {
					dinfo.SetAmount(dinfo.Amount - shieldProps.maxDamageToAbsorb.Value);
                }
				else
                {
					dinfo.SetAmount(0);
				}
				hediff.ResourceAmount -= shieldProps.resourceConsumptionPerDamage.Value;
			}
			else if (shieldProps.ratioPerAbsorb.HasValue)
			{
				var damageAmount = dinfo.Amount;
				if (shieldProps.maxDamageToAbsorb.HasValue && damageAmount > shieldProps.maxDamageToAbsorb.Value)
				{
					damageAmount = shieldProps.maxDamageToAbsorb.Value;
				}
				var resourceAmount = hediff.ResourceAmount;
				var ratioPerAbsorb = shieldProps.ratioPerAbsorb.Value;
				var resourceCost = damageAmount / ratioPerAbsorb;
				if (resourceAmount >= resourceCost)
				{
					Log.Message(" - ProcessDamage - dinfo.SetAmount(0f); - 20", true);
					dinfo.SetAmount(0f);
					hediff.ResourceAmount -= resourceCost;
				}
				else
				{
					damageAmount -= resourceAmount * ratioPerAbsorb;
					dinfo.SetAmount(damageAmount);
					Log.Message(" - ProcessDamage - hediff.ResourceAmount = 0; - 24", true);
					hediff.ResourceAmount = 0;
				}
			}
			Log.Message("Post: " + hediff.def.defName + " - hediff.ResourceAmount: " + hediff.ResourceAmount + " - Damage amount: " + dinfo.Amount);
		}
	}

	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain_NewTmp")]
	public static class Patch_HasPartsToWear
	{
		private static bool Prefix(ref float __result, Pawn pawn, Apparel ap, List<float> wornScoresCache)
		{
			if (!AddHumanlikeOrders_Fix.CanWear(pawn, ap, out string tmp))
            {
				__result = -1000f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
	public static class AddHumanlikeOrders_Fix
	{
		public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
		{
			IntVec3 c = IntVec3.FromVector3(clickPos);
			foreach (var apparel in GridsUtility.GetThingList(c, pawn.Map).Where(x => x is Apparel).Cast<Apparel>())
			{
				TaggedString toCheck = "ForceWear".Translate(apparel.LabelCap, apparel);
				FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
				(toCheck));
				if (floatMenuOption != null && !CanWear(pawn, apparel, out string reason))
				{
					opts.Remove(floatMenuOption);
					var newOption = new FloatMenuOption("CannotWear".Translate(apparel.LabelShort) + "(" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					opts.Add(newOption);
				}
			}

			if (pawn.equipment != null)
			{
				List<Thing> thingList = c.GetThingList(pawn.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i].TryGetComp<CompEquippable>() != null)
					{
						var equipment = (ThingWithComps)thingList[i];
						TaggedString toCheck = "Equip".Translate(equipment.LabelShort);
						FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
						(toCheck));
						if (floatMenuOption != null && !CanEquip(pawn, equipment, out string reason))
						{
							opts.Remove(floatMenuOption);
							var newOption = new FloatMenuOption("CannotEquip".Translate(equipment.LabelShort) + " (" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
							opts.Add(newOption);
						}
					}
				}
			}
		}

		public static bool CanWear(Pawn pawn, Apparel apparel, out string reason)
		{
			var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
			if (hediffComp?.Props.hediffOptions != null)
			{
				foreach (var option in hediffComp.Props.hediffOptions)
				{
					if (option.disallowEquippingIfEmptyNullHediff)
					{
						var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
						if (hediff is null || hediff.ResourceAmount <= 0)
						{
							reason = option.cannotEquipReason;
							return false;
						}
					}

					if (option.blackListHediffsPreventEquipping != null)
					{
						foreach (var hediffDef in option.blackListHediffsPreventEquipping)
						{
							var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
							if (hediff != null)
							{
								reason = option.cannotEquipReasonIncompatible + hediffDef.label;
								return false;
							}
						}
					}
				}
			}
			reason = "";
			return true;
		}

		private static bool CanEquip(Pawn pawn, ThingWithComps weapon, out string reason)
		{
			var hediffComp = weapon.GetComp<CompWeaponAdjustHediffs>();
			if (hediffComp?.Props.hediffOptions != null)
			{
				foreach (var option in hediffComp.Props.hediffOptions)
				{
					if (option.disallowEquippingIfEmptyNullHediff)
					{
						var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
						if (hediff is null || hediff.ResourceAmount <= 0)
						{
							reason = option.cannotEquipReason;
							return false;
						}
					}

					if (option.blackListHediffsPreventEquipping != null)
					{
						foreach (var hediffDef in option.blackListHediffsPreventEquipping)
						{
							var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
							if (hediff != null)
							{
								reason = option.cannotEquipReasonIncompatible;
								return false;
							}
						}
					}
				}
			}
			reason = "";
			return true;
		}
	}
}
