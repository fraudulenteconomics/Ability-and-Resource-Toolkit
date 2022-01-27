using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HediffResourceFramework
{

	public class HediffCompProperties_AdjustHediffs : HediffCompProperties
	{
        public List<HediffOption> resourceSettings;

        public string disablePostUse;
        public HediffCompProperties_AdjustHediffs()
		{
			compClass = typeof(HediffComp_AdjustHediffs);
		}
	}

	public class HediffComp_AdjustHediffs : HediffComp, IAdjustResource
	{
        public HediffCompProperties_AdjustHediffs Props => (HediffCompProperties_AdjustHediffs)this.props;
        public Thing Parent => this.Pawn;
        public List<HediffOption> ResourceSettings => Props.resourceSettings;
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
        public Pawn PawnHost => this.Pawn;
        public void Register()
        {
            HediffResourceManager.Instance.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            HediffResourceManager.Instance.DeregisterAdjuster(this);
        }
        public bool TryGetQuality(out QualityCategory qc)
        {
            qc = QualityCategory.Normal;
            return false;
        }

        public void Drop()
        {
            this.Pawn.health.RemoveHediff(this.parent);
        }
        public void Notify_Removed()
        {
            Deregister();
            if (this.Pawn != null)
            {
                HediffResourceUtils.RemoveExcessHediffResources(this.Pawn, this);
            }
        }

        public override void CompPostPostRemoved()
        {
            this.Notify_Removed();
            base.CompPostPostRemoved();
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Register();
        }
        public virtual void ResourceTick()
        {
            var pawn = this.Pawn;
            if (pawn != null)
            {
                foreach (var hediffOption in Props.resourceSettings)
                {
                    var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
                    if (hediffResource != null && (PostUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame)
                        || !hediffResource.CanGainResource))
                    {
                        continue;
                    }
                    else
                    {
                        float num = hediffOption.GetResourceGain(this);
                        HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, num, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
                    }
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look(ref postUseDelayTicks, "postUseDelayTicks", LookMode.Reference, LookMode.Deep, ref hediffResourceKeys, ref hediffResourceDisablesValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Register();
            }
        }

        public void Update()
        {

        }

        public ThingDef GetStuff()
        {
            return null;
        }

        private List<HediffResource> hediffResourceKeys;
        private List<HediffResouceDisable> hediffResourceDisablesValues;
    }
}