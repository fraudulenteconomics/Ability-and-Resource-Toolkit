using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_AdjustHediffsArea : CompProperties_AdjustHediffs
    {
        public CompProperties_AdjustHediffsArea()
        {
            this.compClass = typeof(CompAdjustHediffsArea);
        }
    }

    public class CompAdjustHediffsArea : CompAdjustHediffs
    {
        private CompPowerTrader powerComp;
        private CompRefuelable fuelComp;
        private CompFlickable flickableComp;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = this.parent.GetComp<CompPowerTrader>();
            fuelComp = this.parent.GetComp<CompRefuelable>();
            flickableComp = this.parent.GetComp<CompFlickable>();
        }
        public override void ResourceTick()
        {
            if (Active && this.parent.IsHashIntervalTick(60))
            {
                foreach (var option in Props.resourceSettings)
                {
                    var num = GetResourceGain(option);
                    var affectedCells = GetAllCells(option);
                    foreach (var cell in affectedCells)
                    {
                        foreach (var pawn in cell.GetThingList(this.parent.Map).OfType<Pawn>())
                        {
                            if (pawn == this.parent && !option.addToCaster) continue;

                            if (option.affectsAllies && (pawn.Faction == this.parent.Faction || !pawn.Faction.HostileTo(this.parent.Faction)))
                            {
                                HRFLog.Message($"Ally: {pawn}, resource: {option.hediff}, num to adjust: {num}");
                                AppendResource(pawn, option, num);
                            }
                            else if (option.affectsEnemies && pawn.Faction.HostileTo(this.parent.Faction))
                            {
                                HRFLog.Message($"Enemy: {pawn}, resource: {option.hediff}, num to adjust: {num}");
                                AppendResource(pawn, option, num);
                            }
                        }
                    }
                }
            }
        }

        public bool Active => this.parent.Map != null && IsEnabled();
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
                var option = GetFirstHediffOptionFor(hediffResourceDef);
                if (option != null && cell.DistanceTo(this.parent.Position) <= option.radius)
                {
                    return true;
                }
            }
            return false;
        }
        public void AppendResource(Pawn pawn, HediffAdjust option, float num)
        {
            var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
            if (hediffResource != null && !hediffResource.CanGainResource)
            {
                return;
            }
            else
            {
                hediffResource = HediffResourceUtils.AdjustResourceAmount(pawn, option.hediff, num, option.addHediffIfMissing);
                if (hediffResource != null)
                {
                    hediffResource.TryAddAmplifier(this);
                }
            }
        }
        public float GetResourceGain(HediffAdjust option)
        {
            float num = option.resourcePerSecond;
            if (option.qualityScalesResourcePerSecond && this.parent.TryGetQuality(out QualityCategory qc))
            {
                num *= HediffResourceUtils.GetQualityMultiplier(qc);
            }
            return num;
        }

        public float GetResourceCapacityGainFor(HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                var option = GetFirstHediffOptionFor(hediffResourceDef);
                if (option.qualityScalesCapacityOffset && this.parent.TryGetQuality(out QualityCategory qc))
                {
                    return option.maxResourceCapacityOffset * HediffResourceUtils.GetQualityMultiplier(qc);
                }
                else
                {
                    return option.maxResourceCapacityOffset;
                }
            }
            return 0f;
        }
        public HashSet<IntVec3> GetAllCells(HediffAdjust option)
        {
            if (option.worksThroughWalls)
            {
                return GetAllCellsInRadius(option);
            }
            else
            {
                return GetAffectedCells(option);
            }
        }
        public HashSet<IntVec3> GetAllCellsInRadius(HediffAdjust option)
        {
            HashSet<IntVec3> tempCells = new HashSet<IntVec3>();
            foreach (var cell in this.parent.OccupiedRect().Cells)
            {
                foreach (var intVec in GenRadial.RadialCellsAround(cell, option.radius, true))
                {
                    tempCells.Add(intVec);
                }
            }
            return tempCells;
        }
        public HashSet<IntVec3> GetAffectedCells(HediffAdjust option)
        {
            HashSet<IntVec3> affectedCells = new HashSet<IntVec3>();
            HashSet<IntVec3> tempCells = GetAllCellsInRadius(option);

            Predicate<IntVec3> validator = delegate (IntVec3 cell)
            {
                if (!tempCells.Contains(cell)) return false;
                var edifice = cell.GetEdifice(this.parent.Map);
                var result = edifice == null || edifice.def.passability != Traversability.Impassable || edifice == this.parent;
                return result;
            };
            var centerCell = this.parent.OccupiedRect().CenterCell;
            this.parent.Map.floodFiller.FloodFill(centerCell, validator, delegate (IntVec3 x)
            {
                if (tempCells.Contains(x))
                {
                    var edifice = x.GetEdifice(this.parent.Map);
                    var result = edifice == null || edifice.def.passability != Traversability.Impassable || edifice == this.parent;
                    if (result && (GenSight.LineOfSight(centerCell, x, this.parent.Map) || centerCell.DistanceTo(x) <= 1.5f))
                    {
                        affectedCells.Add(x);
                    }
                }
            }, int.MaxValue, rememberParents: false, (IEnumerable<IntVec3>)null);
            affectedCells.AddRange(this.parent.OccupiedRect());
            return affectedCells;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Active)
            {
                foreach (var option in this.Props.resourceSettings)
                {
                    GenDraw.DrawFieldEdges(GetAllCells(option).ToList());
                }
            }
        }

        public HediffAdjust GetFirstHediffOptionFor(HediffResourceDef hediffResourceDef)
        {
            return this.Props.resourceSettings.FirstOrDefault(x => x.hediff == hediffResourceDef);
        }
    }
}
