using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HediffResourceFramework
{
    public class StatBooster
    {
        public HediffResourceDef hediff;
        public bool preventUseIfHediffMissing;
        public string cannotUseMessageKey;

        public bool toggleResourceUse;
        public string toggleResourceGizmoTexPath;
        public string toggleResourceLabel;
        public string toggleResourceDesc;
        public string texPathToggledOn;
        public GlowerOptions glowerOptions;
        public bool glowOnlyPowered;
        public float resourcePerSecond = -1f;
        public float resourceOnComplete = -1f;
        public BodyPartDef applyToPart;
        public bool addHediffIfMissing;
        public bool qualityScalesResourcePerSecond;
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;

        public int increaseQuality = -1;
        public QualityCategory increaseQualityCeiling = QualityCategory.Legendary;

        public List<StatBonus> outputStatOffsets;
        public List<StatBonus> outputStatFactors;
        public bool defaultToggleState;
    }
}