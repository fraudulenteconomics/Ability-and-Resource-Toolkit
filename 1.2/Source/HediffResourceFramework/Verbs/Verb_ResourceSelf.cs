using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class Verb_ResourceSelf : Verb_Shoot
    {
        public new VerbResourceProps verbProps => base.verbProps as VerbResourceProps;
        protected override bool TryCastShot()
        {
            var result = base.TryCastShot();
            if (result && this.CasterPawn != null)
            {
                Log.Message("Giving: " + this.CasterPawn + " - " + verbProps.hediffDefResource + " - " + verbProps.resourceAmount);
                HediffResourceUtils.AdjustResourceAmount(this.CasterPawn, verbProps.hediffDefResource, verbProps.resourceAmount, verbProps.addHediffIfMissing);
            }
            return result;
        }
    }
}
