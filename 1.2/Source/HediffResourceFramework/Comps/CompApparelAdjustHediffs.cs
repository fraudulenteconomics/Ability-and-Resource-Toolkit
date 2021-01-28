using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_ApparelAdjustHediffs : CompProperties
    {
        public List<HediffAdjust> hediffOptions;

        public CompProperties_ApparelAdjustHediffs()
        {
            this.compClass = typeof(CompApparelAdjustHediffs);
        }
    }

    public class CompApparelAdjustHediffs : CompAdjustHediffs
    {
        public CompProperties_ApparelAdjustHediffs Props
        {
            get
            {
                return (CompProperties_ApparelAdjustHediffs)this.props;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame >= this.delayTicks && this.parent is Apparel apparel && apparel.Wearer != null)
            {
                if (apparel.Wearer.IsHashIntervalTick(60))
                {
                    foreach (var option in Props.hediffOptions)
                    {
                        float num = option.resourcePerSecond;
                        num *= 0.00333333341f;
                        if (option.qualityScalesResourcePerSecond && apparel.TryGetQuality(out QualityCategory qc))
                        {
                            num *= HediffResourceUtils.GetQualityMultiplier(qc);
                        }
                        num /= 3.33f;
                        HediffResourceUtils.AdjustResourceAmount(apparel.Wearer, option.hediff, num, option.addHediffIfMissing);
                    }
                }
            }
        }
    }

}
