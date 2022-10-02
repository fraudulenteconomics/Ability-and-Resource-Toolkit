using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ART
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
}
