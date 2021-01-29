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


	[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
	internal static class TakeDamage_Patch
	{
		private static void Prefix(Thing __instance, ref DamageInfo dinfo)
		{
			Log.Message(" - Prefix - if (dinfo.Instigator is Pawn launcher) - 1", true);
			if (dinfo.Instigator is Pawn launcher)
			{
				Log.Message(" - Prefix - var equipment = launcher.equipment?.Primary; - 2", true);
				var equipment = launcher.equipment?.Primary;
				Log.Message(" - Prefix - if (equipment != null && dinfo.Weapon == equipment.def) - 3", true);
				if (equipment != null && dinfo.Weapon == equipment.def)
				{
					Log.Message(" - Prefix - var compCharge = equipment.GetComp<CompChargeResource>(); - 4", true);
					var compCharge = equipment.GetComp<CompChargeResource>();
					Log.Message(" - Prefix - var hediffResource = launcher.health.hediffSet.GetFirstHediffOfDef(compCharge.Props.hediffResource) as HediffResource; - 5", true);
					var hediffResource = launcher.health.hediffSet.GetFirstHediffOfDef(compCharge.Props.hediffResource) as HediffResource;
					Log.Message(" - Prefix - if (hediffResource != null && compCharge.Props.damageScaling.HasValue) - 6", true);
					if (hediffResource != null && compCharge.Props.damageScaling.HasValue)
					{
						switch (compCharge.Props.damageScaling.Value)
						{
							case DamageScalingMode.Flat: DoFlatDamage(ref dinfo, hediffResource, compCharge); break;
							case DamageScalingMode.Scalar: DoScalarDamage(ref dinfo, hediffResource, compCharge); break;
							default: break;
						}
					}
				}
			}
		}
	
		private static void DoFlatDamage(ref DamageInfo dinfo, HediffResource hediffResource, CompChargeResource compCharge)
		{
			var amount = dinfo.Amount * Mathf.Pow(compCharge.Props.damagePerCharge, (hediffResource.ResourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge);
			Log.Message("Flat: old damage: " + dinfo.Amount + " - new damage: " + amount);
			dinfo.SetAmount(amount);
			hediffResource.ResourceAmount = 0;
		}
		private static void DoScalarDamage(ref DamageInfo dinfo, HediffResource hediffResource, CompChargeResource compCharge)
		{
			var amount = dinfo.Amount * compCharge.Props.damagePerCharge * ((hediffResource.ResourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge);
			Log.Message("Scalar: old damage: " + dinfo.Amount + " - new damage: " + amount);
			dinfo.SetAmount(amount);
			hediffResource.ResourceAmount = 0;
		}
	}

	[HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
	public static class Patch_PreApplyDamage
	{
		private static void Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;
			foreach (var hediff in __instance?.health?.hediffSet?.hediffs?.OfType<HediffResource>())
			{
				if (dinfo.Amount > 0 && hediff.def.shieldProperties != null)
				{
					var shieldProps = hediff.def.shieldProperties;
					if (shieldProps.absorbRangeDamage && (dinfo.Weapon?.IsRangedWeapon ?? false))
					{
						ProcessDamage(__instance, ref dinfo, hediff, shieldProps);
					}
					else if (shieldProps.absorbMeleeDamage && (dinfo.Weapon is null || dinfo.Weapon.IsMeleeWeapon))
					{
						ProcessDamage(__instance, ref dinfo, hediff, shieldProps);
					}
					if (dinfo.Amount <= 0)
					{
						Log.Message(" - Prefix - absorbed = true; - 10", true);
						absorbed = true;
					}
				}
			}
		}

		private static void ProcessDamage(Pawn pawn, ref DamageInfo dinfo, HediffResource hediff, ShieldProperties shieldProps)
		{
			Log.Message("Pre: " + hediff.def.defName + " - hediff.ResourceAmount: " + hediff.ResourceAmount + " - Damage amount: " + dinfo.Amount);
			bool damageIsProcessed = false;
			if (shieldProps.resourceConsumptionPerDamage.HasValue && hediff.ResourceAmount >= shieldProps.resourceConsumptionPerDamage.Value)
			{
				if (shieldProps.maxAbsorb.HasValue)
				{
					dinfo.SetAmount(dinfo.Amount - shieldProps.maxAbsorb.Value);
				}
				else
				{
					dinfo.SetAmount(0);
				}
				hediff.ResourceAmount -= shieldProps.resourceConsumptionPerDamage.Value;
				damageIsProcessed = true;
			}
			else if (shieldProps.damageAbsorbedPerResource.HasValue)
			{
				var damageAmount = dinfo.Amount;
				if (shieldProps.maxAbsorb.HasValue && damageAmount > shieldProps.maxAbsorb.Value)
				{
					damageAmount = shieldProps.maxAbsorb.Value;
				}
				var resourceAmount = hediff.ResourceAmount;
				var ratioPerAbsorb = shieldProps.damageAbsorbedPerResource.Value;
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
				damageIsProcessed = true;
			}

			if (damageIsProcessed && shieldProps.postDamageDelay.HasValue)
			{
				var delayTicks = Find.TickManager.TicksGame + shieldProps.postDamageDelay.Value;
				var apparels = pawn.apparel?.WornApparel?.ToList();
				if (apparels != null)
				{
					foreach (var apparel in apparels)
					{
						var hediffComp = apparel.GetComp<CompAdjustHediffs>();
						if (hediffComp != null)
                        {
							hediffComp.delayTicks = delayTicks;
						}
					}
				}

				var equipments = pawn.equipment.AllEquipmentListForReading;
				if (equipments != null)
                {
					foreach (var equipment in equipments)
					{
						var hediffComp = equipment.GetComp<CompAdjustHediffs>();
						if (hediffComp != null)
						{
							hediffComp.delayTicks = delayTicks;
						}
					}
				}
			}
			Log.Message("Post: " + hediff.def.defName + " - hediff.ResourceAmount: " + hediff.ResourceAmount + " - Damage amount: " + dinfo.Amount);
		}
	}
}
