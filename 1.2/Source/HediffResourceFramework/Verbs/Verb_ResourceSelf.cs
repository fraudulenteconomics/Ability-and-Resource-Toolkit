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
    public class Verb_ResourceSelf : Verb_ResourceBase
    {
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
            base.TryCastShot();
            if (this.CasterPawn != null)
            {
                if (verbProps.targetResourceSettings != null)
                {
                    foreach (var hediffOption in verbProps.targetResourceSettings)
                    {
                        HRFLog.Message("Giving: " + this.CasterPawn + " - " + hediffOption.hediff + " - " + hediffOption.resourcePerUse);
                        HediffResourceUtils.AdjustResourceAmount(this.CasterPawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                        if (hediffOption.effectRadius != -1f)
                        {
                            foreach (var cell in HediffResourceUtils.GetAllCellsAround(hediffOption, this.CasterPawn))
                            {
                                foreach (var pawn in cell.GetThingList(this.CasterPawn.Map).OfType<Pawn>())
                                {
                                    if (pawn != this.CasterPawn)
                                    {
                                        HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
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
