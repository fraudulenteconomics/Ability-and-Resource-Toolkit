﻿using RimWorld;
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
        public override void CompTick()
        {
            base.CompTick();
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

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref postUseDelayTicks, "postUseDelayTicks");
        }
    }

}
