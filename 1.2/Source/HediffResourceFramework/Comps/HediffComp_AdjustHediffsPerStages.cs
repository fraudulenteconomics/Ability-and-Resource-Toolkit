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

	public class HediffCompProperties_AdjustHediffsPerStages : HediffCompProperties
	{
        public List<List<HediffAdjust>> resourceSettingsPerStages;

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
        public List<HediffAdjust> ResourceSettings
        {
            get
            {
                var stageInd = this.parent.CurStageIndex;
                if (Props.resourceSettingsPerStages.Count > stageInd)
                {
                    return Props.resourceSettingsPerStages[stageInd];
                }
                return null;
            }
        }
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
        public void ResourceTick()
        {
            var pawn = this.Pawn;
            if (pawn != null)
            {
                if (!this.PostUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
                {
                    var resourceSetting = ResourceSettings;
                    if (resourceSetting != null)
                    {
                        foreach (var option in resourceSetting)
                        {
                            var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                            if (hediffResource != null && !hediffResource.CanGainResource)
                            {
                                continue;
                            }
                            else
                            {
                                float num = option.resourcePerSecond;
                                HediffResourceUtils.AdjustResourceAmount(pawn, option.hediff, num, option.addHediffIfMissing);
                            }
                        }
                    }
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            PostUseDelayTicks.RemoveAll(x => x.Key.CasterPawn.DestroyedOrNull());
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