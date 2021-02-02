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
        public float resourcePerCharge = -1f;
        public float damagePerCharge = -1f;
        public float minimumResourcePerUse = -1f;
        public DamageScalingMode? damageScaling;
        public CompProperties_ChargeResource()
        {
            this.compClass = typeof(CompChargeResource);
        }
    }
    public class CompChargeResource : CompAdjustHediffs
    {
        public Dictionary<Projectile, float> projectilesWithChargedResource = new Dictionary<Projectile, float>();
        public CompProperties_ChargeResource Props
        {
            get
            {
                return (CompProperties_ChargeResource)this.props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref projectilesWithChargedResource, "projectilesWithChargedResource", LookMode.Reference, LookMode.Value, ref projectileValues, ref projectileVlaues);
        }

        private List<Projectile> projectileValues;
        private List<float> projectileVlaues;
    }
}
