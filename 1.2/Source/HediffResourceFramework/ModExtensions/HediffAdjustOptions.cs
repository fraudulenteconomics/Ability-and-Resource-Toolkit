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
        public BodyPartDef applyToPart;
        public string disableReason;
        public float effectRadius = -1f;

        public bool worksThroughWalls;
        public bool affectsAllies;
        public bool affectsEnemies;

        public bool resetLifetimeTicks;
        public int postUseDelay;

        public float resourcePerSecond;
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

        public bool addToCaster;
        public bool removeOutsideArea;

        public bool disallowEquipIfOverCapacity;
        public bool dropIfOverCapacity;
        public string overCapacityReasonKey;

        public List<HediffDef> removeHediffsOnDrop;
        public bool requiredForUse = true;

    }
}
