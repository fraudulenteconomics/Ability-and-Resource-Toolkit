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

        public override void Drop()
        {
            base.Drop();
            var pawn = Apparel.Wearer;
            if (pawn != null)
            {
                if (pawn.Map != null)
                {
                    pawn.apparel.TryDrop(Apparel);
                }
                else
                {
                    pawn.inventory.TryAddItemNotForSale(Apparel);
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
            var pawn = Apparel.Wearer;
            if (pawn != null)
            {
                if (!this.PostUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
                {
                    foreach (var option in Props.resourceSettings)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
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
                            HediffResourceUtils.AdjustResourceAmount(pawn, option.hediff, num, option.addHediffIfMissing);
                        }
                    }
                }
            }
        }
    }
}
