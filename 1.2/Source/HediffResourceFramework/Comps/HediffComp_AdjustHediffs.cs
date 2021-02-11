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
			compClass = typeof(HediffComp_ResourcePerSecond);
		}
	}

	public class HediffComp_AdjustHediffs : HediffComp, IAdjustResource
	{
        public HediffCompProperties_AdjustHediffs Props => (HediffCompProperties_AdjustHediffs)this.props;
        public Thing Parent => this.Pawn;
        public List<HediffAdjust> ResourceSettings => Props.resourceSettings;
        public string DisablePostUse => Props.disablePostUse;

        private Dictionary<Verb, VerbDisable> postUseDelayTicks;
        public Dictionary<Verb, VerbDisable> PostUseDelayTicks => postUseDelayTicks;
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
                List<HediffResourceDef> hediffResourcesToRemove = this.Pawn.health.hediffSet.hediffs.OfType<HediffResource>()
                    .Select(x => x.def).Where(x => Props.resourceSettings.Any(y => y.hediff == x)).ToList();
                var apparels = this.Pawn.apparel.WornApparel;
                if (apparels != null)
                {
                    foreach (var ap in apparels)
                    {
                        var comp = ap.TryGetComp<CompApparelAdjustHediffs>();
                        if (comp?.Props?.resourceSettings != null)
                        {
                            foreach (var hediffOption in comp.Props.resourceSettings)
                            {
                                if (hediffResourcesToRemove.Contains(hediffOption.hediff))
                                {
                                    hediffResourcesToRemove.Remove(hediffOption.hediff);
                                }
                            }
                        }
                    }
                }

                var equipments = this.Pawn.equipment?.AllEquipmentListForReading;
                if (equipments != null)
                {
                    foreach (var eq in equipments)
                    {
                        var comp = eq.TryGetComp<CompWeaponAdjustHediffs>();
                        if (comp?.Props?.resourceSettings != null)
                        {
                            foreach (var hediffOption in comp.Props.resourceSettings)
                            {
                                if (hediffResourcesToRemove.Contains(hediffOption.hediff))
                                {
                                    hediffResourcesToRemove.Remove(hediffOption.hediff);
                                }
                            }
                        }
                    }
                }

                foreach (var hediffDef in hediffResourcesToRemove)
                {
                    var hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                    if (hediff != null)
                    {
                        this.Pawn.health.RemoveHediff(hediff);
                    }
                }
            }

        }

        public override void CompPostPostRemoved()
        {
            this.Notify_Removed();
            base.CompPostPostRemoved();
        }

        public void ResourceTick()
        {
            if (this.Pawn != null)
            {
                if (this.Pawn.IsHashIntervalTick(60))
                {
                    if (!this.postUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
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