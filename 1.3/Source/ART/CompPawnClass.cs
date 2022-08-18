using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public int MaxLevel => ClassTrait.maxLevel;
        public Ability GetLearnedAbility(AbilityDef abilityDef) => compAbilities.LearnedAbilities.FirstOrDefault(x => x.def == abilityDef);
        public bool HasClass(out ClassTraitDef classTrait)
        {
            classTrait = ClassTrait;
            return classTrait != null;
        }
        public void GainXP(float xp)
        {
            if (level < MaxLevel)
            {
                xpPoints += xp;
                while (xpPoints >= previousXp + RequiredXPtoGain)
                {
                    level++;
                    if (pawn.Spawned && PawnUtility.ShouldSendNotificationAbout(pawn))
                    {
                        Messages.Message("ART.PawnLevelUp".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
                    }
                    abilityPoints += ClassTrait.abilityPointsPerLevel;
                    previousXp += RequiredXPtoGain;
                    if (level >= MaxLevel)
                    {
                        return;
                    }
                }
            }
        }

        public void Init(ClassTraitDef trait)
        {
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
            foreach (var tree in ClassTrait.classAbilities)
            {
                abilityTier = tree.abilityTiers.First(x => x.abilityDef == abilityDef);
                if (abilityTier != null)
                {
                    abilityTree = tree;
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

            comp.GiveAbility(abilityDef);

            if (spendAbilityPoints)
            {
                var abilityPointsToSpent = abilityData.abilityTier.abilityPointsToLearn;
                abilityPoints -= abilityPointsToSpent;
            }
        }

        public void Erase()
        {
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
            xpPoints = 0;
            abilityPoints = 0;
        }

        public ClassTraitDef ClassTrait
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
