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
			if (__result != null && verb.CasterIsPawn)
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
			if (verb.CasterIsPawn)
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

	public class FloatValueCache
	{
		public FloatValueCache(float value)
		{
			this.value = value;
		}
		private float value;
		public float Value
        {
			get
            {
				return value;
			}
			set
            {
				this.value = value;
				updateTick = Find.TickManager.TicksGame;
            }
        }
		public int updateTick;
	}

	[StaticConstructorOnStartup]
	public class Gizmo_ResourceStatus : Gizmo
	{
		private HediffResource hediffResource;

		private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
		public Gizmo_ResourceStatus(HediffResource hediffResource)
		{
			order = -100f;
			this.hediffResource = hediffResource;
			resourceCapacityCache = new FloatValueCache(hediffResource.ResourceCapacity);
			resourceAmountCache = new FloatValueCache(hediffResource.ResourceAmount);
		}
		public override float GetWidth(float maxWidth)
		{
			return 140f;
		}

		private Texture2D fullShieldBarTexCache;
		private Texture2D FullShieldBarTex
        {
			get
            {
				if (fullShieldBarTexCache is null)
                {
					Color fullShieldBarColor;

					if (hediffResource.def.progressBarColor.HasValue)
					{
						fullShieldBarColor = hediffResource.def.progressBarColor.Value;
					}
					else
					{
						fullShieldBarColor = hediffResource.def.defaultLabelColor;
					}
					fullShieldBarTexCache = SolidColorMaterials.NewSolidColorTexture(fullShieldBarColor);
				}
				return fullShieldBarTexCache;
			}
        }

		private Color cachedBackgroundColor = Color.clear;
		private Color BackGroundColor
        {
			get
            {
				if (cachedBackgroundColor == Color.clear)
                {
					cachedBackgroundColor = hediffResource.def.backgroundBarColor.HasValue ? hediffResource.def.backgroundBarColor.Value : Widgets.WindowBGFillColor; ;
				}
				return cachedBackgroundColor;
            }
        }

		private Color cachedDrawBox = Color.clear;
		private Color DrawBoxColor
        {
			get
            {
				if (cachedDrawBox == Color.clear)
                {
					cachedDrawBox = new ColorInt(97, 108, 122).ToColor;
				}
				return cachedDrawBox;
            }
        }

		public FloatValueCache resourceCapacityCache;
		public FloatValueCache resourceAmountCache;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect rect2 = rect.ContractedBy(6f);
			DrawWindowBackground(rect, hediffResource.def);
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

			if (Find.TickManager.TicksGame > resourceCapacityCache.updateTick + 30)
            {
				resourceCapacityCache.Value = hediffResource.ResourceCapacity;
				resourceAmountCache.Value = hediffResource.ResourceAmount;
			}

			var resourceAmount = resourceAmountCache.Value;
			var resourceCapacity = resourceCapacityCache.Value;
			float fillPercent = resourceAmount / resourceCapacity;

			Widgets.FillableBar(rect4, fillPercent, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			if (hediffResource.def.resourceBarTextColor.HasValue)
            {
				GUI.color = hediffResource.def.resourceBarTextColor.Value;
			}
			Widgets.Label(rect4, (resourceAmount).ToString("F0") + " / " + (resourceCapacity).ToString("F0"));
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			return new GizmoResult(GizmoState.Clear);
		}

		public void DrawWindowBackground(Rect rect, HediffResourceDef hediffResourceDef)
		{
			GUI.color = BackGroundColor;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = DrawBoxColor;
			Widgets.DrawBox(rect);
			GUI.color = Color.white;
		}
	}

	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public class Pawn_GetGizmos_Patch
	{
		public static Dictionary<HediffResource, Gizmo_ResourceStatus> cachedGizmos = new Dictionary<HediffResource, Gizmo_ResourceStatus>();
		public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
		{
			foreach (var g in __result)
            {
				yield return g;
            }
			if (__instance.Faction == Faction.OfPlayer)
			{
				var hediffResources = HediffResourceUtils.GetHediffResourcesFor(__instance);
				if (hediffResources != null)
                {
					foreach (var hediffResource in hediffResources)
                    {
						if (hediffResource.def.showResourceBar)
                        {
							if (cachedGizmos.TryGetValue(hediffResource, out Gizmo_ResourceStatus gizmo_ResourceStatus))
                            {
								yield return gizmo_ResourceStatus;
							}
							else
                            {
								gizmo_ResourceStatus = new Gizmo_ResourceStatus(hediffResource);
								cachedGizmos[hediffResource] = gizmo_ResourceStatus;
								yield return gizmo_ResourceStatus;
							}
						}
					}
                }


			}

			if (Prefs.DevMode)
			{
				Command_Action command_Action = new Command_Action();
				command_Action.defaultLabel = "Debug: Charge all hediff resources";
				command_Action.action = delegate
				{
					var hediffResources = HediffResourceUtils.GetHediffResourcesFor(__instance);
					if (hediffResources != null)
					{
						foreach (var hediffResource in hediffResources)
						{
							hediffResource.ResourceAmount = hediffResource.ResourceCapacity;
						}
					}
				};
				yield return command_Action;

				Command_Action command_Empty = new Command_Action();
				command_Empty.defaultLabel = "Debug: Empty all hediff resources";
				command_Empty.action = delegate
				{
					var hediffResources = HediffResourceUtils.GetHediffResourcesFor(__instance);
					if (hediffResources != null)
					{
						foreach (var hediffResource in hediffResources)
						{
							hediffResource.ResourceAmount = 0;
						}
					}
				};
				yield return command_Empty;

				Command_Action command_ReduceBy10 = new Command_Action();
				command_ReduceBy10.defaultLabel = "Debug: Reduce all hediff resources by 10";
				command_ReduceBy10.action = delegate
				{
					var hediffResources = HediffResourceUtils.GetHediffResourcesFor(__instance);
					if (hediffResources != null)
					{
						foreach (var hediffResource in hediffResources)
						{
							hediffResource.ResourceAmount -= 10;
						}
					}
				};
				yield return command_ReduceBy10;

				Command_Action command_AddBy10 = new Command_Action();
				command_AddBy10.defaultLabel = "Debug: Add all hediff resources by 10";
				command_AddBy10.action = delegate
				{
					var hediffResources = HediffResourceUtils.GetHediffResourcesFor(__instance);
					if (hediffResources != null)
					{
						foreach (var hediffResource in hediffResources)
						{
							hediffResource.ResourceAmount += 10;
						}
					}
				};
				yield return command_AddBy10;

				Command_Action command_Action3 = new Command_Action();
				command_Action3.defaultLabel = "Debug: Remove all hediff resources";
				command_Action3.action = delegate
				{
					var hediffResources = HediffResourceUtils.GetHediffResourcesFor(__instance);
					if (hediffResources != null)
					{
						foreach (var hediffResource in hediffResources)
						{
							hediffResource.pawn.health.RemoveHediff(hediffResource);
						}
					}
				};
				yield return command_Action3;
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
				if (TryGetAmmoString(verbTarget.verb, out List<Tuple<HediffOption, HediffResource>> hediffs))
				{
					var butRect = new Rect(topLeft.x, topLeft.y, verbTarget.GetWidth(maxWidth), 75f);
					for (var i = 0; i < hediffs.Count; i++)
                    {
						var pos = 35f - ((i + 1) * 18f);
						Vector2 vector = new Vector2(5f, pos);
						Text.Font = GameFont.Tiny;
						GUI.color = hediffs[i].Item2.def.defaultLabelColor;
						Widgets.Label(new Rect(butRect.x + vector.x, butRect.y + vector.y, butRect.width - 3f, 75f - pos), hediffs[i].Item1.minimumResourcePerUse.ToString());
						GUI.color = Color.white;
						Text.Font = GameFont.Small;
					}
				}
			}
		}

		public static bool TryGetAmmoString(Verb verb, out List<Tuple<HediffOption, HediffResource>> hediffs)
		{
			hediffs = new List<Tuple<HediffOption, HediffResource>>();
			if (verb.CasterIsPawn)
			{
				var resourceProps = GetResourceProps(verb);
				if (resourceProps != null)
                {
					var resourceSettings = resourceProps.ResourceSettings;
					if (resourceSettings != null)
                    {
						foreach (var option in resourceSettings)
						{
							var resourceHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
							if (resourceHediff != null)
							{
								hediffs.Add(new Tuple<HediffOption, HediffResource>(option, resourceHediff));
							}
						}
					}
                }
			}
			if (hediffs.Count > 0)
            {
				return true;
            }
			return false;
		}

		private static IResourceProps GetResourceProps(Verb verb)
        {
			if (verb.verbProps is IResourceProps verbResourceProps) return verbResourceProps;
			if (verb.tool is IResourceProps toolResourceProps) return toolResourceProps;
			return null;
		}
	}
}
