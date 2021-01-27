using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class HediffResource : HediffWithComps
    {
        public new HediffResourceDef def => base.def as HediffResourceDef;
        private float resourceAmount;
        public float ResourceAmount
        {
            get
            {
                return resourceAmount;
            }
            set
            {
                resourceAmount = value;
                if (resourceAmount > ResourceCapacity)
                {
                    resourceAmount = ResourceCapacity;
                }
                if (resourceAmount < 0)
                {
                    this.pawn.health.RemoveHediff(this);
                }
                else
                {
                    this.Severity = resourceAmount;
                }
            }
        }

        public float ResourceCapacity
        {
            get
            {
                return this.def.maxResourceCapacity + HediffResourceUtils.GetHediffResourceCapacityGainFor(this.pawn, def);
            }
        }
        public override string Label
        {
            get 
            {
                return base.Label + ": " + this.ResourceAmount.ToStringDecimalIfSmall() + " / " + this.ResourceCapacity.ToStringDecimalIfSmall();
            }
        }

        public override string TipStringExtra
        {
            get
            {
                return base.TipStringExtra + "\n" + "HRF.Fulfils".Translate((TotalResourceGainAmount()).ToStringDecimalIfSmall());
            }
        }

        public override bool ShouldRemove
        {
            get
            {
                if (this.def.keepWhenEmpty && this.ResourceAmount <= 0)
                {
                    return false;
                }
                return base.ShouldRemove;
            }
        }
        public float TotalResourceGainAmount()
        {
            float num = 0;
            var apparels = pawn.apparel?.WornApparel?.ToList();
            if (apparels != null)
            {
                foreach (var apparel in apparels)
                {
                    var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
                    if (hediffComp?.Props.hediffOptions != null)
                    {
                        foreach (var option in hediffComp.Props.hediffOptions)
                        {
                            if (option.hediff == def)
                            {
                                var num2 = option.resourceOffset;
                                num2 *= 0.00333333341f;
                                if (option.qualityScalesResourceOffset && apparel.TryGetQuality(out QualityCategory qc))
                                {
                                    num2 *= HediffResourceUtils.GetQualityMultiplier(qc);
                                }
                                num += num2;
                            }
                        }
                    }
                }
            }

            var equipments = pawn.equipment.AllEquipmentListForReading;
            if (equipments != null)
            {
                foreach (var equipment in equipments)
                {
                    var hediffComp = equipment.GetComp<CompWeaponAdjustHediffs>();
                    if (hediffComp?.Props.hediffOptions != null)
                    {
                        foreach (var option in hediffComp.Props.hediffOptions)
                        {
                            if (option.hediff == def)
                            {
                                var num2 = option.resourceOffset;
                                num2 *= 0.00333333341f;
                                if (option.qualityScalesResourceOffset && equipment.TryGetQuality(out QualityCategory qc))
                                {
                                    num2 *= HediffResourceUtils.GetQualityMultiplier(qc);
                                }
                                num += num2;
                            }
                        }
                    }
                }
            }

            var hediffCompResourcePerDay = this.TryGetComp<HediffComp_ResourcePerDay>();
            if (hediffCompResourcePerDay != null)
            {
                float num2 = hediffCompResourcePerDay.ResourceChangePerDay();
                num2 *= 0.00333333341f;
                num2 /= 3.33f;
                num += num2;
            }
            return num;
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (this.def.resourceGainPerDamages != null && this.def.resourceGainPerDamages.resourceGainOffsets.TryGetValue(dinfo.Def.defName, out float resourceGain))
            {
                this.ResourceAmount += resourceGain;
            }
            else if (this.def.resourceGainPerAllDamages != 0f)
            {
                this.ResourceAmount += this.def.resourceGainPerAllDamages;
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.resourceAmount = this.def.initialResourceAmount;
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            var apparels = pawn.apparel?.WornApparel?.ToList();
            if (apparels != null)
            {
                foreach (var apparel in apparels)
                {
                    var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
                    if (hediffComp?.Props.hediffOptions != null)
                    {
                        foreach (var option in hediffComp.Props.hediffOptions)
                        {
                            if (option.dropApparelIfEmptyNullHediff && option.hediff == def)
                            {
                                pawn.apparel.TryDrop(apparel);
                            }
                        }
                    }
                }
            }

            var equipments = pawn.equipment.AllEquipmentListForReading;
            if (equipments != null)
            {
                foreach (var equipment in equipments)
                {
                    var hediffComp = equipment.GetComp<CompWeaponAdjustHediffs>();
                    if (hediffComp?.Props.hediffOptions != null)
                    {
                        foreach (var option in hediffComp.Props.hediffOptions)
                        {
                            if (option.dropWeaponIfEmptyNullHediff && option.hediff == def)
                            {
                                pawn.equipment.TryDropEquipment(equipment, out ThingWithComps result, pawn.Position);
                            }
                        }
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref resourceAmount, "resourceAmount");
        }
    }
}
