using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class HediffCompProperties_AdjustHediffsArea : HediffCompProperties_AdjustHediffs
    {
        public bool stackEffects;
        public int stackMax = -1;
        public HediffCompProperties_AdjustHediffsArea()
        {
            this.compClass = typeof(CompAdjustHediffsArea);
        }
    }

    public class HediffCompAdjustHediffsArea : HediffComp_AdjustHediffs, IAdjustResouceInArea
    {
        public new HediffCompProperties_AdjustHediffsArea Props => this.props as HediffCompProperties_AdjustHediffsArea;
        public override void ResourceTick()
        {
            if (Active)
            {
                foreach (var option in Props.resourceSettings)
                {
                    var num = option.GetResourceGain(this);
                    var affectedCells = HediffResourceUtils.GetAllCellsAround(option, this.Pawn, this.Pawn.OccupiedRect());
                    foreach (var cell in affectedCells)
                    {
                        foreach (var pawn in cell.GetThingList(this.Pawn.Map).OfType<Pawn>())
                        {
                            if (pawn == this.Pawn && !option.addToCaster) continue;

                            if (option.affectsAllies && (pawn.Faction == this.Pawn.Faction || !pawn.Faction.HostileTo(this.Pawn.Faction)))
                            {
                                AppendResource(pawn, option, num);
                            }
                            else if (option.affectsEnemies && pawn.Faction.HostileTo(this.Pawn.Faction))
                            {
                                AppendResource(pawn, option, num);
                            }
                        }
                    }
                }
            }
        }

        public bool Active => this.Pawn.Map != null;

        public bool InRadiusFor(IntVec3 cell, HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                var option = GetFirstHediffOptionFor(hediffResourceDef);
                if (option != null && cell.DistanceTo(this.Pawn.Position) <= option.effectRadius)
                {
                    return true;
                }
            }
            return false;
        }
        public void AppendResource(Pawn pawn, HediffOption hediffOption, float num)
        {
            Log.Message("AppendResource: " + pawn);
            var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
            if (hediffResource != null && !hediffResource.CanGainResource)
            {
                return;
            }
            else
            {
                if (hediffResource != null)
                {
                    var amplifiers = hediffResource.GetAmplifiersFor(hediffOption.hediff);
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
                hediffResource = HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, num, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
                if (hediffResource != null)
                {
                    Log.Message(this.parent + " is affecting " + pawn + " - " + hediffOption.hediff);
                    hediffResource.TryAddAmplifier(this);
                }
            }
        }

        public float GetResourceCapacityGainFor(HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                var option = GetFirstHediffOptionFor(hediffResourceDef);
                return option.maxResourceCapacityOffset;
            }
            return 0f;
        }
        
        public HediffOption GetFirstHediffOptionFor(HediffResourceDef hediffResourceDef)
        {
            return this.Props.resourceSettings.FirstOrDefault(x => x.hediff == hediffResourceDef);
        }
    }
}
