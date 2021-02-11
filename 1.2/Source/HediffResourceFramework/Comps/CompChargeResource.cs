using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class ChargeSettings : IExposable
    {
        public HediffResourceDef hediffResource;
        public float resourcePerCharge = -1f;
        public float damagePerCharge = -1f;
        public float minimumResourcePerUse = -1f;
        public DamageScalingMode? damageScaling;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref hediffResource, "hediffResource");
            Scribe_Values.Look(ref resourcePerCharge, "resourcePerCharge");
            Scribe_Values.Look(ref damagePerCharge, "damagePerCharge");
            Scribe_Values.Look(ref minimumResourcePerUse, "minimumResourcePerUse");
            Scribe_Values.Look(ref damageScaling, "damageScaling");
        }
    }

    public class ChargeResource : IExposable
    {
        public ChargeResource()
        {

        }

        public ChargeResource(float chargeResource, ChargeSettings chargeSettings)
        {
            this.chargeResource = chargeResource;
            this.chargeSettings = chargeSettings;
        }

        public float chargeResource;
        public ChargeSettings chargeSettings;
        public void ExposeData()
        {
            Scribe_Values.Look(ref chargeResource, "chargeResource");
            Scribe_Deep.Look(ref chargeSettings, "chargeSettings");
        }
    }

    public class ChargeResources : IExposable
    {
        public ChargeResources()
        {

        }

        public List<ChargeResource> chargeResources = new List<ChargeResource>();
        public void ExposeData()
        {
            Scribe_Collections.Look(ref chargeResources, "chargeResources", LookMode.Deep);
        }
    }

    public class CompProperties_ChargeResource : CompProperties
    {
        public CompProperties_ChargeResource()
        {
            this.compClass = typeof(CompChargeResource);
        }
    }
    public class CompChargeResource : ThingComp
    {
        public Dictionary<Projectile, ChargeResources> projectilesWithChargedResource = new Dictionary<Projectile, ChargeResources>();
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
            Scribe_Collections.Look(ref projectilesWithChargedResource, "projectilesWithChargedResource", LookMode.Reference, LookMode.Deep, ref projectileValues, ref projectileVlaues);
        }

        private List<Projectile> projectileValues;
        private List<ChargeResources> projectileVlaues;
    }
}
