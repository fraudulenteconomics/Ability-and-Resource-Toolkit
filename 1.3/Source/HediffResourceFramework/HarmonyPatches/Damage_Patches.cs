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
using static UnityEngine.GraphicsBuffer;

namespace HediffResourceFramework
{
	[HarmonyPatch(typeof(Projectile), "Launch", new Type[]
	{
		typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef)
	})]
	public static class Patch_Projectile_Launch
	{
		public static void Postfix(Projectile __instance, Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Thing equipment = null, ThingDef targetCoverDef = null)
		{
			if (equipment != null)
            {
				var comp = equipment.TryGetComp<CompResourceOnAction>();
				if (comp != null)
				{
					foreach (var resourceOnAction in comp.Props.resourcesOnAction)
					{
						if (!resourceOnAction.onSelf)
						{
							HediffResourceManager.Instance.firedProjectilesByEquipments[__instance] = equipment;
						}
					}
				}
			}

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
					var amount = (float)__result;
					HediffResourceUtils.ApplyChargeResource(ref amount, chargeResources);
					__result = (int)amount;
					compCharge.ProjectilesWithChargedResource.Remove(__instance);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), "Impact")]
	internal static class Impact_Patch
	{
		private static void Prefix(Projectile __instance, Thing hitThing)
		{
			if (hitThing is Pawn target && HediffResourceManager.Instance.firedProjectilesByEquipments.TryGetValue(__instance, out var equipment))
            {
				var comp = equipment.TryGetComp<CompResourceOnAction>();
				if (comp != null)
				{
					foreach (var resourceOnAction in comp.Props.resourcesOnAction)
					{
						if (!resourceOnAction.onSelf)
						{
							resourceOnAction.TryApplyOn(target);
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
			var effectOnImpactOptions = dinfo.Def.GetModExtension<EffectOnImpact>();
			if (effectOnImpactOptions != null && __instance.health?.hediffSet != null)
			{
				foreach (var resourceEffect in effectOnImpactOptions.resourceEffects)
				{
					var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance, resourceEffect.hediffDef,
						resourceEffect.adjustTargetResource, resourceEffect.addHediffIfMissing, resourceEffect.applyToPart);
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
					if (dinfo.Amount > 0 && hediff.def.ShieldIsActive(__instance))
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
								if (hediffResource != null && hediffResource.CanHaveDelay(newDelayTicks))
								{
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
						var hediffComp = equipment.GetComp<CompAdjustHediffs>();
						if (hediffComp != null && hediffComp.Props.resourceSettings != null)
						{
							foreach (var hediffOption in hediffComp.Props.resourceSettings)
							{
								var newDelayTicks = (int)(shieldProps.postDamageDelay.Value * hediffOption.postDamageDelayMultiplier);
								var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
								if (hediffResource != null && hediffResource.CanHaveDelay(newDelayTicks))
								{
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
