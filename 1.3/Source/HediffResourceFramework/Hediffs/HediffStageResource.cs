using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
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
using static Mono.Security.X509.X520;

namespace HediffResourceFramework
{
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
        public TendingProperties tendingProperties;
        public TogglingProperties togglingProperties;
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