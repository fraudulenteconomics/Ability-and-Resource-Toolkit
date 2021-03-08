using System.Collections.Generic;
using HediffResourceFramework;
using RimWorld;
using Verse;

// ReSharper disable InconsistentNaming

namespace FraudeconCode
{
    public class VerbProps : VerbResourceProps
    {
        public bool alwaysGetChunks = true;
        public HediffDef applyHediff;
        public int avatarDuration = 12000;
        public float blinkDuration;
        public bool canHarvestTrees;
        public DamageDef cauterizeDamageDef;
        public float chargeDamageAmount;
        public DamageDef chargeDamageDef;
        public GraphicData chargeGraphic;
        public float chargeWidth;
        public List<MinCountDef> effectCount;
        public float effectDamageAmount;
        public DamageDef effectDamageDef;
        public float effectDuration;
        public HediffDef effectHediff;
        public float effectRadius;
        public float effectRate;
        public float extinguishRadius;
        public HediffDef feedbackHediff;
        public DamageDef landingDamageDef;
        public float landingEffectRadius;
        public float leatherYield = 0.0f;
        public int maxTargets;
        public float meatYield = 0.5f;
        public List<ThingDef> meteorMaterial;
        public float meteorSize;
        public float multishotRadius;
        public int multishotShots;
        public bool multishotTargetFriendly = false;
        public bool removeRoofs = true;
        public RotStage? requireRotStage;
        public PawnKindDef servantDef;
        public int servantDuration;
        public float yieldMultiplier = 1f;
    }

    public struct MinCountDef
    {
        public int minCount;
        public string spawnDef;
        public string pawnKindDef;
    }
}