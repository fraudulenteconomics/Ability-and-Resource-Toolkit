using System.Collections.Generic;
using System.Linq;
using Verse;

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