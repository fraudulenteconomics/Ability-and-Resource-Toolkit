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
        public override bool Available()
        {
            if (verbProps.targetResourceSettings != null)
            {
                foreach (var hediffOption in verbProps.targetResourceSettings)
                {
                    var hediffResource = this.CasterPawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
                    if (hediffResource != null && hediffResource.ResourceAmount == hediffResource.ResourceCapacity)
                    {
                        return false;
                    }
                }

            }
            return true;
        }
        protected override bool TryCastShot()
        {
            var result = base.TryCastShot();
            if (result && this.CasterPawn != null)
            {
                if (verbProps.targetResourceSettings != null)
                {
                    foreach (var hediffOption in verbProps.targetResourceSettings)
                    {
                        Log.Message("Giving: " + this.CasterPawn + " - " + hediffOption.hediff + " - " + hediffOption.resourcePerUse);
                        HediffResourceUtils.AdjustResourceAmount(this.CasterPawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                    }
                }
            }
            return result;
        }
    }
}
