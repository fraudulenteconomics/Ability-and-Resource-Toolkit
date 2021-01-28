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
			if (dinfo.Instigator is Pawn launcher)
			{
				var equipment = launcher.equipment?.Primary;
				if (equipment != null && dinfo.Weapon == equipment.def && dinfo.Def.isRanged)
				{
					var options = equipment.def.GetModExtension<HediffAdjustOptions>();
					if (options != null)
					{
						foreach (var option in options.hediffOptions)
						{
							var hediffResource = launcher.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
							if (hediffResource != null && option.damageScaling.HasValue)
							{
								switch (option.damageScaling.Value)
								{
									case DamageScalingMode.Flat: DoFlatDamage(ref dinfo, hediffResource, option); continue;
									case DamageScalingMode.Scalar: DoScalarDamage(ref dinfo, hediffResource, option); continue;
									default: continue;
								}
							}
						}
					}
				}
			}
		}

		private static void DoFlatDamage(ref DamageInfo dinfo, HediffResource hediffResource, HediffOption option)
		{
			var amount = dinfo.Amount * Mathf.Pow(option.damagePerCharge, (hediffResource.ResourceAmount - option.minimumResourcePerUse) / option.resourcePerCharge);
			Log.Message("Flat: old damage: " + dinfo.Amount + " - new damage: " + amount);
			dinfo.SetAmount(amount);
		}
		private static void DoScalarDamage(ref DamageInfo dinfo, HediffResource hediffResource, HediffOption option)
		{
			var amount = dinfo.Amount * option.damagePerCharge * ((hediffResource.ResourceAmount - option.minimumResourcePerUse) / option.resourcePerCharge);
			Log.Message("Scalar: old damage: " + dinfo.Amount + " - new damage: " + amount);
			dinfo.SetAmount(amount);
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
				if (shieldProps.maxAbsorb.HasValue)
				{
					dinfo.SetAmount(dinfo.Amount - shieldProps.maxAbsorb.Value);
				}
				else
				{
					dinfo.SetAmount(0);
				}
				hediff.ResourceAmount -= shieldProps.resourceConsumptionPerDamage.Value;
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
			}

			if (shieldProps.postDamageDelay.HasValue)
			{
				var delayTicks = Find.TickManager.TicksGame + shieldProps.postDamageDelay.Value;

			}
			Log.Message("Post: " + hediff.def.defName + " - hediff.ResourceAmount: " + hediff.ResourceAmount + " - Damage amount: " + dinfo.Amount);
		}
	}
}
