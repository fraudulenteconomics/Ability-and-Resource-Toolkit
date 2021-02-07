using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class VerbResourceProps : VerbProperties
    {
        public HediffResourceDef hediffDefResource;
        public float resourceAmount;
        public bool addHediffIfMissing;
    }
    public class Verb_ResourceTarget : Verb_Shoot
    {
        public new VerbResourceProps verbProps => base.verbProps as VerbResourceProps;
        protected override bool TryCastShot()
        {
            var result = base.TryCastShot();
            if (result && this.currentTarget.Thing is Pawn target)
            {
                Log.Message("Giving: " + target + " - " + verbProps.hediffDefResource + " - " + verbProps.resourceAmount);
                HediffResourceUtils.AdjustResourceAmount(target, verbProps.hediffDefResource, verbProps.resourceAmount, verbProps.addHediffIfMissing);
            }
            return result;
        }
    }
}
