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
        Flat,
        Linear
    }
    public class HediffOption
    {
        public HediffOption()
        {

        }

        public HediffResourceDef hediff;

        public float resourcePerUse;
        public bool disableIfMissingHediff;
        public float minimumResourcePerUse = -1f;
        public float disableAboveResource = -1f;
        public bool addHediffIfMissing = false;
        public string disableReason;

        public int extendLifetime = -1;
        public int postUseDelay;
    }
}
