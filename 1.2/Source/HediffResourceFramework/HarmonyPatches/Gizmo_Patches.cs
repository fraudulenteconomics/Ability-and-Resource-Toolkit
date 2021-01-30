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
				if (!HediffResourceUtils.IsUsableBy(verb, out string disableReason))
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
				if (!HediffResourceUtils.IsUsableBy(verb, out string disableReason))
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


	[StaticConstructorOnStartup]
	public class Gizmo_ResourceStatus : Gizmo
	{
		public HediffResource hediffResource;

		private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));

		private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
		public Gizmo_ResourceStatus()
		{
			order = -100f;
		}

		public override float GetWidth(float maxWidth)
		{
			return 140f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
		{
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect rect2 = rect.ContractedBy(6f);
			Widgets.DrawWindowBackground(rect);
			Rect rect3 = rect2;
			rect3.height = rect.height / 2f;
			Text.Font = GameFont.Tiny;
			var label = hediffResource.def.LabelCap;
			if (hediffResource.def.lifetimeTicks != -1)
			{
				label += " (" + Mathf.CeilToInt((hediffResource.def.lifetimeTicks - hediffResource.duration).TicksToSeconds()) + "s)";
			}
			Widgets.Label(rect3, label);
			Rect rect4 = rect2;
			rect4.yMin = rect2.y + rect2.height / 2f;
			float fillPercent = hediffResource.ResourceAmount / hediffResource.ResourceCapacity;
			Widgets.FillableBar(rect4, fillPercent, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect4, (hediffResource.ResourceAmount).ToString("F0") + " / " + (hediffResource.ResourceCapacity).ToString("F0"));
			Text.Anchor = TextAnchor.UpperLeft;
			return new GizmoResult(GizmoState.Clear);
		}
	}

	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public class Pawn_GetGizmos_Patch
	{
		public static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
		{
			if (__instance.Faction == Faction.OfPlayer)
			{
				List<Gizmo> list = __result.ToList<Gizmo>();
				var hediffResources = __instance.health?.hediffSet?.hediffs.OfType<HediffResource>();
				if (hediffResources != null)
                {
					foreach (var hediffResource in hediffResources)
                    {
						Gizmo_ResourceStatus gizmo_hediffResourceStatus = new Gizmo_ResourceStatus();
						gizmo_hediffResourceStatus.hediffResource = hediffResource;
						list.Add(gizmo_hediffResourceStatus);
					}
                }
				__result = list;
			}
		}
	}

	[HarmonyPatch(typeof(Command), "GizmoOnGUI")]
	public static class Patch_GizmoOnGUI
	{
		private static void Postfix(Command __instance, Vector2 topLeft, float maxWidth)
		{
			if (__instance is Command_VerbTarget verbTarget)
            {
				if (TryGetAmmoString(verbTarget.verb, out string ammoString, out int hediffCount))
                {
					var butRect = new Rect(topLeft.x, topLeft.y, verbTarget.GetWidth(maxWidth), 75f);
					var pos = 70f - (hediffCount * 18f);
					Vector2 vector = new Vector2(10f, pos);
					Text.Font = GameFont.Tiny;
					Widgets.Label(new Rect(butRect.x + vector.x, butRect.y + vector.y, butRect.width - 3f, 75f - pos), ammoString);
					Text.Font = GameFont.Small;

				}
			}
		}

		public static bool TryGetAmmoString(Verb verb, out string ammoString, out int hediffCount)
		{
			ammoString = "";
			hediffCount = 0;
			if (verb.CasterIsPawn && verb.EquipmentSource != null)
			{
				var options = verb.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
				{
					foreach (var option in options.hediffOptions)
					{
						if (HediffResourceUtils.VerbMatches(verb, option))
						{
							var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
							if (manaHediff != null)
                            {
								ammoString += (int)option.minimumResourcePerUse + "/" + (int)manaHediff.ResourceAmount + " " + manaHediff.def.label + "\n";
								hediffCount++;
							}
						}
					}
				}
			}
			if (hediffCount > 0)
            {
				return true;
            }
			return false;
		}
	}
}
