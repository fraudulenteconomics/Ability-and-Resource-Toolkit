using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class HediffOption
    {
        public HediffOption()
        {

        }

        public HediffResourceDef hediff;
        public float resourceOffset;
        public bool qualityScalesResourceOffset;
        public float maxResourceCapacityOffset;
        public bool qualityScalesCapacityOffset;
        public int verbIndex = -1;
        public string verbLabel;
        public bool disableOnEmptyOrMissingHediff;
        public float minimumResourceCastRequirement = -1f;
        public bool disallowEquippingIfEmptyNullHediff;
        public string cannotEquipReason;
        public List<HediffDef> blackListHediffsPreventEquipping;
        public List<HediffDef> dropWeaponOrApparelIfBlacklistHediff;
        public string cannotEquipReasonIncompatible;

        public bool dropApparelIfEmptyNullHediff;
        public bool dropWeaponIfEmptyNullHediff;
        public bool addHediffIfMissing = false;
        public string disableReason;

        public int postUseDelay;

    }
    public class HediffAdjustOptions : DefModExtension
    {
        public List<HediffOption> hediffOptions;
    }
}
