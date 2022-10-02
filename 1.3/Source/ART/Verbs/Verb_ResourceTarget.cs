using RimWorld;
using System.Linq;
using Verse;

namespace ART
{
    public class Verb_ResourceTarget : Verb_ResourceBase
    {
        public override bool Available()
        {
            return ResourceProps?.TargetResourceSettings?.Count() > 0;
        }
        public override bool TryCastShot()
        {
            base.TryCastShot();
            var targetResourceSettings = ResourceProps.TargetResourceSettings;
            if (targetResourceSettings != null)
            {
                foreach (var resourceProperties in targetResourceSettings)
                {
                    if (resourceProperties.hediff != null)
                    {
                        var target = currentTarget.Thing as Pawn;
                        if (target != null)
                        {
                            Utils.AdjustResourceAmount(target, resourceProperties.hediff, resourceProperties.resourcePerUse, resourceProperties.addHediffIfMissing,
                                resourceProperties, resourceProperties.applyToPart);
                        }
                        if (resourceProperties.effectRadius > 0)
                        {
                            foreach (var cell in Utils.GetAllCellsAround(resourceProperties, new TargetInfo(currentTarget.Cell, Caster.Map), CellRect.SingleCell(currentTarget.Cell)))
                            {
                                foreach (var pawn in cell.GetThingList(CasterPawn.Map).OfType<Pawn>())
                                {
                                    if (pawn != target)
                                    {
                                        if (resourceProperties.affectsAllies && (pawn.Faction == CasterPawn.Faction || !pawn.Faction.HostileTo(CasterPawn.Faction)))
                                        {
                                            Utils.AdjustResourceAmount(pawn, resourceProperties.hediff, resourceProperties.resourcePerUse, resourceProperties.addHediffIfMissing,
                                                resourceProperties, resourceProperties.applyToPart);
                                        }
                                        else if (resourceProperties.affectsEnemies && pawn.Faction.HostileTo(CasterPawn.Faction))
                                        {
                                            Utils.AdjustResourceAmount(pawn, resourceProperties.hediff, resourceProperties.resourcePerUse, resourceProperties.addHediffIfMissing,
                                                resourceProperties, resourceProperties.applyToPart);
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
