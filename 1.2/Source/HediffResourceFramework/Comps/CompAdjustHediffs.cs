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
        public bool addHediffIfMissing = false;
        public float postDamageDelayMultiplier = 1f;
        public float postUseDelayMultiplier = 1f;

        public float radius;
        public bool worksThroughWalls;
        public bool affectsAllies;
        public bool affectsEnemies;
        public bool addToCaster;
        public bool removeOutsideArea;
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

    public class CompProperties_AdjustHediffs : CompProperties
    {
        public List<HediffAdjust> resourceSettings;

        public string disablePostUse;

        public string savePrefix;

    }
    public class CompAdjustHediffs : ThingComp
    {
        public CompProperties_AdjustHediffs Props
        {
            get
            {
                return (CompProperties_AdjustHediffs)this.props;
            }
        }

        public Dictionary<Verb, VerbDisable> postUseDelayTicks;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref postUseDelayTicks, "postUseDelayTicks", LookMode.Reference, LookMode.Deep, ref verbKeys, ref verbDisablesValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var gameComp = Current.Game.GetComponent<HediffResourceManager>();
                gameComp.RegisterComp(this);
            }
        }

        private List<Verb> verbKeys;
        private List<VerbDisable> verbDisablesValues;

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

        public virtual void Notify_Removed()
        {

        }
        public virtual void ResourceTick()
        {

        }
    }
}
