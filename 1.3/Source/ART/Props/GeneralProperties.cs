using System.Collections.Generic;
using Verse;

namespace ART
{
    public class GeneralProperties
    {
        public int ticksPerEffect;
        public float effectRadius;
        public bool worksThroughWalls;
        public List<string> blacklistTradeTags;
        public List<string> whitelistTradeTags;
        public SoundDef soundOnEffect;
        public bool CanApplyOn(Thing thing)
        {
            if (thing.def.tradeTags.NullOrEmpty() is false)
            {
                if (blacklistTradeTags.NullOrEmpty() is false && blacklistTradeTags.Any(x => thing.def.tradeTags.Contains(x)))
                {
                    return false;
                }
                if (whitelistTradeTags.NullOrEmpty() is false && !whitelistTradeTags.Any(x => thing.def.tradeTags.Contains(x)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}