using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_ChargeResource : CompProperties
    {
        public HediffResourceDef hediffResource;
        public float resourcePerCharge;
        public float damagePerCharge;
        public int minimumResourcePerUse;
        public DamageScalingMode? damageScaling;
        public CompProperties_ChargeResource()
        {
            this.compClass = typeof(CompChargeResource);
        }
    }
    public class CompChargeResource : CompAdjustHediffs
    {
        public CompProperties_ChargeResource Props
        {
            get
            {
                return (CompProperties_ChargeResource)this.props;
            }
        }
    }
}
