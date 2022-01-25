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
    public class HediffResource : HediffWithComps, IResourceGenerator
    {
        public new HediffResourceDef def => base.def as HediffResourceDef;
        private float resourceAmount;
        public int duration;
        public int delayTicks;
        public HediffStageResource CurStageResource => this.CurStage as HediffStageResource;
        public IEnumerable<Tuple<CompAdjustHediffs, HediffOption, ResourceStorage>> GetResourceTupleStorages()
        {
            foreach (var adjustResource in this.pawn.GetAllAdjustHediffsComps())
            {
                if (adjustResource is CompAdjustHediffs comp)
                {
                    foreach (var pair in comp.GetResourceStoragesFor(this.def))
                    {
                        yield return new Tuple<CompAdjustHediffs, HediffOption, ResourceStorage>(comp, pair.Item2, pair.Item3);
                    }
                }
            }
        }
        public float ResourceFromStorages
        {
            get
            {
                var amount = 0f;
                foreach (var tuple in GetResourceTupleStorages())
                {
                    amount += tuple.Item3.ResourceAmount;
                }
                return amount;
            }
        }
        public float StoragesTotalCapacity
        {
            get
            {
                var amount = 0f;
                foreach (var tuple in GetResourceTupleStorages())
                {
                    amount += tuple.Item3.ResourceCapacity;
                }
                return amount;
            }
        }
        public float ResourceAmountNoStorages => resourceAmount;
        public float ResourceAmount
        {
            get
            {
                return resourceAmount + ResourceFromStorages;
            }
            set
            {
                var storages = GetResourceTupleStorages();
                var totalValue = resourceAmount + ResourceFromStorages;
                var toChange = value - totalValue;
                if (toChange > 0)
                {
                    var toAdd = Mathf.Min(toChange, ResourceCapacityInt - resourceAmount);
                    toChange -= toAdd;
                    resourceAmount += toAdd;
                    while (toChange > 0)
                    {
                        bool changed = false;
                        foreach (var storage in storages)
                        {
                            if (storage.Item2.hediff == this.def)
                            {
                                toAdd = Mathf.Min(toChange, storage.Item3.ResourceCapacity - storage.Item3.ResourceAmount);
                                if (toAdd > 0)
                                {
                                    changed = true;
                                    storage.Item3.ResourceAmount += toAdd;
                                    toChange -= toAdd;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        if (!changed)
                        {
                            break;
                        }
                    }
                }

                if (toChange < 0)
                {
                    var toSubtract = resourceAmount >= Math.Abs(toChange) ? toChange : -resourceAmount;
                    toChange -= toSubtract;
                    resourceAmount += toSubtract;
                    while (toChange < 0)
                    {
                        bool changed = false;
                        foreach (var storage in storages)
                        {
                            if (storage.Item2.hediff == this.def)
                            {
                                toSubtract = storage.Item3.ResourceAmount >= Math.Abs(toChange) ? toChange : -storage.Item3.ResourceAmount;
                                if (toSubtract < 0)
                                {
                                    changed = true;
                                    storage.Item3.ResourceAmount += toSubtract;
                                    toChange -= toSubtract;
                                }
                            }
                        }
                        if (!changed)
                        {
                            break;
                        }
                    }
                }

                var storagesToDestroy = new List<CompAdjustHediffs>();
                foreach (var storage in storages)
                {
                    if (storage.Item2.destroyWhenEmpty && storage.Item3.ResourceAmount <= 0 ||
                        storage.Item2.destroyWhenFull && storage.Item3.ResourceAmount >= storage.Item3.ResourceCapacity)
                    {
                        storagesToDestroy.Add(storage.Item1);
                    }
                }
                foreach (var comp in storagesToDestroy)
                {
                    comp.parent.Destroy();
                }

                var storagesToDrop = new List<CompAdjustHediffs>();
                foreach (var storage in storages)
                {
                    if (storage.Item2.dropWhenEmpty && storage.Item3.ResourceAmount <= 0 ||
                        storage.Item2.dropWhenFull && storage.Item3.ResourceAmount >= storage.Item3.ResourceCapacity)
                    {
                        storagesToDrop.Add(storage.Item1);
                    }
                }

                foreach (var comp in storagesToDrop)
                {
                    comp.Drop();
                }

                var storagesToUnforbid = new List<CompAdjustHediffs>();
                foreach (var storage in storages)
                {
                    if (storage.Item2.unforbidWhenEmpty && storage.Item3.ResourceAmount <= 0 ||
                        storage.Item2.unforbidWhenFull && storage.Item3.ResourceAmount >= storage.Item3.ResourceCapacity)
                    {
                        storagesToUnforbid.Add(storage.Item1);
                    }
                }
                
                foreach (var comp in storagesToUnforbid)
                {
                    comp.parent.SetForbidden(false);
                }

                if (resourceAmount <= 0 && ResourceFromStorages <= 0 && !this.def.keepWhenEmpty)
                {
                    this.pawn.health.RemoveHediff(this);
                }
                else
                {
                    this.Severity = ResourceAmount;
                }
            }
        }
        public bool CanGainResource => Find.TickManager.TicksGame > this.delayTicks;

        private float ResourceCapacityInt
        {
            get
            {
                return this.def.maxResourceCapacity + HediffResourceUtils.GetHediffResourceCapacityGainFor(this.pawn, def) + GetHediffResourceCapacityGainFromAmplifiers();
            }
        }
        public float ResourceCapacity
        {
            get
            {
                var value = ResourceCapacityInt;
                return value;
            }
        }

        public bool CanGainCapacity(float newCapacity)
        {
            return ResourceCapacity > newCapacity || ResourceCapacity > 0;
        }

        public void AddDelay(int newDelayTicks)
        {
            this.delayTicks = Find.TickManager.TicksGame + newDelayTicks;
        }
        public bool CanHaveDelay(int newDelayTicks)
        {
            if (Find.TickManager.TicksGame > delayTicks || newDelayTicks > Find.TickManager.TicksGame - delayTicks)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanApplyStatBooster(StatBooster statBooster, out string failReason)
        {
            failReason = "";
            if (statBooster.resourceOnComplete != -1f && this.ResourceAmount < -statBooster.resourceOnComplete)
            {
                failReason = "HRF.ConsumesOnComplete".Translate(-statBooster.resourceOnComplete, this.def.label);
                return false;
            }
            else if (statBooster.resourcePerSecond != -1 && this.ResourceAmount < -statBooster.resourcePerSecond)
            {
                failReason = "HRF.ConsumesPerSecond".Translate(-statBooster.resourcePerSecond, this.def.label);
                return false;
            }
            return true;
        }

        public List<Thing> amplifiers = new List<Thing>();

        public Dictionary<Thing, IAdjustResouceInArea> cachedAmplifiers = new Dictionary<Thing, IAdjustResouceInArea>();

        public float GetHediffResourceCapacityGainFromAmplifiers()
        {
            float num = 0;
            foreach (var compAmplifier in Amplifiers)
            {
                num += compAmplifier.GetResourceCapacityGainFor(this.def);
            }
            return num;
        }
        public IAdjustResouceInArea GetCompAmplifierFor(Thing thing)
        {
            if (!cachedAmplifiers.TryGetValue(thing, out IAdjustResouceInArea comp))
            {
                comp = thing.TryGetComp<CompAdjustHediffsArea>();
                cachedAmplifiers[thing] = comp;
            }
            return comp;
        }
        public IEnumerable<IAdjustResouceInArea> Amplifiers
        {
            get
            {
                for (int num = amplifiers.Count - 1; num >= 0; num--)
                {
                    var amplifier = amplifiers[num];
                    if (amplifier is null || amplifier.Destroyed)
                    {
                        amplifiers.RemoveAt(num);
                    }
                    else
                    {
                        var comp = GetCompAmplifierFor(amplifier);
                        if (comp != null && comp.InRadiusFor(this.pawn.Position, this.def))
                        {
                            yield return comp;
                        }
                        else
                        {
                            amplifiers.RemoveAt(num);
                        }
                    }
                }
            }
        }

        public void TryAddAmplifier(IAdjustResouceInArea comp)
        {
            if (!amplifiers.Contains(comp.Parent))
            {
                amplifiers.Add(comp.Parent);
                cachedAmplifiers[comp.Parent] = comp;
            }
        }

        public IEnumerable<IAdjustResouceInArea> GetAmplifiersFor(HediffResourceDef hediffResourceDef)
        {
            foreach (var amplifier in Amplifiers)
            {
                foreach (var option in amplifier.ResourceSettings)
                {
                    if (option.hediff == hediffResourceDef)
                    {
                        yield return amplifier;
                    }
                }
            }
        }
        public override string Label
        {
            get 
            {
                var label = base.Label;
                if (!def.hideResourceAmount)
                {
                    label += ": " + this.resourceAmount.ToStringDecimalIfSmall() + "/" + this.ResourceCapacity.ToStringDecimalIfSmall();
                    if (StoragesTotalCapacity > 0)
                    {
                        label += " (" + ResourceFromStorages + "/" + StoragesTotalCapacity + " " + "HRF.Stored".Translate() + ")";
                    }
                }
                if (this.def.lifetimeTicks != -1)
                {
                    label += " (" + Mathf.CeilToInt((this.def.lifetimeTicks - this.duration).TicksToSeconds()) + "s)";
                }
                if (this.CurStage is HediffStageResource hediffStageResource && hediffStageResource.effectWhenDowned != null && hediffStageResource.effectWhenDowned.ticksBetweenActivations > 0)
                {
                    if (HediffResourceManager.Instance.pawnDownedStates.TryGetValue(pawn, out var state))
                    {
                        if (state.lastDownedEffectTicks.TryGetValue(this.def, out var value))
                        {
                            var enabledInTick = value + hediffStageResource.effectWhenDowned.ticksBetweenActivations;
                            if (enabledInTick > Find.TickManager.TicksGame)
                            {
                                label += " (" + "HRF.WillBeActiveIn".Translate((enabledInTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()) + ")";
                            }
                        }
                    }
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
                if (SourceOnlyAmplifiers())
                {
                    return true;
                }
                var value = base.ShouldRemove;
                if (value)
                {
                    HRFLog.Message("Removing: " + this + " this.ResourceAmount: " + this.ResourceAmount + " - this.Severity: " + this.Severity);
                }
                return value;
            }
        }
        
        public bool SourceOnlyAmplifiers()
        {
            var amplifiers = Amplifiers;
            if (!this.def.keepWhenEmpty && amplifiers.Any())
            {
                foreach (var amplifier in amplifiers)
                {
                    var comp = GetCompAmplifierFor(amplifier.Parent);
                    if (comp != null && !comp.InRadiusFor(this.pawn.Position, this.def))
                    {
                        var option = comp.GetFirstHediffOptionFor(this.def);
                        if (option.removeOutsideArea)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void Tick()
        {
            base.Tick();
            this.duration++;
            if (Find.TickManager.TicksGame % 30 == 0 && ResourceCapacity < 0)
            {
                HediffResourceUtils.TryDropExcessHediffGears(this.pawn);
            }
        }
        
        private Vector3 impactAngleVect;

        private int lastAbsorbDamageTick = -9999;
        public void AbsorbedDamage(DamageInfo dinfo)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            Vector3 loc = base.pawn.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
            float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
            FleckMaker.Static(loc, base.pawn.Map, FleckDefOf.ExplosionFlash, num);
            int num2 = (int)num;
            for (int i = 0; i < num2; i++)
            {
                FleckMaker.ThrowDustPuff(loc, base.pawn.Map, Rand.Range(0.8f, 1.2f));
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
                    bubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, CurStageResource.shieldProperties.shieldColor);
                }
                return bubbleMat;
            }
        }
        public void Draw()
        {
            if (this.CurStage is HediffStageResource hediffStageResource && hediffStageResource.ShieldIsActive(pawn) && this.ResourceAmount > 0)
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

            var comps = HediffResourceUtils.GetAllAdjustHediffsComps(this.pawn);
            foreach (var comp in comps)
            {
                var resourceSettings = comp.ResourceSettings;
                if (resourceSettings != null)
                {
                    foreach (var option in resourceSettings)
                    {
                        if (option.hediff == def)
                        {
                            var num2 = option.GetResourceGain(comp);
                            num += num2;
                        }
                    }
                }
            }

            var hediffCompResourcePerDay = this.TryGetComp<HediffComp_ResourcePerSecond>();
            if (hediffCompResourcePerDay != null)
            {
                num += hediffCompResourcePerDay.Props.resourcePerSecond;
            }
            return num;
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (this.CurStage is HediffStageResource hediffStageResource)
            {
                if (hediffStageResource.resourceGainPerDamages != null)
                {
                    foreach (var value in hediffStageResource.resourceGainPerDamages)
                    {
                        if (dinfo.Def != null && value.damageDef == dinfo.Def)
                        {
                            this.ResourceAmount += value.GetResourceGain(totalDamageDealt);
                        }
                    }
                }
                else if (hediffStageResource.resourceGainPerAllDamages != 0f)
                {
                    this.ResourceAmount += hediffStageResource.resourceGainPerAllDamages;
                }
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            HRFLog.Message(this.def.defName + " adding resource hediff to " + this.pawn);

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
            HRFLog.Message(this.def.defName + " removing resource hediff from " + this.pawn);

            var comps = HediffResourceUtils.GetAllAdjustHediffsComps(this.pawn);
            foreach (var comp in comps)
            {
                var resourceSettings = comp.ResourceSettings;
                if (resourceSettings != null)
                {
                    foreach (var option in resourceSettings)
                    {
                        if (option.dropIfHediffMissing && option.hediff == def)
                        {
                            comp.Drop();
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
            Scribe_Values.Look(ref delayTicks, "delayTicks");
            Scribe_Collections.Look(ref amplifiers, "amplifiers", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                foreach (var amplifier in amplifiers)
                {
                    cachedAmplifiers[amplifier] = amplifier.TryGetComp<CompAdjustHediffsArea>();
                }
            }
        }
    }
}
