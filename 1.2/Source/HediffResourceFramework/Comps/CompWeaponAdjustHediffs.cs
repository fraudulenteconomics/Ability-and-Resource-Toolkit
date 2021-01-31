using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_WeaponAdjustHediffs : CompProperties
    {
        public List<HediffAdjust> hediffOptions;

        public string disableWeaponPostUse;
        public CompProperties_WeaponAdjustHediffs()
        {
            this.compClass = typeof(CompWeaponAdjustHediffs);
        }
    }
    public class CompWeaponAdjustHediffs : CompAdjustHediffs
    {
        public CompProperties_WeaponAdjustHediffs Props
        {
            get
            {
                return (CompProperties_WeaponAdjustHediffs)this.props;
            }
        }

        private CompEquippable compEquippable;
        private CompEquippable CompEquippable
        {
            get
            {
                if (compEquippable is null)
                {
                    compEquippable = this.parent.GetComp<CompEquippable>();
                }
                return compEquippable;
            }
        }

        public CompEquippable Equipment => this.parent.GetComp<CompEquippable>();
        public override void Notify_Removed()
        {
            base.Notify_Removed();
            List<HediffResourceDef> hediffResourcesToRemove = Equipment.PrimaryVerb.CasterPawn.health.hediffSet.hediffs.OfType<HediffResource>().Select(x => x.def).ToList();
            
            var equipments = Equipment.PrimaryVerb.CasterPawn.equipment.AllEquipmentListForReading;
            if (equipments != null)
            {
                foreach (var eq in equipments)
                {
                    if (eq != Equipment.parent)
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
            }

            var apparels = Equipment.PrimaryVerb.CasterPawn.apparel.WornApparel;
            if (apparels != null)
            {
                foreach (var ap in apparels)
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

            foreach (var hediffDef in hediffResourcesToRemove)
            {
                var hediff = Equipment.PrimaryVerb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                {
                    Equipment.PrimaryVerb.CasterPawn.health.RemoveHediff(hediff);
                }
            }
        }
        public override void ResourceTick()
        {
            base.ResourceTick();
            if (Find.TickManager.TicksGame >= this.delayTicks && this.parent is ThingWithComps equipment && CompEquippable.PrimaryVerb.CasterPawn != null)
            {
                if (CompEquippable.PrimaryVerb.CasterPawn.IsHashIntervalTick(60))
                {
                    foreach (var option in Props.hediffOptions)
                    {
                        float num = option.resourcePerTick;
                        num *= 0.00333333341f;
                        if (option.qualityScalesResourcePerTick && equipment.TryGetQuality(out QualityCategory qc))
                        {
                            num *= HediffResourceUtils.GetQualityMultiplier(qc);
                        }
                        num /= 3.33f;
                        HediffResourceUtils.AdjustResourceAmount(CompEquippable.PrimaryVerb.CasterPawn, option.hediff, num, option.addHediffIfMissing);
                    }
                }
            }
        }
    }
}
