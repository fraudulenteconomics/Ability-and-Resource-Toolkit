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

	[HarmonyPatch(typeof(Hediff), "PostAdd")]
	public static class Patch_PostAdd
	{
		private static void Postfix(Hediff __instance)
		{
			var pawn = __instance?.pawn;
			if (pawn != null)
			{
				var apparels = pawn.apparel?.WornApparel?.ToList();
				if (apparels != null)
				{
					foreach (var apparel in apparels)
					{
						var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
						if (hediffComp?.Props.hediffOptions != null)
						{
							foreach (var option in hediffComp.Props.hediffOptions)
							{
								if (option.dropWeaponOrApparelIfBlacklistHediff?.Contains(__instance.def) ?? false)
								{
									pawn.apparel.TryDrop(apparel);
								}
							}
						}
					}
				}

				var equipments = pawn?.equipment?.AllEquipmentListForReading;
				if (equipments != null)
				{
					foreach (var equipment in equipments)
					{
						var hediffComp = equipment.GetComp<CompWeaponAdjustHediffs>();
						if (hediffComp?.Props.hediffOptions != null)
						{
							foreach (var option in hediffComp.Props.hediffOptions)
							{
								if (option.dropWeaponOrApparelIfBlacklistHediff?.Contains(__instance.def) ?? false)
								{
									pawn.equipment.TryDropEquipment(equipment, out ThingWithComps result, pawn.Position);
								}
							}
						}
					}
				}
			}
		}
	}
}
