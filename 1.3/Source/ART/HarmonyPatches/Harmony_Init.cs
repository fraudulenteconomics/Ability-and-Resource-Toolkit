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

namespace ART
{
	public class ARTMod : Mod
	{
		public ARTMod(ModContentPack content) : base(content)
		{
			Harmony harmony = new Harmony("Fraudecon.ART");
			harmony.PatchAll();
		}
	}

	//[HarmonyPatch(typeof(JobDriver), nameof(JobDriver.DriverTick))]
	//public static class JobDriverPatch
    //{
	//	public static void Prefix(JobDriver __instance)
    //    {
	//		if (__instance.pawn.jobs.debugLog)
    //        {
	//			Log.Message("Pre Ticking " + __instance + " - " + __instance.pawn.CurJob);
    //        }
    //    }
	//
	//	public static void Postfix(JobDriver __instance)
	//	{
	//		if (__instance.pawn.jobs.debugLog)
	//		{
	//			Log.Message("Post Ticking " + __instance + " - " + __instance.pawn.CurJob);
	//			Log.ResetMessageCount();
	//		}
	//	}
	//}
}
