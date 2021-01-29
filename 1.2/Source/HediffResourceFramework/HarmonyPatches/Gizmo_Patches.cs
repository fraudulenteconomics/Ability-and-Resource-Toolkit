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




	[HarmonyPatch(typeof(CompReloadable), "CreateVerbTargetCommand")]
	public static class Patch_CreateVerbTargetCommand
	{
		private static void Postfix(ref Command_Reloadable __result, Thing gear, Verb verb)
		{
			if (__result != null && verb.CasterIsPawn && verb.EquipmentSource != null)
			{
				if (!HediffResourceUtils.IsUsableBy(verb, out bool verbIsFromHediffResource, out string disableReason))
				{
					HediffResourceUtils.DisableGizmo(__result, disableReason);
				}
			}
		}
	}

	[HarmonyPatch(typeof(PawnVerbGizmoUtility), "GetGizmosForVerb")]
	public static class Patch_GetGizmosForVerb
	{
		private static void Postfix(Verb verb, ref IEnumerable<Gizmo> __result)
		{
			if (verb.CasterIsPawn && verb.EquipmentSource != null)
			{
				var list = __result.ToList();
				if (!HediffResourceUtils.IsUsableBy(verb, out bool verbIsFromHediffResource, out string disableReason))
                {
					foreach (var gizmo in list)
                    {
						HediffResourceUtils.DisableGizmo(gizmo, disableReason);
                    }
				}
				__result = list;
			}
		}
	}
}
