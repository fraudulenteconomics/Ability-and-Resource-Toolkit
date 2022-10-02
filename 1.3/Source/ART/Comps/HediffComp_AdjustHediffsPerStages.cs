using RimWorld;
using System.Collections.Generic;
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
        public HediffCompProperties_AdjustHediffsPerStages Props => (HediffCompProperties_AdjustHediffsPerStages)props;
        public Thing Parent => Pawn;
        public List<ResourceProperties> ResourceSettings
        {
            get
            {
                if (parent.CurStage is HediffStageResource hediffResourceStage)
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

        public Pawn PawnHost => Pawn;

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
            Pawn.health.RemoveHediff(parent);
            Notify_Removed();
        }
        public void Notify_Removed()
        {
            Deregister();
            if (Pawn != null)
            {
                Utils.RemoveExcessHediffResources(Pawn, this);
            }
        }

        public override void CompPostPostRemoved()
        {
            Notify_Removed();
            base.CompPostPostRemoved();
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Register();
        }
        public void ResourceTick()
        {
            var pawn = Pawn;
            if (pawn != null)
            {
                var resourceSetting = ResourceSettings;
                if (resourceSetting != null)
                {
                    foreach (var resourceProperties in resourceSetting)
                    {
                        if (pawn.health.hediffSet.GetFirstHediffOfDef(resourceProperties.hediff) is HediffResource hediffResource && ((PostUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame))
                            || !hediffResource.CanGainResource))
                        {
                            continue;
                        }
                        else
                        {
                            float num = resourceProperties.GetResourceGain(this);
                            Utils.AdjustResourceAmount(pawn, resourceProperties.hediff, num, resourceProperties.addHediffIfMissing, resourceProperties, resourceProperties.applyToPart);
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

        public bool IsStorageFor(ResourceProperties resourceProperties, out ResourceStorage resourceStorages)
        {
            resourceStorages = null;
            return false;
        }

        private List<HediffResource> hediffResourceKeys;
        private List<HediffResouceDisable> hediffResourceDisablesValues;
    }
}