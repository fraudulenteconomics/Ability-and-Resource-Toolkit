using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

	[HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents")]
	public static class Patch_DoWindowContents
	{
		[HarmonyPriority(int.MaxValue)]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			var codes = instructions.ToList();

			for (var i = 0; i < codes.Count; i++)
            {
				if (i > 2 && codes[i - 1].opcode == OpCodes.Blt_S && codes[i - 2].Calls(AccessTools.Method(typeof(List<IngredientCount>), "get_Count")) 
					&& codes[i].opcode == OpCodes.Ldloc_S && codes[i].operand is LocalBuilder lb && lb.LocalIndex >= 34)
				{
					yield return new CodeInstruction(OpCodes.Ldloc_S, lb.LocalIndex);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Dialog_BillConfig), "bill"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Bill), "recipe"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_DoWindowContents), "AppendLine"));
                }
				yield return codes[i];

            }
		}

		public static void AppendLine(StringBuilder stringBuilder, RecipeDef recipe)
        {
			var extension = recipe.GetModExtension<RecipeResourceIngredients>();
			if (extension != null)
            {
				foreach (var resourceCost in extension.recourseCostList)
                {
					if (resourceCost.cost > 0)
                    {
						stringBuilder.AppendLine("BillRequires".Translate(resourceCost.cost, resourceCost.resource.label));
					}
				}
			}
		}
	}
}
