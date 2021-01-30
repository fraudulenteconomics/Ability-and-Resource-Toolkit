using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class HediffAdjust
    {
        public HediffAdjust()
        {

        }

        public HediffResourceDef hediff;
        public float resourcePerTick;
        public bool qualityScalesResourcePerTick;
        public float maxResourceCapacityOffset;
        public bool qualityScalesCapacityOffset;

        public bool disallowEquipIfHediffMissing;
        public string cannotEquipReason;
        public List<HediffDef> blackListHediffsPreventEquipping;
        public List<HediffDef> dropWeaponOrApparelIfBlacklistHediff;
        public string cannotEquipReasonIncompatible;

        public bool dropIfHediffMissing;
        public bool addHediffIfMissing = false;
    }

    public class VerbDisable : IExposable
    {
        public VerbDisable()
        {

        }

        public VerbDisable(int delayTicks, string disableReason)
        {
            this.delayTicks = delayTicks;
            this.disableReason = disableReason;
        }

        public int delayTicks;
        public string disableReason;

        public void ExposeData()
        {
            Scribe_Values.Look(ref delayTicks, "delayTicks");
            Scribe_Values.Look(ref disableReason, "disableReason");
        }
    }
    public class CompAdjustHediffs : ThingComp
    {
        public float delayTicks;
        public Dictionary<Verb, VerbDisable> postUseDelayTicks;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref delayTicks, "delayTicks", 0);
            Scribe_Collections.Look(ref postUseDelayTicks, "postUseDelayTicks", LookMode.Reference, LookMode.Deep, ref verbKeys, ref verbDisablesValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var gameComp = Current.Game.GetComponent<HediffResourceManager>();
                gameComp.RegisterComp(this);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var gameComp = Current.Game.GetComponent<HediffResourceManager>();
            gameComp.RegisterComp(this);
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            var gameComp = Current.Game.GetComponent<HediffResourceManager>();
            gameComp.RegisterComp(this);
        }

        public virtual void ResourceTick()
        {
        }

        private List<Verb> verbKeys;
        private List<VerbDisable> verbDisablesValues;
    }
}
