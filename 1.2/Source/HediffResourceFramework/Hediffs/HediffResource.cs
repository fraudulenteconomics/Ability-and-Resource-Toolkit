using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HediffResourceFramework
{
    public class HediffResource : HediffWithComps
    {
        public new HediffResourceDef def => base.def as HediffResourceDef;
        private float resourceAmount;
        public int duration;
        public int delayTicks;
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
                    if (ResourceCapacity == 0)
                    {
                        Log.Message($"{this} Resource amount ({resourceAmount}) is bigger than ResourceCapacity, setting it to {ResourceCapacity}");
                    }
                    resourceAmount = ResourceCapacity;
                }

                if (resourceAmount < 0)
                {
                    resourceAmount = 0;
                }

                if (resourceAmount <= 0 && !this.def.keepWhenEmpty)
                {
                    Log.Message($"Removing hediffResource: " + this);
                    this.pawn.health.RemoveHediff(this);
                }
                else
                {
                    this.Severity = resourceAmount;
                    Log.Message($"Adjusting severity ({this.Severity}): " + this.def.defName);
                }
            }
        }

        public bool CanGainResource()
        {
            return Find.TickManager.TicksGame > this.delayTicks;
        }
        public void AddDelay(int newDelayTicks)
        {
            this.delayTicks = Find.TickManager.TicksGame + newDelayTicks;
            Log.Message($"Setting new delay to {delayTicks}");
        }
        public bool CanHaveDelay(int newDelayTicks)
        {
            Log.Message($"newDelayTicks: {newDelayTicks}, Find.TickManager.TicksGame: {Find.TickManager.TicksGame}, delayTicks: {delayTicks}, Find.TickManager.TicksGame - delayTicks: {Find.TickManager.TicksGame - delayTicks}");
            if (Find.TickManager.TicksGame > delayTicks || newDelayTicks > Find.TickManager.TicksGame - delayTicks)
            {
                Log.Message($"{this} can have new delay ticks {newDelayTicks}");
                return true;
            }
            else
            {
                Log.Message($"{this} can't have new delay ticks {newDelayTicks}, cur delay ticks: {delayTicks}");
                return false;
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
                var label = base.Label + ": " + this.ResourceAmount.ToStringDecimalIfSmall() + " / " + this.ResourceCapacity.ToStringDecimalIfSmall();
                if (this.def.lifetimeTicks != -1)
                {
                    label += " (" + Mathf.CeilToInt((this.def.lifetimeTicks - this.duration).TicksToSeconds()) + "s)";
                }
                return label;
            }
        }

        public override string TipStringExtra
        {
            get
            {
                return base.TipStringExtra + "\n" + this.def.fulfilsTranslationKey.Translate((TotalResourceGainAmount()).ToStringDecimalIfSmall());
            }
        }

        public override bool ShouldRemove
        {
            get
            {
                if (this.def.lifetimeTicks != -1 && duration > this.def.lifetimeTicks)
                {
                    return true;
                }
                if (this.def.keepWhenEmpty && this.ResourceAmount <= 0)
                {
                    return false;
                }
                var value = base.ShouldRemove;
                if (value)
                {
                    Log.Message("Removing: " + this + " this.ResourceAmount: " + this.ResourceAmount + " - this.Severity: " + this.Severity);
                }
                return value;
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.duration++;
        }

        
        private Vector3 impactAngleVect;

        private int lastAbsorbDamageTick = -9999;
        public void AbsorbedDamage(DamageInfo dinfo)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            Vector3 loc = base.pawn.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
            float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
            MoteMaker.MakeStaticMote(loc, base.pawn.Map, ThingDefOf.Mote_ExplosionFlash, num);
            int num2 = (int)num;
            for (int i = 0; i < num2; i++)
            {
                MoteMaker.ThrowDustPuff(loc, base.pawn.Map, Rand.Range(0.8f, 1.2f));
            }
            lastAbsorbDamageTick = Find.TickManager.TicksGame;
        }

        private Material bubbleMat;

        public Material BubbleMat
        {
            get
            {
                if (bubbleMat is null)
                {
                    bubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, this.def.shieldProperties.shieldColor);
                }
                return bubbleMat;
            }
        }
        public void Draw()
        {
            if (this.def.shieldProperties != null && this.ResourceAmount > 0)
            {
                float num = Mathf.Lerp(1.2f, 1.55f, this.def.lifetimeTicks != -1 ? (this.def.lifetimeTicks - duration) / this.def.lifetimeTicks : 1);
                Vector3 drawPos = base.pawn.Drawer.DrawPos;
                drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                int num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
                if (num2 < 8)
                {
                    float num3 = (float)(8 - num2) / 8f * 0.05f;
                    drawPos += impactAngleVect * num3;
                    num -= num3;
                }
                float angle = Rand.Range(0, 360);
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat, 0);
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
                                var num2 = option.resourcePerSecond;
                                num2 *= 0.00333333341f;
                                if (option.qualityScalesResourcePerSecond && apparel.TryGetQuality(out QualityCategory qc))
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
                                var num2 = option.resourcePerSecond;
                                num2 *= 0.00333333341f;
                                if (option.qualityScalesResourcePerSecond && equipment.TryGetQuality(out QualityCategory qc))
                                {
                                    num2 *= HediffResourceUtils.GetQualityMultiplier(qc);
                                }
                                num += num2;
                            }
                        }
                    }
                }
            }

            var hediffCompResourcePerDay = this.TryGetComp<HediffComp_ResourcePerSecond>();
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
            this.duration = 0;
            if (this.def.sendLetterWhenGained && this.pawn.Faction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter(this.def.letterTitleKey.Translate(this.pawn.Named("PAWN"), this.def.Named("RESOURCE")),
                    this.def.letterMessageKey.Translate(this.pawn.Named("PAWN"), this.def.Named("RESOURCE")), this.def.letterType, this.pawn);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            var apparels = pawn.apparel?.WornApparel?.ToList();
            if (apparels != null)
            {
                for (int num = apparels.Count - 1; num >= 0; num--)
                {
                    var apparel = apparels[num];
                    var hediffComp = apparel.GetComp<CompApparelAdjustHediffs>();
                    if (hediffComp?.Props.hediffOptions != null)
                    {
                        foreach (var option in hediffComp.Props.hediffOptions)
                        {
                            if (option.dropIfHediffMissing && option.hediff == def)
                            {
                                if (pawn.Map != null)
                                {
                                    pawn.apparel.TryDrop(apparel);
                                }
                                else
                                {
                                    pawn.inventory.TryAddItemNotForSale(apparel);
                                }
                            }
                        }
                    }
                }
            }

            var equipments = pawn.equipment.AllEquipmentListForReading;
            if (equipments != null)
            {
                for (int num = equipments.Count - 1; num >= 0; num--)
                {
                    var equipment = equipments[num];
                    var hediffComp = equipment.GetComp<CompWeaponAdjustHediffs>();
                    if (hediffComp?.Props.hediffOptions != null)
                    {
                        foreach (var option in hediffComp.Props.hediffOptions)
                        {
                            if (option.dropIfHediffMissing && option.hediff == def)
                            {
                                if (pawn.Map != null)
                                {
                                    pawn.equipment.TryDropEquipment(equipment, out ThingWithComps result, pawn.Position);
                                }
                                else
                                {
                                    pawn.inventory.TryAddItemNotForSale(equipment);
                                }
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
            Scribe_Values.Look(ref duration, "duration");
            //Scribe_Values.Look(ref delayTicks, "delayTicks");
        }
    }
}
