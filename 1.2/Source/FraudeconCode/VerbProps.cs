using HediffResourceFramework;
using Verse;

// ReSharper disable InconsistentNaming

namespace FraudeconCode
{
    public class VerbProps : VerbResourceProps
    {
        public bool alwaysGetChunks = true;
        public DamageDef cauterizeDamageDef;
        public float effectRadius;
        public float extinguishRadius;
        public float multishotRadius;
        public int multishotShots;
        public bool multishotTargetFriendly = false;
        public float yieldMultiplier = 1f;
    }
}