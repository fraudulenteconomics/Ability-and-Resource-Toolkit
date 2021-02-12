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
        public List<HediffAdjust> resourceSettings;

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
        public List<HediffAdjust> ResourceSettings => Props.resourceSettings;
        public string DisablePostUse => Props.disablePostUse;

        private Dictionary<Verb, VerbDisable> postUseDelayTicks;
        public Dictionary<Verb, VerbDisable> PostUseDelayTicks
        {
            get
            {
                if (postUseDelayTicks is null)
                {
                    postUseDelayTicks = new Dictionary<Verb, VerbDisable>();
                }
                return postUseDelayTicks;
            }
        }
        public void Register()
        {
            var gameComp = Current.Game.GetComponent<HediffResourceManager>();
            gameComp.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            var gameComp = Current.Game.GetComponent<HediffResourceManager>();
            gameComp.RegisterAdjuster(this);
        }
        public bool TryGetQuality(out QualityCategory qc)
        {
            qc = QualityCategory.Normal;
            return false;
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
            if (this.Pawn != null)
            {
                if (this.Pawn.IsHashIntervalTick(60))
                {
                    if (!this.PostUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
                    {
                        foreach (var option in Props.resourceSettings)
                        {
                            var hediffResource = this.Pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                            if (hediffResource != null && !hediffResource.CanGainResource)
                            {
                                continue;
                            }
                            else
                            {
                                float num = option.resourcePerSecond;
                                HediffResourceUtils.AdjustResourceAmount(this.Pawn, option.hediff, num, option.addHediffIfMissing);
                            }
                        }
                    }
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look(ref postUseDelayTicks, "postUseDelayTicks", LookMode.Reference, LookMode.Deep, ref verbKeys, ref verbDisablesValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var gameComp = Current.Game.GetComponent<HediffResourceManager>();
                gameComp.RegisterAdjuster(this);
            }
        }

        private List<Verb> verbKeys;
        private List<VerbDisable> verbDisablesValues;
    }
}