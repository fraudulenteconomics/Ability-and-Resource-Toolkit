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

	[HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
	public static class Patch_TryDropEquipment
	{
		private static void Prefix(ThingWithComps eq)
		{
			var comp = eq.TryGetComp<CompWeaponAdjustHediffs>();
			if (comp != null)
            {
				comp.Notify_Removed();
            }
		}
	}

	[HarmonyPatch(typeof(Pawn_ApparelTracker), "TryDrop", 
		new Type[] { typeof(Apparel), typeof(Apparel), typeof(IntVec3), typeof(bool)}, 
		new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
	public static class Patch_TryDrop
	{
		private static void Prefix(Apparel ap)
		{
			var comp = ap.TryGetComp<CompApparelAdjustHediffs>();
			if (comp != null)
			{
				comp.Notify_Removed();
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain_NewTmp")]
	public static class Patch_HasPartsToWear
	{
		private static bool Prefix(ref float __result, Pawn pawn, Apparel ap, List<float> wornScoresCache)
		{
			if (!AddHumanlikeOrders_Patch.CanWear(pawn, ap, out string tmp))
            {
				__result = -1000f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
	public static class AddHumanlikeOrders_Patch
	{
		public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
		{
			IntVec3 c = IntVec3.FromVector3(clickPos);
			foreach (var apparel in GridsUtility.GetThingList(c, pawn.Map).OfType<Apparel>())
			{
				TaggedString toCheck = "ForceWear".Translate(apparel.LabelCap, apparel);
				FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
				(toCheck));
				if (floatMenuOption != null && !CanWear(pawn, apparel, out string reason))
				{
					opts.Remove(floatMenuOption);
					var newOption = new FloatMenuOption("HRF.CannotWear".Translate(apparel.def.label) + " (" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
					opts.Add(newOption);
				}
			}

			if (pawn.equipment != null)
			{
				List<Thing> thingList = c.GetThingList(pawn.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i].TryGetComp<CompEquippable>() != null)
					{
						var equipment = (ThingWithComps)thingList[i];
						TaggedString toCheck = "Equip".Translate(equipment.LabelShort);
						FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
						(toCheck));
						if (floatMenuOption != null && !CanEquip(pawn, equipment, out string reason))
						{
							opts.Remove(floatMenuOption);
							var newOption = new FloatMenuOption("CannotEquip".Translate(equipment.LabelShort) + " (" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
							opts.Add(newOption);
						}
					}
				}
			}
		}

		public static bool CanWear(Pawn pawn, Apparel apparel, out string reason)
		{
			var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
			if (hediffComp?.Props.hediffOptions != null)
			{
				foreach (var option in hediffComp.Props.hediffOptions)
				{
					if (option.disallowEquipIfHediffMissing)
					{
						var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
						if (hediff is null)
						{
							reason = option.cannotEquipReason;
							return false;
						}
					}

					if (option.blackListHediffsPreventEquipping != null)
					{
						foreach (var hediffDef in option.blackListHediffsPreventEquipping)
						{
							var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
							if (hediff != null)
							{
								reason = option.cannotEquipReasonIncompatible + hediffDef.label;
								return false;
							}
						}
					}
				}
			}
			reason = "";
			return true;
		}

		private static bool CanEquip(Pawn pawn, ThingWithComps weapon, out string reason)
		{
			var hediffComp = weapon.GetComp<CompWeaponAdjustHediffs>();
			if (hediffComp?.Props.hediffOptions != null)
			{
				foreach (var option in hediffComp.Props.hediffOptions)
				{
					if (option.disallowEquipIfHediffMissing)
					{
						var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
						if (hediff is null || hediff.ResourceAmount <= 0)
						{
							reason = option.cannotEquipReason;
							return false;
						}
					}

					if (option.blackListHediffsPreventEquipping != null)
					{
						foreach (var hediffDef in option.blackListHediffsPreventEquipping)
						{
							var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
							if (hediff != null)
							{
								reason = option.cannotEquipReasonIncompatible;
								return false;
							}
						}
					}
				}
			}
			reason = "";
			return true;
		}
	}
}
