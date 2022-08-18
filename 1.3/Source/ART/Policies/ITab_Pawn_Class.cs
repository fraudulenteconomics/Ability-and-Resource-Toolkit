using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VFECore.Abilities;
using AbilityDef = VFECore.Abilities.AbilityDef;

namespace ART
{
    [HotSwappableAttribute]
    [StaticConstructorOnStartup]
    public class ITab_Pawn_Class : ITab
    {
        public ITab_Pawn_Class()
        {
            this.size = new Vector2(500f, 470f);
            this.labelKey = "ART.Class";
        }
        public override void FillTab()
        {
            var pawn = PawnToShowInfoAbout;
            var comp = pawn.GetComp<CompPawnClass>();
            var pawnClass = comp.ClassTrait;
            var rect = new Rect(0f, 0f, this.size.x, this.size.y);
            GUI.BeginGroup(rect);
            Text.Font = GameFont.Small;

            Rect innerMenu = new Rect(15f, 15f, rect.width - 30f, rect.height - 15f);
            Vector2 pos = new Vector2(innerMenu.x, innerMenu.y);
            var skillPointsRect = new Rect(pos.x, pos.y, 180, 28f);
            Widgets.Label(skillPointsRect, "ART.SkillPointsAvailable".Translate(comp.abilityPoints));

            var nextLevelProgress = new Rect(skillPointsRect.x, skillPointsRect.yMax, 150, 28f);
            Widgets.Label(nextLevelProgress, "ART.ProgressToNextLevel".Translate());
            var nextLevelProgressBar = new Rect(nextLevelProgress.xMax, nextLevelProgress.y, 120, 20f);
            Widgets.FillableBar(nextLevelProgressBar, comp.GainedXPSinceLastLevel / comp.RequiredXPtoGain);
            TooltipHandler.TipRegion(nextLevelProgressBar, "ART.CurrentXP".Translate(comp.GainedXPSinceLastLevel, comp.RequiredXPtoGain));

            var curLevelRect = new Rect(nextLevelProgressBar.xMax + 30, skillPointsRect.y, 180, 28f);
            Widgets.Label(curLevelRect, "ART.Level".Translate(comp.level));
            pos.y += 60;

            var defs = pawn.GetAllLearnableAbilities().ToList();

            var list = new List<List<AbilityDef>>();
            var rowList = new List<AbilityDef>();
            for (int j = 0; j < defs.Count; j++)
            {
                if (j > 0 && j % 2 == 0)
                {
                    list.Add(rowList.ListFullCopy());
                    rowList = new List<AbilityDef> { defs[j] };
                }
                else
                {
                    rowList.Add(defs[j]);
                }
            }
            if (rowList.Any())
            {
                list.Add(rowList.ListFullCopy());
            }

            var lowerRect = new Rect(innerMenu.x, pos.y, innerMenu.width, innerMenu.height - 70);
            var totalRect = new Rect(lowerRect.x, lowerRect.y, lowerRect.width - 16, (list.Count * 42f));
            Widgets.BeginScrollView(lowerRect, ref scrollPosition, totalRect);

            foreach (var row in list)
            {
                pos.x = row.Count == 2 ? 50 : ((3f / row.Count) * 50f) + 50f;
                var rest = innerMenu.width - 130;
                for (var i = 0; i < row.Count; i++)
                {
                    var abilityDef = row[i];
                    var ability = comp.GetLearnedAbility(abilityDef);

                    bool learnedAbility = ability != null;
                    var abilityTier = learnedAbility ? comp.GetAbilityTier(abilityDef) : abilityDef.abilityTiers[0];

                    Text.Anchor = TextAnchor.MiddleCenter;
                    var abilityNameRect = new Rect(pos.x - 20, pos.y, 50 + 40, 32);
                    Widgets.Label(abilityNameRect, abilityDef.label);

                    var iconBox = new Rect(pos.x, abilityNameRect.yMax, 50, 50);
                    GUI.DrawTexture(iconBox, abilityDef.icon ?? abilityTier.icon);

                    var infoRect = new Rect(iconBox.x - 20, iconBox.yMax, iconBox.width + 40, 21f);
                    if (comp.FullyLearned(abilityDef))
                    {
                        Widgets.Label(infoRect, "ART.Mastered".Translate());
                    }
                    else if (learnedAbility)
                    {
                        Widgets.Label(infoRect, "ART.AbilityLevel".Translate(ability.LevelHumanReadable, abilityDef.abilityTiers.Count));
                    }
                    else
                    {
                        Widgets.Label(infoRect, "ART.NotUnlocked".Translate());
                    }
                    Text.Anchor = TextAnchor.UpperLeft;
                    TooltipHandler.TipRegion(iconBox, ability.GetDescriptionForPawn());


                    var canLearnNextTier = comp.CanUnlockNextTier(abilityDef, out var skillPointsRequirement, out var fullyUnlocked);
                    if ((pawn.HostFaction == Faction.OfPlayer || pawn.Faction == Faction.OfPlayer) && !fullyUnlocked)
                    {
                        var plusRect = new Rect(iconBox.xMax + 10, iconBox.y + ((iconBox.height / 2) - 12), 24, 24);
                        var plusSignTexture = ContentFinder<Texture2D>.Get("UI/Buttons/Plus");
                        if (canLearnNextTier && Widgets.ButtonImage(plusRect, plusSignTexture))
                        {
                            if (canLearnNextTier)
                            {
                                if (learnedAbility)
                                {
                                    comp.LearnAbility(abilityDef, ability.level + 1);
                                }
                                else
                                {
                                    comp.LearnAbility(abilityDef, 0);
                                }
                            }
                        }
                    }
                    pos.x += 150;
                    GUI.color = Color.white;
                }
                pos.y += 115;
            }


            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private Vector2 scrollPosition;
        public override bool IsVisible => this.PawnToShowInfoAbout?.HasClass(out _) ?? false;
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
                    Corpse corpse = base.SelThing as Corpse;
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