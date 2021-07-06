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
	[HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing")]
	public static class Patch_NewFrameDef_Thing
	{
		private static void Postfix(ref ThingDef __result, ref ThingDef def)
		{
			var options = def.GetModExtension<FacilityInProgress>();
			if (options != null)
            {
				var props = new CompProperties_FacilityInUse_StatBoosters();
				props.statBoosters = options.statBoosters;
				__result.comps.Add(props);
				Log.Message("Adding CompProperties_FacilityInUse_StatBoosters to " + __result);
			}
		}
	}
}
