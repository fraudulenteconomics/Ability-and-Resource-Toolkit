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
        public CompProperties_ApparelAdjustHediffs Props
        {
            get
            {
                return (CompProperties_ApparelAdjustHediffs)this.props;
            }
        }

        public Apparel Apparel => this.parent as Apparel;
        public override void Notify_Removed()
        {
            base.Notify_Removed();
            List<HediffResourceDef> hediffResourcesToRemove = Apparel.Wearer.health.hediffSet.hediffs.OfType<HediffResource>().Select(x => x.def).ToList();
            var apparels = Apparel.Wearer.apparel.WornApparel;
            if (apparels != null)
            {
                foreach (var ap in apparels)
                {
                    if (ap != Apparel)
                    {
                        var comp = ap.TryGetComp<CompApparelAdjustHediffs>();
                        if (comp?.Props?.hediffOptions != null)
                        {
                            foreach (var hediffOption in comp.Props.hediffOptions)
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

            var equipments = Apparel.Wearer.equipment.AllEquipmentListForReading;
            if (equipments != null)
            {
                foreach (var eq in equipments)
                {
                    var comp = eq.TryGetComp<CompWeaponAdjustHediffs>();
                    if (comp?.Props?.hediffOptions != null)
                    {
                        foreach (var hediffOption in comp.Props.hediffOptions)
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
        public override void ResourceTick()
        {
            base.ResourceTick();
            if (Find.TickManager.TicksGame >= this.delayTicks && Apparel.Wearer != null)
            {
                if (Apparel.Wearer.IsHashIntervalTick(60))
                {
                    if (!this.postUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
                    {
                        foreach (var option in Props.hediffOptions)
                        {
                            float num = option.resourcePerTick;
                            num *= 0.00333333341f;
                            if (option.qualityScalesResourcePerTick && Apparel.TryGetQuality(out QualityCategory qc))
                            {
                                num *= HediffResourceUtils.GetQualityMultiplier(qc);
                            }
                            num /= 3.33f;
                            HediffResourceUtils.AdjustResourceAmount(Apparel.Wearer, option.hediff, num, option.addHediffIfMissing);
                        }
                    }

                }
            }
        }
    }

}
