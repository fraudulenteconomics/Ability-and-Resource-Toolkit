using HediffResourceFramework;
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
        public float effectRadius;
        public float extinguishRadius;
        public float leatherYield = 0.0f;
        public float meatYield = 0.5f;
        public float multishotRadius;
        public int multishotShots;
        public bool multishotTargetFriendly = false;
        public bool removeRoofs = true;
        public float yieldMultiplier = 1f;
    }
}