using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ART
{
    public class Verb_ResourceTarget : Verb_ResourceBase
    {
        public override bool Available()
        {
            return this.ResourceProps?.TargetResourceSettings?.Count() > 0;
        }
        public override bool TryCastShot()
        {
            base.TryCastShot();
            var targetResourceSettings = this.ResourceProps.TargetResourceSettings;
            if (targetResourceSettings != null)
            {
                foreach (var hediffOption in targetResourceSettings)
                {
                    if (hediffOption.hediff != null)
                    {
                        Pawn target = currentTarget.Thing as Pawn;
                        if (target != null)
                        {
                            HediffResourceUtils.AdjustResourceAmount(target, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
                        }
                        if (hediffOption.effectRadius != -1f)
                        {
                            foreach (var cell in HediffResourceUtils.GetAllCellsAround(hediffOption, new TargetInfo(currentTarget.Cell, this.Caster.Map), CellRect.SingleCell(currentTarget.Cell)))
                            {
                                foreach (var pawn in cell.GetThingList(this.CasterPawn.Map).OfType<Pawn>())
                                {
                                    if (pawn != target)
                                    {
                                        if (hediffOption.affectsAllies && (pawn.Faction == this.CasterPawn.Faction || !pawn.Faction.HostileTo(this.CasterPawn.Faction)))
                                        {
                                            HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
                                        }
                                        else if (hediffOption.affectsEnemies && pawn.Faction.HostileTo(this.CasterPawn.Faction))
                                        {
                                            HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
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
