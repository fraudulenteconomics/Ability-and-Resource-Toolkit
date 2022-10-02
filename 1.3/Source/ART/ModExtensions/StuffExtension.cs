using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ART
{
    public class StuffExtension : DefModExtension
    {
        public List<ResourceProperties> resourceSettings;
        public List<AdditionalDamage> additionalDamages;

        public void DamageThing(Thing damager, Thing thing, DamageInfo source, bool isRanged, bool isMelee)
        {
            ARTLog.Message("source: " + source);
            foreach (var additionalDamage in additionalDamages)
            {
                if ((isRanged && additionalDamage.damageRange) || (isMelee && additionalDamage.damageMelee))
                {
                    float damageAmount = GetDamageAmount(additionalDamage, damager, thing);
                    var damage = new DamageInfo(additionalDamage.damage, damageAmount, instigator: source.Instigator, hitPart: source.HitPart, weapon: source.Weapon);
                    ARTLog.Message("Damaging " + thing + " with " + damage);
                    thing.TakeDamage(damage);
                }
            }
        }

        public float GetDamageAmount(AdditionalDamage additionalDamage, Thing damager, Thing thing)
        {
            float num = additionalDamage.amount.RandomInRange;
            num *= damager.GetStatValue(ART_DefOf.HFR_StuffDamageFactor);
            return num;
        }
    }
}
