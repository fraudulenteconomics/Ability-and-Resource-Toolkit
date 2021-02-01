﻿using HarmonyLib;
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
	[HarmonyPatch(typeof(Projectile), "DamageAmount", MethodType.Getter)]
	internal static class DamageAmount_Patch
	{
		private static void Postfix(Projectile __instance, ref int __result)
		{
			if (__instance.Launcher is Pawn launcher)
			{
				var equipment = launcher.equipment?.Primary;
				if (equipment != null && __instance.EquipmentDef == equipment.def)
				{
					var compCharge = equipment.GetComp<CompChargeResource>();
					if (compCharge != null)
					{
						var hediffResource = launcher.health.hediffSet.GetFirstHediffOfDef(compCharge.Props.hediffResource) as HediffResource;
						Log.Message("1 instance - " + __instance + " - __result: " + __result + " - hediffResource: " + hediffResource + " - compCharge.Props.damageScaling.HasValue: " + compCharge.Props.damageScaling.HasValue);
						if (hediffResource != null && compCharge.Props.damageScaling.HasValue)
						{
							switch (compCharge.Props.damageScaling.Value)
							{
								case DamageScalingMode.Flat: DoFlatDamage(ref __result, hediffResource, compCharge); break;
								case DamageScalingMode.Scalar: DoScalarDamage(ref __result, hediffResource, compCharge); break;
								case DamageScalingMode.Linear: DoLinearDamage(ref __result, hediffResource, compCharge); break;
								default: break;
							}
						}
						Log.Message("2 instance - " + __instance + " - result: " + __result + " - hediffResource: " + hediffResource + " - compCharge.Props.damageScaling.HasValue: " + compCharge.Props.damageScaling.HasValue);
					}
				}
			}
		}

		private static void DoFlatDamage(ref int __result, HediffResource hediffResource, CompChargeResource compCharge)
		{
			var oldDamage = __result;
			__result = (int)(__result + (compCharge.Props.damagePerCharge * (hediffResource.ResourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge));
			Log.Message("Flat: old damage: " + oldDamage + " - new damage: " + __result);
			hediffResource.ResourceAmount = 0;
		}
		private static void DoScalarDamage(ref int __result, HediffResource hediffResource, CompChargeResource compCharge)
		{
			var oldDamage = __result;
			__result = (int)(__result * Mathf.Pow((1 + compCharge.Props.damagePerCharge), (hediffResource.ResourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge));
			Log.Message("Scalar: old damage: " + oldDamage + " - new damage: " + __result);
			hediffResource.ResourceAmount = 0;
		}

		private static void DoLinearDamage(ref int __result, HediffResource hediffResource, CompChargeResource compCharge)
		{
			var oldDamage = __result;
			__result = (int)(__result * (1 + (compCharge.Props.damagePerCharge * (hediffResource.ResourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge)));
			Log.Message("Linear: old damage: " + oldDamage + " - new damage: " + __result);
			hediffResource.ResourceAmount = 0;
		}
	}

	[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal", new Type[] 
	{
		typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool), typeof(bool)
	})]
	public static class Patch_RenderPawnInternal
    {
		public static void Postfix(bool portrait, Pawn ___pawn)
		{
			if (portrait) return;
			foreach (var hediff in ___pawn.health.hediffSet.hediffs.OfType<HediffResource>())
			{
				hediff.Draw();
			}
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
					hediff.AbsorbedDamage(dinfo);
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
							hediffComp.delayTicks += delayTicks * hediffComp.Props.postDamageDelayMultiplier;
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
							hediffComp.delayTicks += delayTicks * hediffComp.Props.postDamageDelayMultiplier;
						}
					}
				}
			}
			Log.Message("Post: " + hediff.def.defName + " - hediff.ResourceAmount: " + hediff.ResourceAmount + " - Damage amount: " + dinfo.Amount);
		}
	}
}
