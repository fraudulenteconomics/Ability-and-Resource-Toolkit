using RimWorld;
using System.Linq;
using Verse;

namespace ART
{
    public class CompProperties_AdjustHediffsArea : CompProperties_AdjustHediffs
    {
        public bool stackEffects;
        public int stackMax = -1;
        public CompProperties_AdjustHediffsArea()
        {
            compClass = typeof(CompAdjustHediffsArea);
        }
    }

    public class CompAdjustHediffsArea : CompAdjustHediffs, IAdjustResouceInArea
    {
        private CompPowerTrader powerComp;
        private CompRefuelable fuelComp;
        private CompFlickable flickableComp;

        public new CompProperties_AdjustHediffsArea Props => props as CompProperties_AdjustHediffsArea;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            fuelComp = parent.GetComp<CompRefuelable>();
            flickableComp = parent.GetComp<CompFlickable>();
        }
        public override void ResourceTick()
        {
            ARTLog.Message("Active: " + Active + " - " + this);
            ARTLog.Message("this.parent.Map: " + parent.Map);
            ARTLog.Message(" IsEnabled(): " + IsEnabled());
            if (Active)
            {
                foreach (var option in Props.resourceSettings)
                {
                    float num = Utils.GetResourceGain(option, this);
                    var affectedCells = Utils.GetAllCellsAround(option, parent, parent.OccupiedRect());
                    foreach (var cell in affectedCells)
                    {
                        foreach (var pawn in cell.GetThingList(parent.Map).OfType<Pawn>())
                        {
                            if (pawn == parent && !option.affectsSelf)
                            {
                                continue;
                            }

                            if (option.affectsAllies && (pawn.Faction == parent.Faction || !pawn.Faction.HostileTo(parent.Faction)))
                            {
                                AppendResource(pawn, option, num);
                            }
                            else if (option.affectsEnemies && pawn.Faction.HostileTo(parent.Faction))
                            {
                                AppendResource(pawn, option, num);
                            }
                        }
                    }
                }
            }
        }

        public bool Active => parent.Map != null && IsEnabled();
        public override Pawn PawnHost => null;
        public bool IsEnabled()
        {
            if (flickableComp != null && !flickableComp.SwitchIsOn)
            {
                return false;
            }
            if (powerComp != null && !powerComp.PowerOn)
            {
                return false;
            }
            if (fuelComp != null && !fuelComp.HasFuel)
            {
                return false;
            }
            return true;
        }
        public bool InRadiusFor(IntVec3 cell, HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                var option = GetFirstResourcePropertiesFor(hediffResourceDef);
                if (option != null && cell.DistanceTo(parent.Position) <= option.effectRadius)
                {
                    return true;
                }
            }
            return false;
        }
        public void AppendResource(Pawn pawn, ResourceProperties resourceProperties, float num)
        {
            ARTLog.Message("AppendResource: " + pawn);
            var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(resourceProperties.hediff) as HediffResource;
            if (hediffResource != null && !hediffResource.CanGainResource)
            {
                return;
            }
            else
            {
                if (hediffResource != null)
                {
                    var amplifiers = hediffResource.GetAmplifiersFor(resourceProperties.hediff);
                    if (!Props.stackEffects)
                    {
                        if (amplifiers.Count() > 0 && amplifiers.Any(x => x != this))
                        {
                            return;
                        }
                    }
                    if (Props.stackMax != -1)
                    {
                        if (amplifiers.Count() >= Props.stackMax && !amplifiers.Contains(this))
                        {
                            return;
                        }
                    }
                }
                hediffResource = Utils.AdjustResourceAmount(pawn, resourceProperties.hediff, num, resourceProperties.addHediffIfMissing, resourceProperties, resourceProperties.applyToPart);
                if (hediffResource != null)
                {
                    ARTLog.Message(parent + " is affecting " + pawn + " - " + resourceProperties.hediff);
                    hediffResource.TryAddAmplifier(this);
                }
            }
        }

        public float GetResourceCapacityGainFor(HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                return this.GetCapacityFor(GetFirstResourcePropertiesFor(hediffResourceDef));
            }
            return 0f;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Active)
            {
                foreach (var option in Props.resourceSettings)
                {
                    GenDraw.DrawFieldEdges(Utils.GetAllCellsAround(option, parent, parent.OccupiedRect()).ToList());
                }
            }
        }

        public ResourceProperties GetFirstResourcePropertiesFor(HediffResourceDef hediffResourceDef)
        {
            return Props.resourceSettings.FirstOrDefault(x => x.hediff == hediffResourceDef);
        }
    }
}
