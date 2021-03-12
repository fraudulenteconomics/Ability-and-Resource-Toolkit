using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class HediffResouceDisable : IExposable
    {
        public HediffResouceDisable()
        {

        }

        public HediffResouceDisable(int delayTicks, string disableReason)
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
        public List<HediffOption> resourceSettings;

        public string disablePostUse;
    }
    public class CompAdjustHediffs : ThingComp, IAdjustResource
    {
        public CompProperties_AdjustHediffs Props => (CompProperties_AdjustHediffs)this.props;
        public Thing Parent => this.parent;
        public virtual List<HediffOption> ResourceSettings => Props.resourceSettings;
        public string DisablePostUse => Props.disablePostUse;

        private Dictionary<HediffResource, HediffResouceDisable> postUseDelayTicks;
        public Dictionary<HediffResource, HediffResouceDisable> PostUseDelayTicks
        {
            get
            {
                if (postUseDelayTicks is null)
                {
                    postUseDelayTicks = new Dictionary<HediffResource, HediffResouceDisable>();
                }
                return postUseDelayTicks;
            }
        }
        public bool TryGetQuality(out QualityCategory qc)
        {
            return this.parent.TryGetQuality(out qc);
        }

        public virtual void Drop()
        {

        }
        public void Register()
        {
            var gameComp = HediffResourceUtils.HediffResourceManager;
            gameComp.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            var gameComp = HediffResourceUtils.HediffResourceManager;
            gameComp.DeregisterAdjuster(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref postUseDelayTicks, "postUseDelayTicks", LookMode.Reference, LookMode.Deep, ref hediffResourceKeys, ref hediffResourceDisablesValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Register();
            }
        }

        private List<HediffResource> hediffResourceKeys;
        private List<HediffResouceDisable> hediffResourceDisablesValues;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Register();
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            Register();
        }

        public virtual void Notify_Removed()
        {
            Deregister();
        }

        public virtual void ResourceTick()
        {

        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }
}
