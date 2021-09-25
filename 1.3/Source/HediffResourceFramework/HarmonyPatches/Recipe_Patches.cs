using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
	[HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
	public static class Patch_MakeNewBill
	{ 
		public static bool Prefix(ref Bill __result, RecipeDef recipe, Precept_ThingStyle precept = null)
        {
			if (recipe.HasModExtension<RecipeResourceIngredients>())
            {
				if (recipe.UsesUnfinishedThing)
				{
					__result = new Bill_ResourceWithUft(recipe, precept);
				}
				else
                {
					__result = new Bill_Resource(recipe, precept);
				}
				return false;
			}
			return true;
        }
	}
}
