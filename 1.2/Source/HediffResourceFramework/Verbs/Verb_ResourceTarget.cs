using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
    public class Verb_ResourceTarget : Verb_CastBase
    {
        public new VerbResourceProps verbProps => base.verbProps as VerbResourceProps;
        protected override bool TryCastShot()
        {
            if (this.currentTarget.Thing is Pawn target)
            {
                if (verbProps.targetResourceSettings != null)
                {
                    foreach (var hediffOption in verbProps.targetResourceSettings)
                    {
                        if (hediffOption.hediff != null)
                        {
                            Log.Message("Giving: " + target + " - " + hediffOption.hediff + " - " + hediffOption.resourcePerUse);
                            HediffResourceUtils.AdjustResourceAmount(target, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                        }
                    }
                }
            }
            return true;
        }
    }
}
