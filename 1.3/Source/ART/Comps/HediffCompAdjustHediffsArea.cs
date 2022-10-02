using RimWorld;
using System.Linq;
using Verse;

namespace ART
{
    public class HediffCompProperties_AdjustHediffsArea : HediffCompProperties_AdjustHediffs
    {
        public bool stackEffects;
        public int stackMax = -1;
        public HediffCompProperties_AdjustHediffsArea()
        {
            compClass = typeof(HediffCompAdjustHediffsArea);
        }
    }

    [HotSwappable]
    public class HediffCompAdjustHediffsArea : HediffComp_AdjustHediffs, IAdjustResouceInArea
    {
        public new HediffCompProperties_AdjustHediffsArea Props => props as HediffCompProperties_AdjustHediffsArea;
        public override void ResourceTick()
        {
            if (Active)
            {
                foreach (var option in Props.resourceSettings)
                {
                    float num = option.GetResourceGain(this);
                    var affectedCells = Utils.GetAllCellsAround(option, Pawn, Pawn.OccupiedRect());
                    foreach (var cell in affectedCells)
                    {
                        foreach (var pawn in cell.GetThingList(Pawn.Map).OfType<Pawn>())
                        {
                            if (pawn == Pawn && !option.affectsSelf)
                            {
                                continue;
                            }

                            if (option.affectsAllies && (pawn.Faction == Pawn.Faction || !pawn.Faction.HostileTo(Pawn.Faction)))
                            {
                                AppendResource(pawn, option, num);
                            }
                            else if (option.affectsEnemies && pawn.Faction.HostileTo(Pawn.Faction))
                            {
                                AppendResource(pawn, option, num);
                            }
                        }
                    }
                }
            }
        }

        public bool Active => Pawn.Map != null;

        public bool InRadiusFor(IntVec3 cell, HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                var option = GetFirstResourcePropertiesFor(hediffResourceDef);
                if (option != null && cell.DistanceTo(Pawn.Position) <= option.effectRadius)
                {
                    return true;
                }
            }
            return false;
        }
        public void AppendResource(Pawn pawn, ResourceProperties resourceProperties, float num)
        {
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
                        if (amplifiers.Any(x => x != this))
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
                    hediffResource.TryAddAmplifier(this);
                }
            }
        }

        public float GetResourceCapacityGainFor(HediffResourceDef hediffResourceDef)
        {
            if (Active)
            {
                var option = GetFirstResourcePropertiesFor(hediffResourceDef);
                return option.maxResourceCapacityOffset;
            }
            return 0f;
        }

        public ResourceProperties GetFirstResourcePropertiesFor(HediffResourceDef hediffResourceDef)
        {
            return Props.resourceSettings.FirstOrDefault(x => x.hediff == hediffResourceDef);
        }


    }
}
