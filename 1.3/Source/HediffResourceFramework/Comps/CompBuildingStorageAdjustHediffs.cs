using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_BuildingStorageAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_BuildingStorageAdjustHediffs()
        {
            this.compClass = typeof(CompBuildingStorageAdjustHediffs);
        }
    }
    public class CompBuildingStorageAdjustHediffs : CompAdjustHediffs
    {
        public IEnumerable<CompAdjustHediffs> StoredItems
        {
            get
            {
                foreach (var cell in this.parent.OccupiedRect())
                {
                    foreach (var thing in cell.GetThingList(this.parent.Map))
                    {
                        if (thing != this.parent && thing.TryGetCompAdjustHediffs(out var comp))
                        {
                            yield return comp;
                        }
                    }
                }
            }
        }
        public override void ResourceTick()
        {
            base.ResourceTick();
            var comps = StoredItems;
            foreach (var comp in comps)
            {
                foreach (var hediffOption in Props.resourceSettings)
                {
                    float num = hediffOption.resourcePerSecond;
                    if (hediffOption.qualityScalesResourcePerSecond && this.parent.TryGetQuality(out QualityCategory qc))
                    {
                        num *= HediffResourceUtils.GetQualityMultiplier(qc);
                    }

                    var storage = comp.GetResourceStoragesFor(hediffOption.hediff).FirstOrDefault();
                    if (storage != null)
                    {
                        storage.Item3.ResourceAmount += num;
                        if (hediffOption.unforbidWhenEmpty && storage.Item3.ResourceAmount <= 0 ||
                            hediffOption.unforbidWhenFull && storage.Item3.ResourceAmount >= storage.Item3.ResourceCapacity)
                        {
                            storage.Item1.parent.SetForbidden(false);
                        }
                    }
                }
            }
        }
    }
}
