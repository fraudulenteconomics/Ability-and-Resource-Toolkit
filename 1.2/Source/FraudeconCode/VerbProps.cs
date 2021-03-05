using HediffResourceFramework;
using Verse;

// ReSharper disable InconsistentNaming

namespace FraudeconCode
{
    public class VerbProps : VerbResourceProps
    {
        public DamageDef cauterizeDamageDef;
        public float multishotRadius;
        public int multishotShots;
        public bool multishotTargetFriendly = false;
    }
}