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
        public bool canHarvestTrees;
        public DamageDef cauterizeDamageDef;
        public float chargeDamageAmount;
        public DamageDef chargeDamageDef;
        public GraphicData chargeGraphic;
        public float chargeWidth;
        public float effectRadius;
        public float extinguishRadius;
        public DamageDef landingDamageDef;
        public float landingEffectRadius;
        public float leatherYield = 0.0f;
        public float meatYield = 0.5f;
        public List<ThingDef> meteorMaterial;
        public float meteorSize;
        public float multishotRadius;
        public int multishotShots;
        public bool multishotTargetFriendly = false;
        public bool removeRoofs = true;
        public RotStage? requireRotStage;
        public float yieldMultiplier = 1f;
    }
}