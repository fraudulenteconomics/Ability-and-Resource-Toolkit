using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HediffResourceFramework
{
	public class HediffResourcesCache
	{
		public HediffResourcesCache(List<HediffResource> value)
		{
			this.value = value;
		}
		public List<HediffResource> value;
		public List<HediffResource> Value
		{
			get
			{
				return value;
			}
			set
			{
				this.value = value;
				updateTick = Find.TickManager.TicksGame;
			}
		}
		public int updateTick;
	}
	public class AdjustResourcesCache
	{
		public AdjustResourcesCache(List<IAdjustResource> value)
		{
			this.value = value;
		}
		public List<IAdjustResource> value;
		public List<IAdjustResource> Value
		{
			get
			{
				return value;
			}
			set
			{
				this.value = value;
				updateTick = Find.TickManager.TicksGame;
			}
		}
		public int updateTick;
	}

	[StaticConstructorOnStartup]
	public static class HediffResourceUtils
	{
		public static bool CanDrink(Pawn pawn, Thing potion, out string reason)
		{
			var comp = potion.def?.ingestible?.outcomeDoers?.OfType<IngestionOutcomeDoer_GiveHediffResource>().FirstOrDefault();
			if (comp?.blacklistHediffsPreventAdd != null)
			{
				foreach (var hediff in comp.blacklistHediffsPreventAdd)
				{
					if (pawn.health.hediffSet.GetFirstHediffOfDef(hediff) != null)
					{
						reason = comp.cannotDrinkReason;
						return false;
					}
				}
			}
			reason = "";
			return true;
		}
		public static bool CanWear(Pawn pawn, Apparel apparel, out string reason)
		{
			var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
			if (hediffComp?.Props.resourceSettings != null)
			{
				foreach (var option in hediffComp.Props.resourceSettings)
				{
					var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
					if (option.disallowEquipIfHediffMissing)
					{
						if (hediff is null)
						{
							reason = option.cannotEquipReason;
							return false;
						}
					}
					if (option.disallowEquipIfOverCapacity && !hediff.CanGainCapacity(option.maxResourceCapacityOffset))
                    {
						reason = option.overCapacityReasonKey.Translate(pawn.Named("PAWN"), apparel.Named("THING"));
						return false;
					}

					if (option.blackListHediffsPreventEquipping != null)
					{
						foreach (var hediffDef in option.blackListHediffsPreventEquipping)
						{
							var blackListHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
							if (blackListHediff != null)
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

		public static bool CanEquip(Pawn pawn, ThingWithComps weapon, out string reason)
		{
			var hediffComp = weapon.GetComp<CompWeaponAdjustHediffs>();
			if (hediffComp?.Props.resourceSettings != null)
			{
				foreach (var option in hediffComp.Props.resourceSettings)
				{
					var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
					if (option.disallowEquipIfHediffMissing)
					{
						if (hediff is null || hediff.ResourceAmount <= 0)
						{
							reason = option.cannotEquipReason;
							return false;
						}
					}

					if (option.disallowEquipIfOverCapacity && !hediff.CanGainCapacity(option.maxResourceCapacityOffset))
					{
						reason = option.overCapacityReasonKey.Translate(pawn.Named("PAWN"), weapon.Named("THING"));
						return false;
					}

					if (option.blackListHediffsPreventEquipping != null)
					{
						foreach (var hediffDef in option.blackListHediffsPreventEquipping)
						{
							var blackListHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
							if (blackListHediff != null)
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
		public static bool IsHediffUser(this Pawn pawn)
		{
			return pawn.health.hediffSet.hediffs.Any(x => x is HediffResource);
		}

		private static Dictionary<Pawn, AdjustResourcesCache> resourceCache = new Dictionary<Pawn, AdjustResourcesCache>();
		public static List<IAdjustResource> GetAllAdjustHediffsComps(Pawn pawn)
		{
			if (resourceCache.TryGetValue(pawn, out AdjustResourcesCache adjustResourcesCache))
            {
				var adjusters = adjustResourcesCache.value;
				if (Find.TickManager.TicksGame > adjustResourcesCache.updateTick + 60)
                {
					adjusters = GetAdjustResourcesInt(pawn);
					adjustResourcesCache.Value = adjusters;
				}
				return adjusters;
            }
			else
            {
				var adjusters = GetAdjustResourcesInt(pawn);
				resourceCache[pawn] = new AdjustResourcesCache(adjusters);
				return adjusters;
			}
		}

		private static Dictionary<Apparel, CompApparelAdjustHediffs> compApparelAdjustCache = new Dictionary<Apparel, CompApparelAdjustHediffs>();
		private static Dictionary<ThingWithComps, CompWeaponAdjustHediffs> compWeaponAdjustCache = new Dictionary<ThingWithComps, CompWeaponAdjustHediffs>();
		private static Dictionary<Hediff, HediffComp_AdjustHediffs> hediffCompAdjustCache = new Dictionary<Hediff, HediffComp_AdjustHediffs>();
		private static Dictionary<Hediff, HediffComp_AdjustHediffsPerStages> hediffCompAdjustPerStageCache = new Dictionary<Hediff, HediffComp_AdjustHediffsPerStages>();
		private static Dictionary<Pawn, CompTraitsAdjustHediffs> compTraitsAdjustHediffsCache = new Dictionary<Pawn, CompTraitsAdjustHediffs>();
		private static List<IAdjustResource> GetAdjustResourcesInt(Pawn pawn)
        {
			List<IAdjustResource> adjustHediffs = new List<IAdjustResource>();
			var apparels = pawn.apparel?.WornApparel?.ToList();
			if (apparels != null)
			{
				foreach (var apparel in apparels)
				{
					if (!compApparelAdjustCache.TryGetValue(apparel, out CompApparelAdjustHediffs comp))
                    {
						comp = apparel.GetComp<CompApparelAdjustHediffs>();
						compApparelAdjustCache[apparel] = comp;
                    }
					if (comp != null)
                    {
						adjustHediffs.Add(comp);
                    }
				}
			}

			var equipments = pawn.equipment?.AllEquipmentListForReading;
			if (equipments != null)
			{
				foreach (var equipment in equipments)
				{
					if (!compWeaponAdjustCache.TryGetValue(equipment, out CompWeaponAdjustHediffs comp))
					{
						comp = equipment.GetComp<CompWeaponAdjustHediffs>();
						compWeaponAdjustCache[equipment] = comp;
					}
					if (comp != null)
					{
						adjustHediffs.Add(comp);
					}
				}
			}

			if (pawn.health?.hediffSet?.hediffs != null)
			{
				foreach (var hediff in pawn.health.hediffSet.hediffs)
				{
					if (!hediffCompAdjustCache.TryGetValue(hediff, out HediffComp_AdjustHediffs comp))
					{
						comp = hediff.TryGetComp<HediffComp_AdjustHediffs>();
						hediffCompAdjustCache[hediff] = comp;
					}
					if (comp != null)
					{
						adjustHediffs.Add(comp);
					}
					if (!hediffCompAdjustPerStageCache.TryGetValue(hediff, out HediffComp_AdjustHediffsPerStages comp2))
					{
						comp2 = hediff.TryGetComp<HediffComp_AdjustHediffsPerStages>();
						hediffCompAdjustPerStageCache[hediff] = comp2;
					}
					if (comp2 != null)
					{
						adjustHediffs.Add(comp2);
					}
				}
			}

			if (!compTraitsAdjustHediffsCache.TryGetValue(pawn, out CompTraitsAdjustHediffs traitComp))
			{
				traitComp = pawn.TryGetComp<CompTraitsAdjustHediffs>();
				compTraitsAdjustHediffsCache[pawn] = traitComp;
			}

			if (traitComp != null)
			{
				adjustHediffs.Add(traitComp);
			}
			return adjustHediffs;
		}
		public static float GetHediffResourceCapacityGainFor(Pawn pawn, HediffResourceDef hdDef)
		{
			float result = 0;
			var comps = GetAllAdjustHediffsComps(pawn);
			foreach (var comp in comps)
			{
				var resourceSettings = comp.ResourceSettings;
				if (resourceSettings != null)
				{
					foreach (var option in resourceSettings)
					{
						if (option.hediff == hdDef && option.maxResourceCapacityOffset != 0f)
						{
							if (option.qualityScalesCapacityOffset && comp.TryGetQuality(out QualityCategory qc))
							{
								result += (option.maxResourceCapacityOffset * GetQualityMultiplier(qc));
							}
							else
							{
								result += option.maxResourceCapacityOffset;
							}
						}
					}
				}
			}
			return result;
		}

		public static void RemoveExcessHediffResources(Pawn pawn, IAdjustResource adjuster)
        {
			List<HediffResourceDef> hediffResourcesToRemove = pawn.health.hediffSet.hediffs.OfType<HediffResource>()
				.Select(x => x.def).Where(x => adjuster.ResourceSettings?.Any(y => y.hediff == x) ?? false).ToList();

			var comps = GetAllAdjustHediffsComps(pawn);
			foreach (var comp in comps)
			{
				var resourceSettings = comp.ResourceSettings;
				if (comp.Parent != adjuster.Parent && resourceSettings != null)
				{
					foreach (var hediffOption in resourceSettings)
					{
						Log.Message($"adjuster.Parent: {adjuster.Parent}, comp.Parent: {comp.Parent}, hediffOption.hediff: {hediffOption.hediff}, hediffOption.addHediffIfMissing: {hediffOption.addHediffIfMissing}");
						if (hediffOption.addHediffIfMissing && hediffResourcesToRemove.Contains(hediffOption.hediff))
						{
							Log.Message("Can't remove " + hediffOption.hediff + " due to blocker: " + comp.Parent + ", adjuster: " + adjuster.Parent);
							hediffResourcesToRemove.Remove(hediffOption.hediff);
						}
					}
				}
			}

			foreach (var hediffDef in hediffResourcesToRemove)
			{
				var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
				if (hediff != null)
				{
					pawn.health.RemoveHediff(hediff);
				}
			}
		}

		public static void TryDropExcessHediffGears(Pawn pawn)
		{
			var comps = GetAllAdjustHediffsComps(pawn);
			foreach (var comp in comps)
			{
				var resourceSettings = comp.ResourceSettings;
				if (resourceSettings != null)
				{
					foreach (var hediffOption in resourceSettings)
					{
						var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
						if (hediff != null && hediffOption.dropIfOverCapacity && hediff.ResourceCapacity < 0)
						{
							comp.Drop();
							if (!hediffOption.overCapacityReasonKey.NullOrEmpty())
                            {
								Messages.Message(hediffOption.overCapacityReasonKey.Translate(pawn.Named("PAWN"), comp.Parent.Named("THING")), MessageTypeDefOf.CautionInput);
                            }
						}
					}
				}
			}
		}
		public static HediffResource AdjustResourceAmount(Pawn pawn, HediffResourceDef hdDef, float sevOffset, bool addHediffIfMissing)
		{
			if (sevOffset != 0f)
			{
				HediffResource firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(hdDef) as HediffResource;
				if (firstHediffOfDef != null)
				{
					firstHediffOfDef.ResourceAmount += sevOffset;
					return firstHediffOfDef;
				}
				else if (addHediffIfMissing)
				{
					firstHediffOfDef = HediffMaker.MakeHediff(hdDef, pawn) as HediffResource;
					pawn.health.AddHediff(firstHediffOfDef);
					firstHediffOfDef.ResourceAmount = sevOffset;
					return firstHediffOfDef;
				}
			}
			else if (pawn.health.hediffSet.GetFirstHediffOfDef(hdDef) is null && addHediffIfMissing)
			{
				var firstHediffOfDef = HediffMaker.MakeHediff(hdDef, pawn) as HediffResource;
				pawn.health.AddHediff(firstHediffOfDef);
				return firstHediffOfDef;
			}
			return null;
		}

		public static float GetQualityMultiplier(QualityCategory qualityCategory)
		{
			switch (qualityCategory)
			{
				case QualityCategory.Awful: return 0.8f;
				case QualityCategory.Poor: return 0.9f;
				case QualityCategory.Normal: return 1f;
				case QualityCategory.Good: return 1.15f;
				case QualityCategory.Excellent: return 1.3f;
				case QualityCategory.Masterwork: return 1.55f;
				case QualityCategory.Legendary: return 1.75f;
				default: return 1f;
			}
		}

		private static Dictionary<Pawn, HediffResourcesCache> hediffResourcesCache = new Dictionary<Pawn, HediffResourcesCache>();

		public static List<HediffResource> GetHediffResourcesFor(Pawn pawn)
        {
			if (hediffResourcesCache.TryGetValue(pawn, out HediffResourcesCache hediffResourceCache))
			{
				var hediffResources = hediffResourceCache.value;
				if (Find.TickManager.TicksGame > hediffResourceCache.updateTick + 30)
				{
					hediffResources = pawn.health.hediffSet.hediffs.OfType<HediffResource>().ToList();
					hediffResourceCache.Value = hediffResources;
				}
				return hediffResources;
			}
			else
			{
				var hediffResources = pawn.health.hediffSet.hediffs.OfType<HediffResource>().ToList();
				hediffResourcesCache[pawn] = new HediffResourcesCache(hediffResources);
				return hediffResources;
			}
		}
		public static bool IsUsableBy(Verb verb, out string disableReason)
		{
			if (verb.CasterPawn is Pawn pawn)
			{
				var hediffResources = GetHediffResourcesFor(pawn);
				foreach (var hediff in hediffResources)
				{
					if (hediff.def.shieldProperties?.cannotUseVerbType != null)
					{
						if (hediff.def.shieldProperties.cannotUseVerbType == VerbType.Both)
						{
							disableReason = "HRF.ShieldPreventAllAttack".Translate();
							return false;
						}
						else if (hediff.def.shieldProperties.cannotUseVerbType == VerbType.None)
						{
							continue;
						}
						else if (verb.IsMeleeAttack && hediff.def.shieldProperties.cannotUseVerbType == VerbType.Melee)
						{
							disableReason = "HRF.ShieldPreventMeleeAttack".Translate();
							return false;
						}
						else if (hediff.def.shieldProperties.cannotUseVerbType == VerbType.Range)
						{
							disableReason = "HRF.ShieldPreventRangeAttack".Translate();
							return false;
						}
					}
				}
				var verbProps = verb.verbProps as VerbResourceProps;
				if (verbProps != null && verbProps.resourceSettings != null)
                {
					foreach (var option in verbProps.resourceSettings)
					{
						var resourceHediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
						if (option.disableIfMissingHediff)
						{
							bool manaIsEmptyOrNull = resourceHediff != null ? resourceHediff.ResourceAmount <= 0 : true;
							if (manaIsEmptyOrNull)
							{
								disableReason = option.disableReason;
								return false;
							}
						}

						if (option.minimumResourcePerUse != -1f)
						{
							if (resourceHediff != null && resourceHediff.ResourceAmount < option.minimumResourcePerUse)
							{
								disableReason = option.disableReason;
								return false;
							}
						}
						if (option.disableAboveResource != -1f)
						{
							if (resourceHediff != null && resourceHediff.ResourceAmount > option.disableAboveResource)
							{
								disableReason = option.disableReason;
								return false;
							}
						}

						if (option.resourcePerUse < 0)
						{
							if (resourceHediff != null)
							{
								var num = resourceHediff.ResourceAmount - option.resourcePerUse;
								if (num < 0)
								{
									disableReason = option.disableReason;
									return false;
								}
							}
						}
					}
				}
			}

			disableReason = "";
			return true;
		}

		public static void DisableGizmo(Gizmo gizmo, string disableReason)
		{
			gizmo.Disable(disableReason);
		}

		public static IChargeResource GetCompChargeSourceFor(Pawn pawn, Projectile projectile)
		{
			var equipments = pawn.equipment?.AllEquipmentListForReading;
			if (equipments != null)
			{
				foreach (var equipment in equipments)
				{
					var chargeComp = equipment.GetComp<CompChargeResource>();
					if (chargeComp != null && (chargeComp.ProjectilesWithChargedResource?.ContainsKey(projectile) ?? false))
					{
						return chargeComp;
					}
				}
			}

			var apparels = pawn.apparel?.WornApparel?.ToList();
			if (apparels != null)
			{
				foreach (var apparel in apparels)
				{
					var chargeComp = apparel.GetComp<CompChargeResource>();
					if (chargeComp != null && (chargeComp.ProjectilesWithChargedResource?.ContainsKey(projectile) ?? false))
					{
						return chargeComp;
					}
				}
			}

			if (pawn.health?.hediffSet?.hediffs != null)
			{
				foreach (var hediff in pawn.health.hediffSet.hediffs)
				{
					var chargeComp = hediff.TryGetComp<HediffCompChargeResource>();
					if (chargeComp != null)
					{
						return chargeComp;
					}
				}
			}

			return null;
		}

		public static void ApplyResourceSettings(Verb verb, IResourceProps props)
        {
			if (props.TargetResourceSettings != null)
			{
				var target = verb.CurrentTarget.Thing as Pawn;
				if (target != null)
				{
					foreach (var option in props.TargetResourceSettings)
					{
						if (option.resetLifetimeTicks)
						{
							var targetHediff = target.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
							if (targetHediff != null)
							{
								targetHediff.duration = 0;
							}
						}
					}
				}

			}

			if (props.ResourceSettings != null)
			{
				var hediffResourceManage = Current.Game.GetComponent<HediffResourceManager>();

				var verbPostUseDelay = new List<int>();
				var verbPostUseDelayMultipliers = new List<float>();

				var hediffPostUse = new Dictionary<HediffResource, List<int>>();
				var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>();

				var disablePostUseString = "";
				var comps = GetAllAdjustHediffsComps(verb.CasterPawn);

				foreach (var option in props.ResourceSettings)
				{
					var hediffResource = AdjustResourceAmount(verb.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
					foreach (var comp in comps)
					{
						var compResourseSettings = comp.ResourceSettings?.FirstOrDefault(x => x.hediff == option.hediff);
						if (compResourseSettings != null)
						{
							if (option.postUseDelay != 0)
							{
								verbPostUseDelay.Add(option.postUseDelay);
								disablePostUseString += comp.DisablePostUse + "\n";
								if (compResourseSettings.postUseDelayMultiplier != 1)
								{
									verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier);
								}
							}
						}

						if (hediffResource != null && option.postUseDelay != 0)
						{
							if (hediffPostUse.ContainsKey(hediffResource))
							{
								hediffPostUse[hediffResource].Add(option.postUseDelay);
							}
							else
							{
								hediffPostUse[hediffResource] = new List<int> { option.postUseDelay };
							}
							if (compResourseSettings != null && compResourseSettings.postUseDelayMultiplier != 1)
							{
								if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource))
								{
									hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier);
								}
								else
								{
									hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier };
								}
							}
						}
					}
				}

				if (verbPostUseDelay.Any() && verbPostUseDelayMultipliers.Any())
				{
					foreach (var comp in comps)
					{
						comp.PostUseDelayTicks[verb] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average()) * verbPostUseDelayMultipliers.Average()), disablePostUseString);
					}
				}
				else if (verbPostUseDelay.Any())
				{
					foreach (var comp in comps)
					{
						comp.PostUseDelayTicks[verb] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average())), disablePostUseString);
					}
				}
				foreach (var hediffData in hediffPostUse)
				{
					if (hediffData.Key != null && hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList))
					{
						int newDelayTicks;
						if (hediffPostUseDelayMultipliers.TryGetValue(hediffData.Key, out List<float> hediffPostUseMultipliers) && hediffPostUseMultipliers.Any())
						{
							newDelayTicks = (int)(hediffPostUseList.Average() * hediffPostUseMultipliers.Average());
						}
						else
						{
							newDelayTicks = (int)(hediffPostUseList.Average());
						}
						if (hediffData.Key.CanHaveDelay(newDelayTicks))
						{
							hediffData.Key.AddDelay(newDelayTicks);
						}
					}
				}
			}
		}

		public static void ApplyChargeResource(ref float damageAmount, ChargeResources chargeResources)
        {
			Log.Message("chargeResources.chargeResources: " + chargeResources.chargeResources.Count());
			foreach (var chargeResource in chargeResources.chargeResources)
			{
				HRFLog.Message("1 instance - __result: " + damageAmount + " - hediffResource: " + chargeResource.chargeResource + " - compCharge.Props.damageScaling.HasValue: " + chargeResource.chargeSettings.damageScaling.HasValue);
				switch (chargeResource.chargeSettings.damageScaling)
				{
					case DamageScalingMode.Flat: DoFlatDamage(ref damageAmount, chargeResource.chargeResource, chargeResource.chargeSettings); break;
					case DamageScalingMode.Scalar: DoScalarDamage(ref damageAmount, chargeResource.chargeResource, chargeResource.chargeSettings); break;
					case DamageScalingMode.Linear: DoLinearDamage(ref damageAmount, chargeResource.chargeResource, chargeResource.chargeSettings); break;
					default: break;
				}
				HRFLog.Message("2 instance - result: " + damageAmount + " - hediffResource: " + chargeResource.chargeResource + " - compCharge.Props.damageScaling.HasValue: " + chargeResource.chargeSettings.damageScaling.HasValue);
			}
		}

		private static void DoFlatDamage(ref float __result, float resourceAmount, ChargeSettings chargeSettings)
		{
			var oldDamage = __result;
			__result = (int)(__result + (chargeSettings.damagePerCharge * (resourceAmount - chargeSettings.minimumResourcePerUse) / chargeSettings.resourcePerCharge));
			HRFLog.Message("Flat: old damage: " + oldDamage + " - new damage: " + __result);
		}
		private static void DoScalarDamage(ref float __result, float resourceAmount, ChargeSettings chargeSettings)
		{
			var oldDamage = __result;
			HRFLog.Message("chargeSettings.damagePerCharge: " + chargeSettings.damagePerCharge + " - resourceAmount: " + resourceAmount
				+ " - chargeSettings.minimumResourcePerUse: " + chargeSettings.minimumResourcePerUse + " - chargeSettings.resourcePerCharge: " + chargeSettings.resourcePerCharge);
			__result = (int)(__result * Mathf.Pow((1 + chargeSettings.damagePerCharge), (resourceAmount - chargeSettings.minimumResourcePerUse) / chargeSettings.resourcePerCharge));
			HRFLog.Message("Scalar: old damage: " + oldDamage + " - new damage: " + __result);
		}

		private static void DoLinearDamage(ref float __result, float resourceAmount, ChargeSettings chargeSettings)
		{
			var oldDamage = __result;
			__result = (int)(__result * (1 + (chargeSettings.damagePerCharge * (resourceAmount - chargeSettings.minimumResourcePerUse) / chargeSettings.resourcePerCharge)));
			HRFLog.Message("Linear: old damage: " + oldDamage + " - new damage: " + __result);
		}
	}
}