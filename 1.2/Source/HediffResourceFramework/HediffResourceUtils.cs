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
		public static void AdjustResourceAmount(Pawn pawn, HediffResourceDef hdDef, float sevOffset, bool addHediffIfMissing)
		{
			if (sevOffset != 0f)
			{
				HediffResource firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(hdDef) as HediffResource;
				if (firstHediffOfDef != null)
				{
					firstHediffOfDef.ResourceAmount += sevOffset;
				}
				else if (sevOffset > 0f && addHediffIfMissing)
				{
					firstHediffOfDef = HediffMaker.MakeHediff(hdDef, pawn) as HediffResource;
					firstHediffOfDef.ResourceAmount = sevOffset;
					pawn.health.AddHediff(firstHediffOfDef);
				}
			}
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
				var compWeapon = verb.EquipmentSource.GetComp<CompWeaponAdjustHediffs>();
				if (compWeapon?.postUseDelayTicks != null && compWeapon.postUseDelayTicks.TryGetValue(verb, out VerbDisable value) && value.delayTicks > Find.TickManager.TicksGame)
				{
					disableReason = value.disableReason;
					return false;
				}
				var compApparel = verb.EquipmentSource.GetComp<CompApparelAdjustHediffs>();
				if (compApparel?.postUseDelayTicks != null && compApparel.postUseDelayTicks.TryGetValue(verb, out VerbDisable value2) && value2.delayTicks > Find.TickManager.TicksGame)
				{
					disableReason = value2.disableReason;
					return false;
				}
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
							if (option.resourcePerUse != 0f)
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
