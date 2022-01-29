using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Xml;
using UnityEngine.Networking.Types;
using UnityEngine;
using Verse;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace HediffResourceFramework
{
    public class IngestibleProperties
    {
        public HediffResourceDef hediffResource;
        public float resourcePerIngestion;
        public float nutritionGiven;
        public FoodTypeFlags nutritionCategories;
    }

    public class PlantSowingProperties
    {
        public HediffResourceDef requiredHediff;
        public float resourcePerSowing;
        public float growthRateOffset;
    }
    public class NeedAdjustRecord
    {
        public NeedDef need;

        public float adjustValue;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "need", xmlRoot);
            adjustValue = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }
    public class NeedAdjustProperties
    {
        public int tickRate;
        public List<NeedAdjustRecord> needsToAdjust;
    }

    public enum HealPriority
    {
        TendablesFirst,
        Random
    }
    public class HealingProperties
    {
        public float healPoints;
        public int hediffsToHeal;
        public bool affectPermanent;
        public bool affectIllness;
        public bool affectInjuries;
        public bool pointsOverflow;
        public HealPriority healPriority;
        public bool affectMechanical;
        public bool affectOrganic = true;
        public float effectRadius;
        public bool affectsAllies = true;
        public bool affectsEnemies = false;
        public int ticksPerEffect;
        public bool healOnApply;
        public SoundDef soundOnEffect;
    }

    public class SkillAdjustProperties
    {
        public SkillDef skill;
        public int skillLevelOffset;
        public int maxSkillLevel;
        public Passion forcedPassion;
    }
    public class HediffStageResource : HediffStage
    {
        public List<HediffOption> resourceSettings;
        public List<ResourceGainPerDamage> resourceGainPerDamages;
        public float resourceGainPerAllDamages;
        public ShieldProperties shieldProperties;
        public TendProperties tendProperties;
        public EffectWhenDowned effectWhenDowned;
        public RepairProperties repairProperties;
        public List<RefuelProperties> refuelProperties;
        public List<AdditionalDamage> additionalDamages;
        public bool preventDeath;
        public bool preventDowning;

        public IngestibleProperties ingestibleProperties;
        public PlantSowingProperties plantSowingProperties;
        public NeedAdjustProperties needAdjustProperties;
        public HealingProperties healingProperties;
        public List<SkillAdjustProperties> skillAdjustProperties;
        public bool ShieldIsActive(Pawn pawn)
        {
            if (shieldProperties != null)
            {
                if (shieldProperties.activeWithHediffs != null && pawn.health.hediffSet.hediffs.Any())
                {
                    return pawn.health.hediffSet.hediffs.All(x => shieldProperties.activeWithHediffs.Contains(x.def));
                }
                return true;
            }
            return false;
        }
    }
}