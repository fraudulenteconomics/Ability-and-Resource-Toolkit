using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public enum DamageScalingMode
    {
        Scalar,
        Flat
    }
    public class HediffOption
    {
        public HediffOption()
        {

        }

        public HediffResourceDef hediff;
        public int verbIndex = -1;
        public string verbLabel;
        
        public float resourcePerUse;
        public bool disableIfMissingHediff;
        public float minimumResourcePerUse = -1f;

        public bool addHediffIfMissing = false;
        public string disableReason;

        public int postUseDelay;
    }
    public class HediffAdjustOptions : DefModExtension
    {
        public List<HediffOption> hediffOptions;
    }
}
