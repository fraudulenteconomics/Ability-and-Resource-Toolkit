﻿using RimWorld;
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
    public class Bill_ResourceWithUft : Bill_ProductionWithUft
    {
        public Bill_ResourceWithUft()
        {

        }
        public Bill_ResourceWithUft(RecipeDef recipe, Precept_ThingStyle precept = null) : base(recipe, precept)
        {

        }
        public RecipeResourceIngredients Extension => this.recipe.GetModExtension<RecipeResourceIngredients>();
        public Dictionary<HediffResourceDef, float> consumedResources = new Dictionary<HediffResourceDef, float>();
        public override bool PawnAllowedToStartAnew(Pawn p)
        {
            return Bill_Resource.AllowedToStartAnew(p, Extension);
        }

        public override void Notify_PawnDidWork(Pawn p)
        {
            base.Notify_PawnDidWork(p);
            Bill_Resource.DoWork(this.recipe, p, Extension, consumedResources);
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

        public override bool ShouldDoNow()
        {
            return true;
        }

        private List<HediffResourceDef> defKeys;
        private List<float> floatValues;
    }
}