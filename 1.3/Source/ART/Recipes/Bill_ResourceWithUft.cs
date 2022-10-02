using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ART
{
    public class Bill_ResourceWithUft : Bill_ProductionWithUft
    {
        public Bill_ResourceWithUft()
        {

        }
        public Bill_ResourceWithUft(RecipeDef recipe, Precept_ThingStyle precept = null) : base(recipe, precept)
        {

        }
        public RecipeResourceIngredients Extension => recipe.GetModExtension<RecipeResourceIngredients>();
        public Dictionary<HediffResourceDef, float> consumedResources = new Dictionary<HediffResourceDef, float>();
        public override bool PawnAllowedToStartAnew(Pawn p)
        {
            return Bill_Resource.AllowedToStartAnew(p, Extension);
        }
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            consumedResources.Clear();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref consumedResources, "consumedResources", LookMode.Def, LookMode.Value, ref defKeys, ref floatValues);
        }

        private List<HediffResourceDef> defKeys;
        private List<float> floatValues;
    }
}
