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
    public class Verb_ResourceTarget : Verb_ResourceBase
    {
        protected override bool TryCastShot()
        {
            base.TryCastShot();
            if (this.currentTarget.Thing is Pawn target)
            {
                if (verbProps.targetResourceSettings != null)
                {
                    foreach (var hediffOption in verbProps.targetResourceSettings)
                    {
                        if (hediffOption.hediff != null)
                        {
                            HRFLog.Message("Giving: " + target + " - " + hediffOption.hediff + " - " + hediffOption.resourcePerUse);
                            HediffResourceUtils.AdjustResourceAmount(target, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                            if (hediffOption.effectRadius != -1f)
                            {
                                foreach (var cell in HediffResourceUtils.GetAllCellsAround(hediffOption, target))
                                {
                                    foreach (var pawn in cell.GetThingList(this.CasterPawn.Map).OfType<Pawn>())
                                    {
                                        if (pawn != target)
                                        {
                                            HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
