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
using Ionic.Zlib;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.SocialPlatforms;
using Verse.Noise;
using static RimWorld.FleshTypeDef;
using static RimWorld.MechClusterSketch;
using System.Runtime.Remoting.Messaging;
using Mono.Cecil;

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
        public bool affectChronic;
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

    public class DamageAuraProperties
    {
        public DamageDef damageDef;
        public int damageAmount;
        public int ticksPerEffect;
        public float effectRadius;
        public bool worksThroughWalls;
        public bool affectsAllies;
        public bool affectsEnemies = true;
        public bool affectsSelf;
        public ThingDef otherDamageMote;
        public ThingDef selfDamageMote;
        public GraphicData auraGraphic;
        public SoundDef soundOnEffect;
    }

    public class LifeStealProperties
    {
        public DamageDef damageDef;
        public float flatHeal;
        public float percentHeal;
        public bool checkOrganic;
        public bool healOverflow;
        public HealPriority healPriority;
        public float effectRadius;
        public bool affectSelf;
        public bool affectsAllies;
        public bool affectsEnemies = true;
        public bool worksThroughWalls;
        public SoundDef soundOnEffect;
        public bool affectMelee;
        public bool affectRanged;

        public void StealLife(Pawn instigator, Pawn targetPawn, DamageInfo source)
        {
            Log.Message("Stealing life: " + instigator + " - " + targetPawn + " - " + source);
            if (damageDef is null || damageDef == source.Def)
            {
                foreach (var pawn in HediffResourceUtils.GetPawnsAround(instigator, effectRadius))
                {
                    Log.Message("Checking pawn: " + pawn);
                    if (CanExtractLife(instigator, pawn, targetPawn))
                    {
                        Log.Message("Healing pawn: " + pawn);
                        var hediffsToHeal = pawn.health.hediffSet.hediffs.Where(x => x is Hediff_Injury).ToList();
                        var healPoints = flatHeal > 0 ? flatHeal : source.Amount * percentHeal;
                        HediffResourceUtils.HealHediffs(pawn, ref healPoints, hediffsToHeal, healOverflow, healPriority, false, soundOnEffect);
                    }
                    else
                    {
                        Log.Message("Cannot work on " + pawn);
                    }
                }
            }
        }
        private bool CanExtractLife(Pawn instigator, Pawn toGive, Pawn targetPawn)
        {
            if (checkOrganic && toGive.RaceProps.IsFlesh != targetPawn.RaceProps.IsFlesh
                || !worksThroughWalls && !GenSight.LineOfSight(toGive.Position, targetPawn.Position, toGive.Map)
                || !affectSelf && instigator == toGive 
                || !affectsAllies && targetPawn.Faction != null && !toGive.HostileTo(instigator)
                || !affectsEnemies && toGive.HostileTo(instigator))
            {
                return false;
            }
            return true;
        }
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
        public DamageAuraProperties damageAuraProperties;
        public LifeStealProperties lifeStealProperties;
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