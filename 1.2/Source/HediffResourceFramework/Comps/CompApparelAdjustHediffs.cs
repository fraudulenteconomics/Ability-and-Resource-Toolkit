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
        public List<HediffOption> hediffOptions;

        public CompProperties_ApparelAdjustHediffs()
        {
            this.compClass = typeof(CompApparelAdjustHediffs);
        }
    }
    public class CompApparelAdjustHediffs : ThingComp
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
            if (this.parent is Apparel apparel && apparel.Wearer != null)
            {
                if (apparel.Wearer.IsHashIntervalTick(200))
                {
                    foreach (var option in Props.hediffOptions)
                    {
                        float num = option.resourceOffset;
                        num *= 0.00333333341f;
                        if (option.qualityScalesResourceOffset && apparel.TryGetQuality(out QualityCategory qc))
                        {
                            num *= HediffResourceUtils.GetQualityMultiplier(qc);
                        }
                        HediffResourceUtils.AdjustResourceAmount(apparel.Wearer, option.hediff, num, option);
                    }
                }
            }
        }
    }

}
