using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ART
{

    public class ResourceProperties : GeneralProperties
    {
        public HediffResourceDef hediff;
        public float resourcePerUse;
        public bool disableIfMissingHediff;
        public float minimumResourcePerUse = -1f;
        public float disableAboveResource = -1f;
        public bool addHediffIfMissing = false;
        public BodyPartDef applyToPart;
        public string disableReasonKey;

        public bool affectsAllies;
        public bool affectsEnemies;

        public bool resetLifetimeTicks;
        public int postUseDelay;

        public float resourcePerSecond;
        public float fixedResourceAmount = -1;
        public bool canRefillStorage = false;

        public float adjustTargetResource;
        public IntRange delayTargetOnDamage = IntRange.zero;
        public float severityPerDamage;
        public bool applyToDamagedPart;

        public bool qualityScalesResourcePerSecond;
        public float maxResourceCapacityOffset;
        public bool qualityScalesCapacityOffset;

        public bool disallowEquipIfHediffMissing;
        public string cannotEquipReason;
        public List<HediffDef> blackListHediffsPreventEquipping;
        public List<HediffDef> dropWeaponOrApparelIfBlacklistHediff;
        public string cannotEquipReasonIncompatible;

        public bool dropIfHediffMissing;
        public float postDamageDelayMultiplier = 1f;
        public float postUseDelayMultiplier = 1f;

        public bool removeOutsideArea;
        public bool disallowEquipIfOverCapacity;
        public bool dropIfOverCapacity;
        public string overCapacityReasonKey;

        public List<HediffDef> removeHediffsOnDrop;
        public bool requiredForUse = true;

        public bool refillOnlyInnerStorage;
        public float maxResourceStorageAmount;

        public bool destroyWhenFull;
        public bool destroyWhenEmpty;
        public float initialResourceAmount;
        public bool dropWhenFull;
        public bool dropWhenEmpty;
        public bool disallowEquipWhenEmpty;
        public bool unforbidWhenFull;
        public bool unforbidWhenEmpty;
        public bool forbidItemsWhenCharging;

        public float resourceCapacityFactor = 1f;
        public float resourcePerSecondFactor = 1f;
        public float resourceCapacityOffset;
        public float resourcePerSecondOffset;

        public FloatRange? temperatureRange;
        public bool activeAboveTemperature;
        public bool activeBelowTemperature;

        public FloatRange? lightRange;
        public bool activeAboveLight;
        public bool activeBelowLight;
        public void AdjustResource(Pawn pawn, IAdjustResource source, Dictionary<HediffResource, HediffResouceDisable> postUseDelayTicks)
        {
            if (CanAdjustResource(pawn, postUseDelayTicks))
            {
                float num = this.GetResourceGain();
                if (source.IsStorageFor(this, out var resourceStorage))
                {
                    if (addHediffIfMissing && pawn.health.hediffSet.GetFirstHediffOfDef(hediff) is null)
                    {
                        BodyPartRecord bodyPartRecord = null;
                        if (applyToPart != null)
                        {
                            bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == applyToPart);
                        }
                        var hediff = HediffMaker.MakeHediff(this.hediff, pawn, bodyPartRecord) as HediffResource;
                        pawn.health.AddHediff(hediff);
                    }
                    float toRefill = Mathf.Min(num, resourceStorage.ResourceCapacity - resourceStorage.ResourceAmount);
                    resourceStorage.ResourceAmount += toRefill;
                }
                else
                {
                    Utils.AdjustResourceAmount(pawn, hediff, num, addHediffIfMissing, this, applyToPart);
                }
            }
        }

        public bool CanAdjustResource(Pawn pawn, Dictionary<HediffResource, HediffResouceDisable> postUseDelayTicks)
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediff) is HediffResource hediffResource &&
                ((postUseDelayTicks != null && postUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame))
                || !hediffResource.CanGainResource))
            {
                return false;
            }

            if (temperatureRange.HasValue)
            {
                float curTemperature = pawn.AmbientTemperature;
                if (!RangeAllowed(activeAboveTemperature, activeBelowTemperature, temperatureRange.Value, curTemperature))
                {
                    return false;
                }
            }

            if (lightRange.HasValue && pawn.Map != null)
            {
                float curLight = pawn.Map.glowGrid.GameGlowAt(pawn.Position);
                if (!RangeAllowed(activeAboveLight, activeBelowLight, lightRange.Value, curLight))
                {
                    return false;
                }
            }
            return true;
        }

        public bool RangeAllowed(bool activeAbove, bool activeBelow, FloatRange range, float value)
        {
            if (activeAbove && activeBelow)
            {
                if (range.Includes(value))
                {
                    return false;
                }
            }
            else if (activeAbove)
            {
                if (value <= range.max)
                {
                    return false;
                }
            }
            else if (activeBelow && value >= range.min)
            {
                return false;
            }
            else if (!range.Includes(value))
            {
                return false;
            }
            return true;
        }
    }
}
