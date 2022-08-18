using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VFECore.Abilities;
using Ability = VFECore.Abilities.Ability;
using AbilityDef = VFECore.Abilities.AbilityDef;

namespace ART
{
    public class AbilityData
    {
        public AbilityTreeDef abilityTree;
        public AbilityTier abilityTier;
    }
    public class CompProperties_PawnClass : CompProperties
    {
        public CompProperties_PawnClass()
        {
            this.compClass = typeof(CompPawnClass);
        }
    }

    [HotSwappable]
    public class CompPawnClass : ThingComp
    {
        public int level;
        public float xpPoints;
        public int abilityPoints;
        private float previousXp;
        public float GainedXPSinceLastLevel => xpPoints - previousXp;
        public float RequiredXPtoGain => 100 * (level + 1);
        public Pawn pawn => this.parent as Pawn;
        public CompAbilities compAbilities => pawn.GetComp<CompAbilities>();

        public Dictionary<AbilityTreeDef, int> abilityLevels;
        public int MaxLevel => ClassTraitDef.maxLevel;
        public HediffResource HediffResource
        {
            get
            {
                var classTrait = ClassTraitDef;
                if (classTrait.resourceHediff != null)
                {
                    return this.pawn.health.hediffSet.GetFirstHediffOfDef(classTrait.resourceHediff) as HediffResource;
                }
                return null;
            }
        }
        public Ability GetLearnedAbility(AbilityDef abilityDef) => compAbilities.LearnedAbilities.FirstOrDefault(x => x.def == abilityDef);
        public bool HasClass(out ClassTraitDef classTrait)
        {
            classTrait = ClassTraitDef;
            return classTrait != null;
        }
        public void GainXP(float xp)
        {
            var classTrait = ClassTraitDef;
            if (level < MaxLevel)
            {
                xpPoints += xp;
                while (xpPoints >= previousXp + RequiredXPtoGain)
                {
                    SetLevel(level + 1);
                    if (pawn.Spawned && PawnUtility.ShouldSendNotificationAbout(pawn) && classTrait.sendMessageOnLevelUp)
                    {
                        Messages.Message((classTrait.levelUpMessageKey ?? "ART.PawnLevelUp").Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
                    }
                    abilityPoints += ClassTraitDef.abilityPointsPerLevel;
                    previousXp += RequiredXPtoGain;
                    if (level >= MaxLevel)
                    {
                        return;
                    }
                }
            }
        }

        public void SetLevel(int newLevel)
        {
            this.level = newLevel;
            var hediffResouce = HediffResource;
            if (hediffResouce != null)
            {
                hediffResouce.SetResourceAmount(newLevel);
            }
        }
        public void Init(ClassTraitDef trait)
        {
            if (trait.resourceHediff != null)
                pawn.health.AddHediff(trait.resourceHediff);
            abilityLevels = new Dictionary<AbilityTreeDef, int>();
            foreach (var tree in trait.classAbilities)
            {
                abilityLevels[tree] = -1;
            }
        }

        public bool Learned(AbilityDef abilityDef)
        {
            return compAbilities.LearnedAbilities.Exists(x => x.def == abilityDef);
        }

        public bool FullyLearned(AbilityDef abilityDef)
        {
            if (Learned(abilityDef))
            {
                AbilityTreeDef abilityTree = GetAbilityDataFrom(abilityDef).abilityTree;
                var result = abilityTree.abilityTiers.Count - 1 == abilityLevels[abilityTree];
                return result;
            }
            return false;
        }

        public AbilityData GetAbilityDataFrom(AbilityDef abilityDef)
        {
            AbilityTreeDef abilityTree = null;
            AbilityTier abilityTier = null;
            foreach (var tree in ClassTraitDef.classAbilities)
            {
                abilityTier = tree.abilityTiers.FirstOrDefault(x => x.abilityDef == abilityDef);
                if (abilityTier != null)
                {
                    abilityTree = tree;
                    break;
                }
            }
            return new AbilityData { abilityTree = abilityTree, abilityTier = abilityTier };
        }

        public bool CanUnlockNextTier(AbilityDef abilityDef, out int abilityPointsToUnlock, out bool fullyUnlocked)
        {
            fullyUnlocked = false;
            abilityPointsToUnlock = 0;
            var abilityTier = GetAbilityDataFrom(abilityDef).abilityTier;
            var ability = this.GetLearnedAbility(abilityDef);
            if (ability != null)
            {
                if (FullyLearned(abilityDef))
                {
                    fullyUnlocked = true;
                    return false;
                }
            }
            if (DebugSettings.godMode)
            {
                return true;
            }
            abilityPointsToUnlock = abilityTier.abilityPointsToLearn;
            if (abilityPoints < abilityPointsToUnlock)
            {
                return false;
            }
            return true;
        }

        public void LearnAbility(AbilityDef abilityDef, bool spendAbilityPoints = true)
        {
            var comp = compAbilities;
            var abilityData = GetAbilityDataFrom(abilityDef);
            foreach (var tier in abilityData.abilityTree.abilityTiers)
            {
                var ability = GetLearnedAbility(tier.abilityDef);
                if (ability != null)
                {
                    comp.LearnedAbilities.Remove(ability);
                }
            }

            abilityLevels[abilityData.abilityTree] = abilityData.abilityTree.abilityTiers.IndexOf(abilityData.abilityTier);
            comp.GiveAbility(abilityDef);
            if (spendAbilityPoints)
            {
                var abilityPointsToSpent = abilityData.abilityTier.abilityPointsToLearn;
                abilityPoints -= abilityPointsToSpent;
            }
        }

        public void Erase(ClassTraitDef classTrait)
        {
            if (classTrait.resourceHediff != null)
            {
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(classTrait.resourceHediff);
                if (hediff != null)
                    pawn.health.RemoveHediff(hediff);
            }

            var comp = compAbilities;
            foreach (var kvp in abilityLevels)
            {
                var ability = this.GetLearnedAbility(kvp.Key.abilityTiers[kvp.Value].abilityDef);
                if (ability != null)
                {
                    comp.LearnedAbilities.Remove(ability);
                }
            }
            abilityLevels.Clear();
            level = 0;
            SetLevel(0);
            xpPoints = 0;
            abilityPoints = 0;
        }
        public Trait ClassTrait
        {
            get
            {
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    if (trait.def is ClassTraitDef classTraitDef)
                    {
                        return trait;
                    }
                }
                return null;
            }
        }
        public ClassTraitDef ClassTraitDef
        {
            get
            {
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    if (trait.def is ClassTraitDef classTraitDef)
                    {
                        return classTraitDef;
                    }
                }
                return null;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref level, "CompPawnClass_" + nameof(level));
            Scribe_Values.Look(ref xpPoints, "CompPawnClass_" + nameof(xpPoints));
            Scribe_Values.Look(ref previousXp, "CompPawnClass_" + nameof(previousXp));
            Scribe_Values.Look(ref abilityPoints, "CompPawnClass_" + nameof(abilityPoints));
            Scribe_Collections.Look(ref abilityLevels, "CompPawnClass_" + nameof(abilityLevels), LookMode.Def, LookMode.Value);
        }
    }

    public class ClassTraitDef : TraitDef
    {
        public int maxLevel;
        public int abilityPointsPerLevel;
        public float baseXp;
        public float xpPerLevelOffset;
        public float xpPerPawnValue;
        public float xpPerNonhumanValue;
        public float xpPerSkillGain;
        public bool sendMessageOnLevelUp;
        public string levelUpMessageKey;
        public string moteOnLevelUp;
        public string soundOnLevelUp;
        public float baseValue;
        public float valuePerLevelOffset;
        public HediffResourceDef resourceHediff;
        public List<AbilityTreeDef> classAbilities;

        [NoTranslate]
        public string iconPath;

        public Texture2D uiIcon = BaseContent.BadTex;
        public override void PostLoad()
        {
            if (!string.IsNullOrEmpty(iconPath))
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    uiIcon = ContentFinder<Texture2D>.Get(iconPath);
                });
            }
        }
    }

    public class AbilityTreeDef : Def
    {
        public List<AbilityTier> abilityTiers;
    }

    public class AbilityTier
    {
        public AbilityDef abilityDef;
        public int minimumLevel;
        public int abilityPointsToLearn;
    }
}
