using UnityEngine;
using Verse;

namespace HediffResourceFramework
{
    [StaticConstructorOnStartup]
	public class Gizmo_ResourceStatus : Gizmo
	{
		private HediffResource hediffResource;

		private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
		public Gizmo_ResourceStatus(HediffResource hediffResource)
		{
			order = -100f;
			this.hediffResource = hediffResource;
			resourceStorageCapacityCache = new FloatValueCache(hediffResource.StoragesTotalCapacity);
			resourceCapacityCache = new FloatValueCache(hediffResource.ResourceCapacity);
			resourceAmountCache = new FloatValueCache(hediffResource.ResourceAmountNoStorages);
			resourceBatteryAmountCache = new FloatValueCache(hediffResource.ResourceFromStorages);
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

		public FloatValueCache resourceStorageCapacityCache;
		public FloatValueCache resourceCapacityCache;
		public FloatValueCache resourceAmountCache;
		public FloatValueCache resourceBatteryAmountCache;
		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect contractedBox = rect.ContractedBy(6f);
			DrawWindowBackground(rect, hediffResource.def);
			Rect rect3 = contractedBox;
			rect3.height = rect.height / 2f;
			Text.Font = GameFont.Tiny;
			var label = hediffResource.def.LabelCap;
			if (hediffResource.def.lifetimeTicks != -1)
			{
				label += " (" + Mathf.CeilToInt((hediffResource.def.lifetimeTicks - hediffResource.duration).TicksToSeconds()) + "s)";
			}
			Widgets.Label(rect3, label);

			if (Find.TickManager.TicksGame > resourceCapacityCache.updateTick + 30)
			{
				resourceCapacityCache.Value = hediffResource.ResourceCapacity;
				resourceAmountCache.Value = hediffResource.ResourceAmountNoStorages;
				resourceBatteryAmountCache.Value = hediffResource.ResourceFromStorages;
				resourceStorageCapacityCache.Value = hediffResource.StoragesTotalCapacity;
			}

			var resourceAmount = resourceAmountCache.Value;
			var resourceCapacity = resourceCapacityCache.Value;
			var resourceStorage = resourceBatteryAmountCache.Value;
			var resourceStorageCapacity = resourceBatteryAmountCache.Value;

			Rect resourceAmountBar = contractedBox;
			resourceAmountBar.height = 18;
			resourceAmountBar.y += 21;

			float fillPercent = resourceAmount / resourceCapacity;

			Widgets.FillableBar(resourceAmountBar, fillPercent, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			if (hediffResource.def.resourceBarTextColor.HasValue)
            {
				GUI.color = hediffResource.def.resourceBarTextColor.Value;
			}
			Widgets.Label(resourceAmountBar, (resourceAmount).ToString("F0") + " / " + (resourceCapacity).ToString("F0"));

			Rect resourceStorageBar = resourceAmountBar;
			resourceStorageBar.y += 25;
			fillPercent = resourceStorage / resourceStorageCapacity;
			Widgets.FillableBar(resourceStorageBar, fillPercent, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
			if (hediffResource.def.resourceBarTextColor.HasValue)
			{
				GUI.color = hediffResource.def.resourceBarTextColor.Value;
			}
			Widgets.Label(resourceStorageBar, (resourceStorage).ToString("F0") + " / " + (resourceStorageCapacity).ToString("F0"));

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
}
