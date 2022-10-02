using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ART
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }
    public class ITab_Pawn_Resource : ITab
    {
        public ITab_Pawn_Resource()
        {
            size = new Vector2(500f, 470f);
            labelKey = "ART.TabResource";
        }

        [TweakValue("0ART", 0f, 100f)] public static float hediffEntryHeight = 26f;
        [TweakValue("0ART", 0f, 300f)] public static float hediffLabelWidth = 165;
        [TweakValue("0ART", 0f, 300f)] public static float hediffBarWidth = 125f;
        [TweakValue("0ART", 0f, 300f)] public static float satisfyNeedsWidth = 115f;
        public override void FillTab()
        {
            var pawn = PawnToShowInfoAbout;
            var rect = new Rect(0f, 0f, size.x, size.y);
            GUI.BeginGroup(rect);
            Text.Font = GameFont.Small;
            var innerMenu = new Rect(10f, 10f, rect.width - 10f, rect.height - 10f);
            var pos = new Vector2(innerMenu.x + 20, innerMenu.y + 20f);

            var hediffDefs = DefDatabase<HediffResourceDef>.AllDefs.Where(x => x.showInResourceTab);
            float listHeight = hediffDefs.Count() * hediffEntryHeight;
            var viewRect = new Rect(pos.x, pos.y, rect.width - 36f, rect.height - pos.y - 20);
            var scrollRect = new Rect(pos.x, pos.y, rect.width - 53f, listHeight);
            Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollRect);

            foreach (var hediffResourceDef in hediffDefs)
            {
                var hediffPolicy = ARTManager.Instance.hediffResourcesPolicies[pawn].satisfyPolicies[hediffResourceDef];
                var hediffLabelBox = new Rect(pos.x, pos.y, hediffLabelWidth, hediffEntryHeight);
                GUI.color = hediffResourceDef.defaultLabelColor;
                Widgets.Label(hediffLabelBox, hediffResourceDef.LabelCap);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(hediffLabelBox, "ART.TabResource".Translate());
                var hediffAmountBar = new Rect(hediffLabelBox.xMax + 5, pos.y + 1, hediffBarWidth, hediffEntryHeight - 2);
                if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffResourceDef) is HediffResource hediffResource)
                {
                    var hediffBarTex = SolidColorMaterials.NewSolidColorTexture(hediffResourceDef.defaultLabelColor);
                    Widgets.FillableBar(hediffAmountBar, hediffResource.ResourceAmount / hediffResource.ResourceCapacity, hediffBarTex);
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(hediffAmountBar, "ART.MissingHediff".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                var satisfyNeedsRange = new Rect(hediffAmountBar.xMax + 5, pos.y, satisfyNeedsWidth, hediffEntryHeight);
                Widgets.FloatRange(satisfyNeedsRange, (int)pos.y, ref hediffPolicy.resourceSeekingThreshold);
                TooltipHandler.TipRegion(satisfyNeedsRange, "ART.NeedThreshold".Translate());

                var checkBox = new Vector2(satisfyNeedsRange.xMax + 5f, pos.y);
                Widgets.Checkbox(checkBox, ref hediffPolicy.seekingIsEnabled);
                TooltipHandler.TipRegion(new Rect(checkBox, new Vector2(24, 24)), "ART.ActiveSeekSatisfy".Translate());
                pos.y += hediffEntryHeight;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();

        }

        private Vector2 scrollPosition;


        public override bool IsVisible => PawnToShowInfoAbout?.health?.hediffSet?.hediffs?.OfType<HediffResource>().Any(x => x.def.showInResourceTab) ?? false;
        private Pawn PawnToShowInfoAbout
        {
            get
            {
                Pawn pawn = null;
                bool flag = base.SelPawn != null;
                if (flag)
                {
                    pawn = base.SelPawn;
                }
                else
                {
                    var corpse = base.SelThing as Corpse;
                    bool flag2 = corpse != null;
                    if (flag2)
                    {
                        pawn = corpse.InnerPawn;
                    }
                }
                bool flag3 = pawn == null;
                Pawn result;
                if (flag3)
                {
                    result = null;
                }
                else
                {
                    result = pawn;
                }
                return result;
            }
        }
    }
}