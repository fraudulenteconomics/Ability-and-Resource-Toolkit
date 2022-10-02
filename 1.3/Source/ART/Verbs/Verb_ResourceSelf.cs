using RimWorld;
using System.Linq;
using Verse;

namespace ART
{
    public class Verb_ResourceSelf : Verb_ResourceBase
    {
        public override bool Available()
        {
            var targetResourceSettings = ResourceProps.TargetResourceSettings;
            if (targetResourceSettings != null)
            {
                foreach (var resourceProperties in targetResourceSettings)
                {
                    if (CasterPawn.health.hediffSet.GetFirstHediffOfDef(resourceProperties.hediff) is HediffResource hediffResource && hediffResource.ResourceAmount == hediffResource.ResourceCapacity)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override bool TryCastShot()
        {
            base.TryCastShot();
            if (CasterPawn != null)
            {
                var targetResourceSettings = ResourceProps.TargetResourceSettings;

                if (targetResourceSettings != null)
                {
                    foreach (var resourceProperties in targetResourceSettings)
                    {
                        ARTLog.Message("Giving: " + CasterPawn + " - " + resourceProperties.hediff + " - " + resourceProperties.resourcePerUse);
                        if (resourceProperties.affectsAllies || (!resourceProperties.affectsAllies && !resourceProperties.affectsEnemies))
                        {
                            Utils.AdjustResourceAmount(CasterPawn, resourceProperties.hediff, resourceProperties.resourcePerUse,
                                resourceProperties.addHediffIfMissing, resourceProperties, resourceProperties.applyToPart);
                        }
                        if (resourceProperties.effectRadius > 0)
                        {
                            foreach (var cell in Utils.GetAllCellsAround(resourceProperties, new TargetInfo(CasterPawn.Position, CasterPawn.Map), CellRect.SingleCell(CasterPawn.Position)))
                            {
                                foreach (var pawn in cell.GetThingList(CasterPawn.Map).OfType<Pawn>())
                                {
                                    if (pawn != CasterPawn)
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
