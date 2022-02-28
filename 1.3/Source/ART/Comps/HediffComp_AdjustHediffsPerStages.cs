﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ART
{
    public class HediffCompProperties_AdjustHediffsPerStages : HediffCompProperties
	{
        public string disablePostUse;
        public HediffCompProperties_AdjustHediffsPerStages()
		{
			compClass = typeof(HediffComp_AdjustHediffsPerStages);
		}
	}

	public class HediffComp_AdjustHediffsPerStages : HediffComp, IAdjustResource
	{
        public HediffCompProperties_AdjustHediffsPerStages Props => (HediffCompProperties_AdjustHediffsPerStages)this.props;
        public Thing Parent => this.Pawn;
        public List<ResourceProperties> ResourceSettings
        {
            get
            {
                if (this.parent.CurStage is HediffStageResource hediffResourceStage)
                {
                    return hediffResourceStage.resourceSettings;
                }
                return null;
            }
        }
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
            ARTManager.Instance.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            ARTManager.Instance.DeregisterAdjuster(this);
        }
        public bool TryGetQuality(out QualityCategory qc)
        {
            qc = QualityCategory.Normal;
            return false;
        }

        public void Drop()
        {
            this.Pawn.health.RemoveHediff(this.parent);
            Notify_Removed();
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
        public void ResourceTick()
        {
            var pawn = this.Pawn;
            if (pawn != null)
            {
                    var resourceSetting = ResourceSettings;
                if (resourceSetting != null)
                {
                    foreach (var resourceProperties in resourceSetting)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(resourceProperties.hediff) as HediffResource;
                        if (hediffResource != null && (PostUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame) 
                            || !hediffResource.CanGainResource))
                        {
                            continue;
                        }
                        else
                        {
                            float num = resourceProperties.GetResourceGain(this);
                            HediffResourceUtils.AdjustResourceAmount(pawn, resourceProperties.hediff, num, resourceProperties.addHediffIfMissing, resourceProperties, resourceProperties.applyToPart);
                        }
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
                Register();
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