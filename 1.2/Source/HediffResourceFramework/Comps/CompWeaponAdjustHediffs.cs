using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_WeaponAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_WeaponAdjustHediffs()
        {
            this.compClass = typeof(CompWeaponAdjustHediffs);
        }
    }
    public class CompWeaponAdjustHediffs : CompAdjustHediffs
    {

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
            List<HediffResourceDef> hediffResourcesToRemove = Equipment.PrimaryVerb.CasterPawn.health.hediffSet.hediffs.OfType<HediffResource>()
                .Select(x => x.def).Where(x => Props.resourceSettings.Any(y => y.hediff == x)).ToList();
            
            var equipments = Equipment.PrimaryVerb.CasterPawn.equipment.AllEquipmentListForReading;
            if (equipments != null)
            {
                foreach (var eq in equipments)
                {
                    if (eq != Equipment.parent)
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
            }
            
            var apparels = Equipment.PrimaryVerb.CasterPawn.apparel.WornApparel;
            if (apparels != null)
            {
                foreach (var ap in apparels)
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

            foreach (var hediffDef in hediffResourcesToRemove)
            {
                var hediff = Equipment.PrimaryVerb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                {
                    Equipment.PrimaryVerb.CasterPawn.health.RemoveHediff(hediff);
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
            if (this.parent is ThingWithComps equipment && CompEquippable.PrimaryVerb.CasterPawn != null)
            {
                if (CompEquippable.PrimaryVerb.CasterPawn.IsHashIntervalTick(60))
                {
                    if (!this.postUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
                    {
                        foreach (var option in Props.resourceSettings)
                        {
                            var hediffResource = CompEquippable.PrimaryVerb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                            if (hediffResource != null && !hediffResource.CanGainResource)
                            {
                                continue;
                            }
                            else 
                            {
                                float num = option.resourcePerSecond;
                                num *= 0.00333333341f;
                                if (option.qualityScalesResourcePerSecond && equipment.TryGetQuality(out QualityCategory qc))
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


    }
}
