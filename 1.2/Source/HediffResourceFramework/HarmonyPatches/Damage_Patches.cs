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
	[HarmonyPatch(typeof(Projectile), "Launch", new Type[]
	{
		typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(Thing), typeof(ThingDef)
	})]
	public static class Patch_Projectile_Launch
	{
		public static void Postfix(Projectile __instance, Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Thing equipment = null, ThingDef targetCoverDef = null)
		{
			if (launcher is Pawn pawn && Patch_TryCastShot.verbSource != null)
			{
				var compCharge = GetChargeSourceFrom(Patch_TryCastShot.verbSource, pawn);
				if (compCharge != null)
                {
					var verbProps = Patch_TryCastShot.verbSource.GetVerb.verbProps as VerbResourceProps;
					if (verbProps?.chargeSettings != null)
					{
						foreach (var chargeSettings in verbProps.chargeSettings)
						{
							var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(chargeSettings.hediffResource) as HediffResource;
							if (hediffResource != null && chargeSettings.damageScaling.HasValue)
							{
								HRFLog.Message("Should do charging damage: " + __instance + " - " + hediffResource);
								if (compCharge.ProjectilesWithChargedResource.ContainsKey(__instance))
								{
									compCharge.ProjectilesWithChargedResource[__instance].chargeResources.Add(new ChargeResource(hediffResource.ResourceAmount, chargeSettings));
								}
								else
								{
									compCharge.ProjectilesWithChargedResource[__instance] = new ChargeResources();
									compCharge.ProjectilesWithChargedResource[__instance].chargeResources = new List<ChargeResource> { new ChargeResource(hediffResource.ResourceAmount, chargeSettings) };
								}
								hediffResource.ResourceAmount = 0f;
							}
						}
					}
				}
			}
		}

		private static IChargeResource GetChargeSourceFrom(Verb verb, Pawn pawn)
        {
			if (verb.EquipmentSource != null) return verb.EquipmentSource.GetComp<CompChargeResource>();
			if (verb.HediffCompSource != null) return verb.HediffSource.TryGetComp<HediffCompChargeResource>();
			return null;
        }
	}

	[HarmonyPatch(typeof(Projectile), "DamageAmount", MethodType.Getter)]
	internal static class DamageAmount_Patch
	{
		private static void Postfix(Projectile __instance, ref int __result)
		{
			if (__instance.Launcher is Pawn launcher)
			{
				var compCharge = HediffResourceUtils.GetCompChargeSourceFor(launcher, __instance);
				if (compCharge?.ProjectilesWithChargedResource != null && compCharge.ProjectilesWithChargedResource.TryGetValue(__instance, out ChargeResources chargeResources) && chargeResources != null)
				{
					foreach (var chargeResource in chargeResources.chargeResources)
					{
						HRFLog.Message("1 instance - " + __instance + " - __result: " + __result + " - hediffResource: " + chargeResource.chargeResource + " - compCharge.Props.damageScaling.HasValue: " + chargeResource.chargeSettings.damageScaling.HasValue);
						switch (chargeResource.chargeSettings.damageScaling)
						{
							case DamageScalingMode.Flat: DoFlatDamage(ref __result, chargeResource.chargeResource, chargeResource.chargeSettings); break;
							case DamageScalingMode.Scalar: DoScalarDamage(ref __result, chargeResource.chargeResource, chargeResource.chargeSettings); break;
							case DamageScalingMode.Linear: DoLinearDamage(ref __result, chargeResource.chargeResource, chargeResource.chargeSettings); break;
							default: break;
						}
						HRFLog.Message("2 instance - " + __instance + " - result: " + __result + " - hediffResource: " + chargeResource.chargeResource + " - compCharge.Props.damageScaling.HasValue: " + chargeResource.chargeSettings.damageScaling.HasValue);
					}
					compCharge.ProjectilesWithChargedResource.Remove(__instance);
				}
			}
		}
		private static void DoFlatDamage(ref int __result, float resourceAmount, ChargeSettings chargeSettings)
		{
			var oldDamage = __result;
			__result = (int)(__result + (chargeSettings.damagePerCharge * (resourceAmount - chargeSettings.minimumResourcePerUse) / chargeSettings.resourcePerCharge));
			HRFLog.Message("Flat: old damage: " + oldDamage + " - new damage: " + __result);
		}
		private static void DoScalarDamage(ref int __result, float resourceAmount, ChargeSettings chargeSettings)
		{
			var oldDamage = __result;
			HRFLog.Message("chargeSettings.damagePerCharge: " + chargeSettings.damagePerCharge + " - resourceAmount: " + resourceAmount 
				+ " - chargeSettings.minimumResourcePerUse: " + chargeSettings.minimumResourcePerUse + " - chargeSettings.resourcePerCharge: " + chargeSettings.resourcePerCharge);
			__result = (int)(__result * Mathf.Pow((1 + chargeSettings.damagePerCharge), (resourceAmount - chargeSettings.minimumResourcePerUse) / chargeSettings.resourcePerCharge));
			HRFLog.Message("Scalar: old damage: " + oldDamage + " - new damage: " + __result);
		}

		private static void DoLinearDamage(ref int __result, float resourceAmount, ChargeSettings chargeSettings)
		{
			var oldDamage = __result;
			__result = (int)(__result * (1 + (chargeSettings.damagePerCharge * (resourceAmount - chargeSettings.minimumResourcePerUse) / chargeSettings.resourcePerCharge)));
			HRFLog.Message("Linear: old damage: " + oldDamage + " - new damage: " + __result);
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
			var effectOnImpactOptions = dinfo.Def.GetModExtension<EffectOnImpact>();
			if (effectOnImpactOptions != null && __instance.health?.hediffSet != null)
			{
				foreach (var resourceEffect in effectOnImpactOptions.resourceEffects)
				{
					var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance, resourceEffect.hediffDef, resourceEffect.adjustTargetResource, resourceEffect.addHediffIfMissing);
					if (hediffResource != null && resourceEffect.delayTargetOnDamage != IntRange.zero)
					{
						hediffResource.AddDelay(resourceEffect.delayTargetOnDamage.RandomInRange);
					}
				}
			}

			var hediffResources = __instance?.health?.hediffSet?.hediffs?.OfType<HediffResource>().ToList();
			if (hediffResources != null)
            {
				for (int num = hediffResources.Count - 1; num >= 0; num--)
                {
					var hediff = hediffResources[num];
					if (dinfo.Amount > 0 && hediff.def.shieldProperties != null)
					{
						var shieldProps = hediff.def.shieldProperties;
						if (shieldProps.absorbRangeDamage && (dinfo.Weapon?.IsRangedWeapon ?? false))
						{
							ProcessDamage(__instance, ref dinfo, hediff, shieldProps);
						}
						else if (shieldProps.absorbMeleeDamage && (dinfo.Weapon is null || dinfo.Weapon == ThingDefOf.Human || dinfo.Weapon.IsMeleeWeapon))
						{
							ProcessDamage(__instance, ref dinfo, hediff, shieldProps);
						}
						if (dinfo.Amount <= 0)
						{
							absorbed = true;
						}
						hediff.AbsorbedDamage(dinfo);
					}
				}
			}
		}

		private static void ProcessDamage(Pawn pawn, ref DamageInfo dinfo, HediffResource hediff, ShieldProperties shieldProps)
		{
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
					dinfo.SetAmount(0f);
					hediff.ResourceAmount -= resourceCost;
				}
				else
				{
					damageAmount -= resourceAmount * ratioPerAbsorb;
					dinfo.SetAmount(damageAmount);
					hediff.ResourceAmount = 0;
				}
				damageIsProcessed = true;
			}

			if (damageIsProcessed && shieldProps.postDamageDelay.HasValue)
			{
				var apparels = pawn.apparel?.WornApparel?.ToList();
				if (apparels != null)
				{
					foreach (var apparel in apparels)
					{
						var hediffComp = apparel.GetComp<CompAdjustHediffs>();
						if (hediffComp != null && hediffComp.Props.resourceSettings != null)
						{
							foreach (var hediffOption in hediffComp.Props.resourceSettings)
							{
								var newDelayTicks = (int)(shieldProps.postDamageDelay.Value * hediffOption.postDamageDelayMultiplier);
								var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
								HRFLog.Message(apparel + " - hediffResource: " + hediffResource);
								HRFLog.Message(apparel + " - hediffResource.CanHaveDelay(newDelayTicks): " + hediffResource?.CanHaveDelay(newDelayTicks));
								if (hediffResource != null && hediffResource.CanHaveDelay(newDelayTicks))
								{
									HRFLog.Message(" - ProcessDamage - hediffResource.AddDelay(newDelayTicks);; - 32", true);
									hediffResource.AddDelay(newDelayTicks);
								}
							}
						}
					}
				}

				var equipments = pawn.equipment?.AllEquipmentListForReading;
				if (equipments != null)
				{
					foreach (var equipment in equipments)
					{
						HRFLog.Message(" - ProcessDamage - var hediffComp = equipment.GetComp<CompAdjustHediffs>(); - 37", true);
						var hediffComp = equipment.GetComp<CompAdjustHediffs>();
						HRFLog.Message(" - ProcessDamage - if (hediffComp != null && hediffComp.Props.hediffOptions != null) - 38", true);
						if (hediffComp != null && hediffComp.Props.resourceSettings != null)
						{
							HRFLog.Message(" - ProcessDamage - foreach (var hediffOption in hediffComp.Props.hediffOptions) - 40", true);
							foreach (var hediffOption in hediffComp.Props.resourceSettings)
							{
								var newDelayTicks = (int)(shieldProps.postDamageDelay.Value * hediffOption.postDamageDelayMultiplier);
								HRFLog.Message(" - ProcessDamage - var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource; - 41", true);
								var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
								if (hediffResource != null && hediffResource.CanHaveDelay(newDelayTicks))
								{
									HRFLog.Message(" - ProcessDamage - hediffResource.AddDelay(newDelayTicks);; - 42", true);
									hediffResource.AddDelay(newDelayTicks);
								}
							}
						}
					}
				}
			}
		}
	}
}
