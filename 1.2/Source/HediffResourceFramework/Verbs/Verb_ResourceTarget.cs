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
        public List<HediffOption> hediffOptions;
    }
    public class Verb_ResourceTarget : Verb_Shoot
    {
        public new VerbResourceProps verbProps => base.verbProps as VerbResourceProps;
        protected override bool TryCastShot()
        {
            var result = base.TryCastShot();
            if (result && this.currentTarget.Thing is Pawn target)
            {
                if (verbProps.hediffOptions != null)
                {
                    foreach (var hediffOption in verbProps.hediffOptions)
                    {
                        if (hediffOption.hediff != null)
                        {
                            Log.Message("Giving: " + target + " - " + hediffOption.hediff + " - " + hediffOption.resourcePerUse);
                            HediffResourceUtils.AdjustResourceAmount(target, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                        }
                    }
                }
            }
            return result;
        }
    }
}
