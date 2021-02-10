using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_ApparelAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_ApparelAdjustHediffs()
        {
            this.compClass = typeof(CompApparelAdjustHediffs);
        }
    }

    public class CompApparelAdjustHediffs : CompAdjustHediffs
    {
        public Apparel Apparel => this.parent as Apparel;
        public override void Notify_Removed()
        {
            base.Notify_Removed();
            if (Apparel.Wearer != null)
            {
                List<HediffResourceDef> hediffResourcesToRemove = Apparel.Wearer.health.hediffSet.hediffs.OfType<HediffResource>()
                    .Select(x => x.def).Where(x => Props.resourceSettings.Any(y => y.hediff == x)).ToList();
                var apparels = Apparel.Wearer.apparel.WornApparel;
                if (apparels != null)
                {
                    foreach (var ap in apparels)
                    {
                        if (ap != Apparel)
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
                }
                
                var equipments = Apparel.Wearer.equipment?.AllEquipmentListForReading;
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
                    var hediff = Apparel.Wearer.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                    if (hediff != null)
                    {
                        Apparel.Wearer.health.RemoveHediff(hediff);
                    }
                }
            }

        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            this.Notify_Removed();
            base.PostDestroy(mode, previousMap);
        }
        public override void ResourceTick()
        {
            base.ResourceTick();
            if (Apparel.Wearer != null)
            {
                if (Apparel.Wearer.IsHashIntervalTick(60))
                {
                    if (!this.postUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
                    {
                        foreach (var option in Props.resourceSettings)
                        {
                            var hediffResource = Apparel.Wearer.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                            if (hediffResource != null && !hediffResource.CanGainResource)
                            {
                                continue;
                            }
                            else
                            {
                                float num = option.resourcePerSecond;
                                if (option.qualityScalesResourcePerSecond && Apparel.TryGetQuality(out QualityCategory qc))
                                {
                                    num *= HediffResourceUtils.GetQualityMultiplier(qc);
                                }
                                Log.Message(this + " - " + Find.TickManager.TicksGame + " - apparel adjust hediff: " + option.hediff + " - num: " + num);
                                HediffResourceUtils.AdjustResourceAmount(Apparel.Wearer, option.hediff, num, option.addHediffIfMissing);
                            }
                        }
                    }
                }
            }
        }
    }
}
