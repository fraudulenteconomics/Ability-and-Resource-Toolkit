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
            var targetResourceSettings = this.ResourceProps.TargetResourceSettings;
            if (targetResourceSettings != null)
            {
                foreach (var hediffOption in targetResourceSettings)
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
                var targetResourceSettings = this.ResourceProps.TargetResourceSettings;

                if (targetResourceSettings != null)
                {
                    foreach (var hediffOption in targetResourceSettings)
                    {
                        HRFLog.Message("Giving: " + this.CasterPawn + " - " + hediffOption.hediff + " - " + hediffOption.resourcePerUse);
                        HediffResourceUtils.AdjustResourceAmount(this.CasterPawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
                        if (hediffOption.effectRadius != -1f)
                        {
                            foreach (var cell in HediffResourceUtils.GetAllCellsAround(hediffOption, new TargetInfo(this.CasterPawn.Position, this.CasterPawn.Map), CellRect.SingleCell(this.CasterPawn.Position)))
                            {
                                foreach (var pawn in cell.GetThingList(this.CasterPawn.Map).OfType<Pawn>())
                                {
                                    if (pawn != this.CasterPawn)
                                    {
                                        HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
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
