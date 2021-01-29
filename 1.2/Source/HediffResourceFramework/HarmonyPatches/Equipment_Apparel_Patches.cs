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

	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain_NewTmp")]
	public static class Patch_HasPartsToWear
	{
		private static bool Prefix(ref float __result, Pawn pawn, Apparel ap, List<float> wornScoresCache)
		{
			if (!AddHumanlikeOrders_Fix.CanWear(pawn, ap, out string tmp))
            {
				__result = -1000f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
	public static class AddHumanlikeOrders_Fix
	{
		public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
		{
			IntVec3 c = IntVec3.FromVector3(clickPos);
			foreach (var apparel in GridsUtility.GetThingList(c, pawn.Map).Where(x => x is Apparel).Cast<Apparel>())
			{
				TaggedString toCheck = "ForceWear".Translate(apparel.LabelCap, apparel);
				FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
				(toCheck));
				if (floatMenuOption != null && !CanWear(pawn, apparel, out string reason))
				{
					opts.Remove(floatMenuOption);
					var newOption = new FloatMenuOption("CannotWear".Translate(apparel.LabelShort) + "(" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
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
