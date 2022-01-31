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
        public bool stackEffects;
        public int stackMax = -1;
        public CompProperties_AdjustHediffsArea()
        {
            this.compClass = typeof(CompAdjustHediffsArea);
        }
    }

    public class CompAdjustHediffsArea : CompAdjustHediffs, IAdjustResouceInArea
    {
        private CompPowerTrader powerComp;
        private CompRefuelable fuelComp;
        private CompFlickable flickableComp;

        public new CompProperties_AdjustHediffsArea Props => this.props as CompProperties_AdjustHediffsArea;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = this.parent.GetComp<CompPowerTrader>();
            fuelComp = this.parent.GetComp<CompRefuelable>();
            flickableComp = this.parent.GetComp<CompFlickable>();
        }
        public override void ResourceTick()
        {
            Log.Message("Active: " + Active + " - " + this);
            Log.Message("this.parent.Map: " + this.parent.Map);
            Log.Message(" IsEnabled(): " + IsEnabled());
            if (Active)
            {
                foreach (var option in Props.resourceSettings)
                {
                    var num = HediffResourceUtils.GetResourceGain(option, this);
                    var affectedCells = HediffResourceUtils.GetAllCellsAround(option, this.parent, this.parent.OccupiedRect());
                    foreach (var cell in affectedCells)
                    {
                        foreach (var pawn in cell.GetThingList(this.parent.Map).OfType<Pawn>())
                        {
                            if (pawn == this.parent && !option.addToCaster) continue;

                            if (option.affectsAllies && (pawn.Faction == this.parent.Faction || !pawn.Faction.HostileTo(this.parent.Faction)))
                            {
                                AppendResource(pawn, option, num);
                            }
                            else if (option.affectsEnemies && pawn.Faction.HostileTo(this.parent.Faction))
                            {
                                AppendResource(pawn, option, num);
                            }
                        }
                    }
                }
            }
        }

        public bool Active => this.parent.Map != null && IsEnabled();
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
                var option = GetFirstHediffOptionFor(hediffResourceDef);
                if (option != null && cell.DistanceTo(this.parent.Position) <= option.effectRadius)
                {
                    return true;
                }
            }
            return false;
        }
        public void AppendResource(Pawn pawn, HediffOption option, float num)
        {
            Log.Message("AppendResource: " + pawn);
            var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
            if (hediffResource != null && !hediffResource.CanGainResource)
            {
                return;
            }
            else
            {
                if (hediffResource != null)
                {
                    var amplifiers = hediffResource.GetAmplifiersFor(option.hediff);
                    if (!this.Props.stackEffects)
                    {
                        if (amplifiers.Count() > 0 && amplifiers.Any(x => x != this))
                        {
                            return;
                        }
                    }
                    if (this.Props.stackMax != -1)
                    {
                        if (amplifiers.Count() >= this.Props.stackMax && !amplifiers.Contains(this))
                        {
                            return;
                        }
                    }
                }
                hediffResource = HediffResourceUtils.AdjustResourceAmount(pawn, option.hediff, num, option.addHediffIfMissing, option.applyToPart);
                if (hediffResource != null)
                {
                    Log.Message(this.parent + " is affecting " + pawn + " - " + option.hediff);
                    hediffResource.TryAddAmplifier(this);
                }
            }
        }

        public float GetResourceCapacityGainFor(HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                return this.GetCapacityFor(GetFirstHediffOptionFor(hediffResourceDef));
            }
            return 0f;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Active)
            {
                foreach (var option in this.Props.resourceSettings)
                {
                    GenDraw.DrawFieldEdges(HediffResourceUtils.GetAllCellsAround(option, this.parent, this.parent.OccupiedRect()).ToList());
                }
            }
        }

        public HediffOption GetFirstHediffOptionFor(HediffResourceDef hediffResourceDef)
        {
            return this.Props.resourceSettings.FirstOrDefault(x => x.hediff == hediffResourceDef);
        }
    }
}
