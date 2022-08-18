using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VFECore.Abilities;
using Ability = VFECore.Abilities.Ability;
using AbilityDef = VFECore.Abilities.AbilityDef;

namespace ART
{
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
                AbilityTreeDef abilityTree = null;
                foreach (var tree in ClassTrait.classAbilities)
                {
                    var abilityTierDef = tree.abilityTiers.First(x => x.abilityDef == abilityDef);
                    if (abilityTierDef != null)
                    {
                        abilityTree = tree;
                    }
                }

                var result = abilityTree.abilityTiers.Count - 1 == abilityLevels[abilityTree];
                return result;
            }
            return false;
        }



        public bool CanUnlockNextTier(AbilityDef kIAbilityDef, out int skillPointsToUnlock, out bool fullyUnlocked)
        {
            fullyUnlocked = false;
            skillPointsToUnlock = 0;
            var ability = this.GetLearnedAbility(kIAbilityDef);
            if (ability != null)
            {
                if (FullyLearned(kIAbilityDef))
                {
                    fullyUnlocked = true;
                    return false;
                }
                if (DebugSettings.godMode)
                {
                    return true;
                }
                var abilityTier = kIAbilityDef.abilityTiers[ability.level + 1];
                if (!abilityTier.isLearnable)
                {
                    return false;
                }
                skillPointsToUnlock = abilityTier.skillPointsToUnlock;
                if (skillPoints < skillPointsToUnlock)
                {
                    return false;
                }
                if (curSpentSkillPoints + skillPointsToUnlock > MaxSPPoints)
                {
                    return false;
                }
            }
            else
            {
                if (DebugSettings.godMode)
                {
                    return true;
                }
                var abilityTier = kIAbilityDef.abilityTiers[0];
                if (!abilityTier.isLearnable)
                {
                    return false;
                }
                if (abilityTier.requiresAbilities != null && !abilityTier.requiresAbilities.All(x => Learned(x)))
                {
                    return false;
                }
                skillPointsToUnlock = abilityTier.skillPointsToUnlock;
                if (skillPoints < skillPointsToUnlock)
                {
                    return false;
                }
                if (curSpentSkillPoints + skillPointsToUnlock > MaxSPPoints)
                {
                    return false;
                }
            }
            return true;
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
        public string levelUpMessageKeyf;
        public string moteOnLevelUp;
        public string soundOnLevelUp;
        public float baseValue;
        public float valuePerLevelOffset;
        public HediffResourceDef resourceHediff;
        public List<AbilityTreeDef> classAbilities;
    }

    public class AbilityTreeDef
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
