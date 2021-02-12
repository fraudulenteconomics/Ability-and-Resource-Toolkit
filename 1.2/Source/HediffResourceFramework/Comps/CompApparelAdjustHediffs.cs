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
            if (Apparel.Wearer != null)
            {
                HediffResourceUtils.RemoveExcessHediffResources(Apparel.Wearer, this);
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
                    if (!this.PostUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
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
                                HediffResourceUtils.AdjustResourceAmount(Apparel.Wearer, option.hediff, num, option.addHediffIfMissing);
                            }
                        }
                    }
                }
            }
        }
    }
}
