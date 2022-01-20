using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
    public class Bill_Resource : Bill_Production
    {
        public Bill_Resource()
        {

        }
        public Bill_Resource(RecipeDef recipe, Precept_ThingStyle precept = null) : base(recipe, precept)
        {

        }
        public RecipeResourceIngredients Extension => this.recipe.GetModExtension<RecipeResourceIngredients>();
        public Dictionary<HediffResourceDef, float> consumedResources = new Dictionary<HediffResourceDef, float>();
        public override bool PawnAllowedToStartAnew(Pawn p)
        {
            return AllowedToStartAnew(p, Extension);
        }

        public override void Notify_PawnDidWork(Pawn p)
        {
            base.Notify_PawnDidWork(p);
            DoWork(this.recipe, p, Extension, consumedResources);
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            consumedResources.Clear();
        }

        public static bool AllowedToStartAnew(Pawn p, RecipeResourceIngredients extension)
        {
            foreach (var resourceCost in extension.recourseCostList)
            {
                var hediff = p.health.hediffSet.GetFirstHediffOfDef(resourceCost.resource) as HediffResource;
                if (hediff is null || resourceCost.cost > 0 && hediff.ResourceAmount < resourceCost.cost)
                {
                    return false;
                }
            }
            if (extension.recourseCostList.Any(x => x.cost < 0))
            {
                var allHediffs = p.health.hediffSet.hediffs.OfType<HediffResource>().Where(x => extension.recourseCostList.Any(y => y.resource == x.def)).ToList();
                if (allHediffs.Count != extension.recourseCostList.Count || allHediffs.All(x => x.ResourceAmount >= x.ResourceCapacity))
                {
                    return false;
                }
            }
            return true;
        }
        public static void DoWork(RecipeDef recipe, Pawn p, RecipeResourceIngredients extension, Dictionary<HediffResourceDef, float> consumedResources)
        {
            if (p.jobs.curDriver is JobDriver_DoBill jobDriver_DoBill)
            {
                UnfinishedThing uft = p.CurJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                float num = ((p.CurJob.RecipeDef.workSpeedStat == null) ? 1f : p.GetStatValue(p.CurJob.RecipeDef.workSpeedStat));
                if (p.CurJob.RecipeDef.workTableSpeedStat != null)
                {
                    Building_WorkTable building_WorkTable = jobDriver_DoBill.BillGiver as Building_WorkTable;
                    if (building_WorkTable != null)
                    {
                        num *= building_WorkTable.GetStatValue(p.CurJob.RecipeDef.workTableSpeedStat);
                    }
                }

                if (DebugSettings.fastCrafting)
                {
                    num *= 30f;
                }
                var workTotalAmount = recipe.WorkAmountTotal(uft?.Stuff);
                foreach (var resourceCost in extension.recourseCostList)
                {
                    if (consumedResources is null)
                    {
                        consumedResources = new Dictionary<HediffResourceDef, float>();
                    }
                    if (!consumedResources.ContainsKey(resourceCost.resource))
                    {
                        consumedResources[resourceCost.resource] = 0;
                    }

                    var diff = resourceCost.cost - consumedResources[resourceCost.resource];
                    var curCost = resourceCost.cost / (workTotalAmount / num);
                    if (diff != 0)
                    {
                        var hediff = p.health.hediffSet.GetFirstHediffOfDef(resourceCost.resource) as HediffResource;
                        if (hediff is null || diff > 0 && (int)hediff.ResourceAmount < (int)diff)
                        {
                            Log.Message("Ending job: " + p.CurJob + " - hediff.ResourceAmount: " + hediff.ResourceAmount + " - diff: " + diff);
                            p.jobs.EndCurrentJob(JobCondition.Incompletable);
                        }
                        else
                        {
                            if (resourceCost.cost < 0 && hediff.ResourceAmount >= hediff.ResourceCapacity)
                            {
                                continue;
                            }
                            var toConsume = diff > 0 ? diff >= curCost ? curCost : diff : diff < curCost ? curCost : diff;
                            hediff.ResourceAmount -= toConsume;
                            if (consumedResources.ContainsKey(resourceCost.resource))
                            {
                                consumedResources[resourceCost.resource] += toConsume;
                            }
                            else
                            {
                                consumedResources[resourceCost.resource] = toConsume;
                            }
                        }
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref consumedResources, "consumedResources", LookMode.Def, LookMode.Value, ref defKeys, ref floatValues);
        }

        public override bool ShouldDoNow()
        {
            return true;
        }

        private List<HediffResourceDef> defKeys;
        private List<float> floatValues;
    }
}
