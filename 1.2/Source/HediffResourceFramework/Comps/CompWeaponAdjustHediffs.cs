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

        public override void ResourceTick()
        {
            base.ResourceTick();
            if (Find.TickManager.TicksGame >= this.delayTicks && this.parent is ThingWithComps equipment && CompEquippable.PrimaryVerb.CasterPawn != null)
            {
                if (CompEquippable.PrimaryVerb.CasterPawn.IsHashIntervalTick(60))
                {
                    Log.Message(" - Comp - foreach (var option in Props.hediffOptions) - 4", true);
                    foreach (var option in Props.hediffOptions)
                    {
                        float num = option.resourcePerTick;
                        Log.Message(" - Comp - num *= 0.00333333341f; - 6", true);
                        num *= 0.00333333341f;
                        if (option.qualityScalesResourcePerTick && equipment.TryGetQuality(out QualityCategory qc))
                        {
                            Log.Message(" - Comp - num *= HediffResourceUtils.GetQualityMultiplier(qc); - 8", true);
                            num *= HediffResourceUtils.GetQualityMultiplier(qc);
                        }
                        num /= 3.33f;
                        Log.Message(" - Comp - HediffResourceUtils.AdjustResourceAmount(CompEquippable.PrimaryVerb.CasterPawn, option.hediff, num, option.addHediffIfMissing); - 10", true);
                        HediffResourceUtils.AdjustResourceAmount(CompEquippable.PrimaryVerb.CasterPawn, option.hediff, num, option.addHediffIfMissing);
                    }
                }
            }
        }
    }
}
