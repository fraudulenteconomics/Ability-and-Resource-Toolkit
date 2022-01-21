using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{

    public class CompProperties_AdjustHediffs : CompProperties
    {
        public List<HediffOption> resourceSettings;

        public string disablePostUse;
    }
    public class CompAdjustHediffs : ThingComp, IAdjustResource, IResourceStorage
    {
        public CompProperties_AdjustHediffs Props => (CompProperties_AdjustHediffs)this.props;
        public Thing Parent => this.parent;
        public virtual List<HediffOption> ResourceSettings => Props.resourceSettings;
        public string DisablePostUse => Props.disablePostUse;
        public bool IsStorageFor(HediffOption hediffOption, out ResourceStorage resourceStorages)
        {
            resourceStorages = GetResourceStoragesFor(hediffOption).FirstOrDefault()?.Item3;
            return resourceStorages != null;
        }

        private Dictionary<int, ResourceStorage> resourceStorages;
        public Dictionary<int, ResourceStorage> ResourceStorages
        {
            get
            {
                if (resourceStorages is null)
                {
                    resourceStorages = new Dictionary<int, ResourceStorage>();
                }

                if (Props.resourceSettings != null)
                {
                    for (var i = 0; i < Props.resourceSettings.Count; i++)
                    {
                        if (Props.resourceSettings[i].maxResourceStorageAmount > 0 && !resourceStorages.ContainsKey(i))
                        {
                            resourceStorages[i] = new ResourceStorage(Props.resourceSettings[i]);
                            if (Props.resourceSettings[i].initialResourceAmount != 0)
                            {
                                resourceStorages[i].ResourceAmount = Props.resourceSettings[i].initialResourceAmount;
                            }
                        }
                    }
                }

                return resourceStorages;
            }
        }

        public IEnumerable<Tuple<CompAdjustHediffs, HediffOption, ResourceStorage>> GetResourceStoragesFor(HediffResourceDef hediffDef)
        {
            var resourceStorages = ResourceStorages;
            if (Props.resourceSettings != null)
            {
                for (var i = 0; i < Props.resourceSettings.Count; i++)
                {
                    if (Props.resourceSettings[i].maxResourceStorageAmount > 0 && Props.resourceSettings[i].hediff == hediffDef)
                    {
                        yield return new Tuple<CompAdjustHediffs, HediffOption, ResourceStorage>(this, Props.resourceSettings[i], resourceStorages[i]);
                    }
                }
            }
        }
        public IEnumerable<Tuple<CompAdjustHediffs, HediffOption, ResourceStorage>> GetResourceStoragesFor(HediffOption hediffOption)
        {
            var resourceStorages = ResourceStorages;
            if (Props.resourceSettings != null)
            {
                for (var i = 0; i < Props.resourceSettings.Count; i++)
                {
                    if (Props.resourceSettings[i].maxResourceStorageAmount > 0 && Props.resourceSettings[i].refillOnlyInnerStorage && Props.resourceSettings[i] == hediffOption)
                    {
                        yield return new Tuple<CompAdjustHediffs, HediffOption, ResourceStorage>(this, Props.resourceSettings[i], resourceStorages[i]);
                    }
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder(base.CompInspectStringExtra());
            var resourceStorages = ResourceStorages;
            if (Props.resourceSettings != null)
            {
                for (var i = 0; i < Props.resourceSettings.Count; i++)
                {
                    if (Props.resourceSettings[i].maxResourceStorageAmount > 0)
                    {
                        foreach (var resourceStorage in GetResourceStoragesFor(Props.resourceSettings[i]).ToList())
                        {
                            if ((Find.TickManager.TicksGame - resourceStorage.Item3.lastChargedTick) <= 60)
                            {
                                sb.AppendLine("HRF.StoredAmountCharged".Translate(resourceStorage.Item3.ResourceAmount, Props.resourceSettings[i].maxResourceStorageAmount));
                            }
                            else
                            {
                                sb.AppendLine("HRF.StoredAmount".Translate(resourceStorage.Item3.ResourceAmount, Props.resourceSettings[i].maxResourceStorageAmount));
                            }
                        }
                    }
                }
            }
            return sb.ToString().TrimEndNewlines();
        }

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
            HediffResourceManager.Instance.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            HediffResourceManager.Instance.DeregisterAdjuster(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref postUseDelayTicks, "postUseDelayTicks", LookMode.Reference, LookMode.Deep, ref hediffResourceKeys, ref hediffResourceDisablesValues);
            Scribe_Collections.Look(ref resourceStorages, "resourceStorages", LookMode.Value, LookMode.Deep, ref intKeys, ref resourceStorageValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Register();
                var resourceSettings = Props.resourceSettings;
                if (resourceStorages is null)
                {
                    resourceStorages = new Dictionary<int, ResourceStorage>();
                }
                foreach (var data in resourceStorages)
                {
                    if (resourceSettings.Count - 1 <= data.Key)
                    {
                        data.Value.hediffOption = resourceSettings[data.Key];
                    }
                }
            }
        }

        private List<HediffResource> hediffResourceKeys;
        private List<HediffResouceDisable> hediffResourceDisablesValues;

        private List<int> intKeys;
        private List<ResourceStorage> resourceStorageValues;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Register();
            if (!respawningAfterLoad)
            {
                TryForbidAfterPlacing();
            }

        }

        public void TryForbidAfterPlacing()
        {
            foreach (var thing in this.parent.Position.GetThingList(this.parent.Map))
            {
                var comp = thing.TryGetComp<CompBuildingStorageAdjustHediffs>();
                if (comp != null)
                {
                    foreach (var resourceSettings in comp.ResourceSettings)
                    {
                        foreach (var thisResourceSettings in this.ResourceSettings)
                        {
                            if (resourceSettings.hediff == thisResourceSettings.hediff && resourceSettings.forbidItemsWhenCharging)
                            {
                                if (comp.compPower != null && !comp.compPower.PowerOn)
                                {
                                    continue;
                                }
                                else
                                {
                                    this.parent.SetForbidden(true);
                                }
                            }
                        }
                    }
                }
            }
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

        }

        public ThingDef GetStuff()
        {
            return this.parent.Stuff;
        }
    }
}
