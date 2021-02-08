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
			if (hediffComp?.Props.hediffOptions != null)
			{
				foreach (var option in hediffComp.Props.hediffOptions)
				{
					if (option.disallowEquipIfHediffMissing)
					{
						var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
						if (hediff is null)
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

		public static bool CanEquip(Pawn pawn, ThingWithComps weapon, out string reason)
		{
			var hediffComp = weapon.GetComp<CompWeaponAdjustHediffs>();
			if (hediffComp?.Props.hediffOptions != null)
			{
				foreach (var option in hediffComp.Props.hediffOptions)
				{
					if (option.disallowEquipIfHediffMissing)
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
		public static bool IsHediffUser(this Pawn pawn)
		{
			return pawn.health.hediffSet.hediffs.Any(x => x is HediffResource);
		}
		public static float GetHediffResourceCapacityGainFor(Pawn pawn, HediffResourceDef hdDef)
		{
			float result = 0;
			if (pawn.apparel?.WornApparel != null)
			{
				foreach (var ap in pawn.apparel.WornApparel)
				{
					var hediffComp = ap.GetComp<CompApparelAdjustHediffs>();
					if (hediffComp?.Props.hediffOptions != null)
					{
						foreach (var option in hediffComp.Props.hediffOptions)
						{
							if (option.hediff == hdDef && option.maxResourceCapacityOffset != 0f)
							{
								if (option.qualityScalesCapacityOffset && ap.TryGetQuality(out QualityCategory qc))
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
							if (option.hediff == hdDef && option.maxResourceCapacityOffset != 0f)
							{
								if (option.qualityScalesCapacityOffset && equipment.TryGetQuality(out QualityCategory qc))
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
			}
			return result;
		}
		public static HediffResource AdjustResourceAmount(Pawn pawn, HediffResourceDef hdDef, float sevOffset, bool addHediffIfMissing)
		{
			Log.Message("Adjusting resource: " + hdDef);
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
		public static bool VerbMatches(Verb verb, HediffOption option)
		{
			if (!option.verbLabel.NullOrEmpty())
			{
				if (verb.ReportLabel == option.verbLabel)
				{
					return true;
				}
				return false;
			}

			if (option.verbIndex != -1)
			{
				if (verb.EquipmentSource.def.Verbs.IndexOf(verb.verbProps) == option.verbIndex)
				{
					return true;
				}
				return false;
			}
			return true;
		}
		public static bool IsUsableBy(Verb verb, out string disableReason)
		{
			if (verb.CasterIsPawn && verb.EquipmentSource != null)
			{
				var hediffResources = verb.CasterPawn.health.hediffSet.hediffs.OfType<HediffResource>().ToHashSet();
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
				var options = verb.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
				{
					foreach (var option in options.hediffOptions)
					{
						if (VerbMatches(verb, option))
						{
							var resourceHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
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
			}

			disableReason = "";
			return true;
		}

		public static void DisableGizmo(Gizmo gizmo, string disableReason)
		{
			gizmo.Disable(disableReason);
		}
	}
}