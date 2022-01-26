using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class StuffExtension : DefModExtension
    {
        public List<HediffOption> resourceSettings;
        public List<AdditionalDamage> additionalDamages;

        public void DamageThing(Thing damager, Thing thing, BodyPartRecord hitPart, bool isRanged, bool isMelee)
        {
            foreach (var additionalDamage in additionalDamages)
            {
                if (isRanged && additionalDamage.damageRange || isMelee && additionalDamage.damageMelee)
                {
                    var damageAmount = GetDamageAmount(additionalDamage, damager, thing);
                    var damage = new DamageInfo(additionalDamage.damage, damageAmount, hitPart: hitPart);
                    thing.TakeDamage(damage);
                }
            }
        }

        public float GetDamageAmount(AdditionalDamage additionalDamage, Thing damager, Thing thing)
        {
            float num = additionalDamage.amount.RandomInRange;
            num *= damager.GetStatValue(HRF_DefOf.HFR_StuffDamageFactor);
            return num;
        }
    }
}
