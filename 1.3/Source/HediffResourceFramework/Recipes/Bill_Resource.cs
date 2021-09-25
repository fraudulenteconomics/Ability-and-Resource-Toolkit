using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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

        public static bool AllowedToStartAnew(Pawn p, RecipeResourceIngredients extension)
        {
            foreach (var resourceCost in extension.recourseCostList)
            {
                var hediff = p.health.hediffSet.GetFirstHediffOfDef(resourceCost.resource) as HediffResource;
                if (hediff is null || hediff.ResourceAmount < resourceCost.cost)
                {
                    return false;
                }
            }
            return true;
        }
        public static void DoWork(RecipeDef recipe, Pawn p, RecipeResourceIngredients extension, Dictionary<HediffResourceDef, float> consumedResources)
        {
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
                UnfinishedThing uft = p.CurJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                JobDriver_DoBill jobDriver_DoBill = (JobDriver_DoBill)p.jobs.curDriver;
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

                var curCost = diff / (recipe.WorkAmountTotal(uft?.Stuff) * num);
                if (diff > 0)
                {
                    var hediff = p.health.hediffSet.GetFirstHediffOfDef(resourceCost.resource) as HediffResource;
                    if (hediff is null || hediff.ResourceAmount < diff)
                    {
                        p.jobs.EndCurrentJob(JobCondition.Incompletable);
                    }
                    else
                    {

                        var toConsume = diff >= curCost ? curCost : diff;
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

                diff = resourceCost.cost - consumedResources[resourceCost.resource];
                Log.Message(resourceCost.resource + " - diff: " + diff + " - curCost: " + curCost + " - num: " + num);
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
