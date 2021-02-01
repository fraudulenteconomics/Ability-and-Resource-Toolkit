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

	//[HarmonyPatch(typeof(ReservationManager), "CanReserve")]
	//public static class Patch_CanReserve
	//{
	//	public static Pawn checkDrinkingReservation;
	//	private static bool Prefix(Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
	//	{
	//		if (claimant == checkDrinkingReservation && claimant.IsHediffUser() && !HediffResourceUtils.CanDrink(claimant, target.Thing, out string reason))
	//		{
	//			Log.Message(claimant + " shouldn't drink " + target.Thing);
	//			return false;
	//		}
	//		if (claimant == checkDrinkingReservation)
    //        {
	//			Log.Message(claimant + " can do anything with " + target.Thing);
	//		}
	//		return true;
	//	}
	//}
	//
	//[HarmonyPatch(typeof(JobGiver_TakeDrugsForDrugPolicy), "FindDrugFor")]
	//public static class Patch_FindDrugFor
	//{
	//	private static void Prefix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = pawn;
	//	}
	//
	//	private static void Postfix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = null;
	//	}
	//}
	//
	//
	//[HarmonyPatch(typeof(JobGiver_EatRandom), "TryGiveJob")]
	//public static class Patch_TryGiveJob
	//{
	//	private static void Prefix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = pawn;
	//	}
	//
	//	private static void Postfix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = null;
	//	}
	//}
	//
	//
	//[HarmonyPatch(typeof(JobGiver_EatInGatheringArea), "FindFood")]
	//public static class Patch_FindFood
	//{
	//	private static void Prefix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = pawn;
	//	}
	//
	//	private static void Postfix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = null;
	//	}
	//}
	//
	//[HarmonyPatch(typeof(FoodUtility), "TryFindBestFoodSourceFor")]
	//public static class Patch_TryFindBestFoodSourceFor
	//{
	//	private static void Prefix(Pawn getter, Pawn eater, bool desperate, out Thing foodSource, out ThingDef foodDef, bool canRefillDispenser = true, bool canUseInventory = true, bool allowForbidden = false, bool allowCorpse = true, bool allowSociallyImproper = false, bool allowHarvest = false, bool forceScanWholeMap = false, bool ignoreReservations = false, FoodPreferability minPrefOverride = FoodPreferability.Undefined)
	//	{
	//		foodDef = null;
	//		foodSource = null;
	//		Patch_CanReserve.checkDrinkingReservation = eater;
	//	}
	//	private static void Postfix(Pawn getter, Pawn eater, bool desperate, out Thing foodSource, out ThingDef foodDef, bool canRefillDispenser = true, bool canUseInventory = true, bool allowForbidden = false, bool allowCorpse = true, bool allowSociallyImproper = false, bool allowHarvest = false, bool forceScanWholeMap = false, bool ignoreReservations = false, FoodPreferability minPrefOverride = FoodPreferability.Undefined)
	//	{
	//		foodDef = null;
	//		foodSource = null;
	//		Patch_CanReserve.checkDrinkingReservation = null;
	//	}
	//}
	//
	//
	//[HarmonyPatch(typeof(JobGiver_BingeDrug), "BestIngestTarget")]
	//public static class Patch_BestIngestTarget
	//{
	//	private static void Prefix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = pawn;
	//	}
	//
	//	private static void Postfix(Pawn pawn)
	//	{
	//		Patch_CanReserve.checkDrinkingReservation = null;
	//	}
	//}
}
