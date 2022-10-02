using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFECore.Abilities;
using AbilityDef = VFECore.Abilities.AbilityDef;

namespace ART
{
    public class HediffResource : Hediff_Ability, IAdjustResource, IDrawable
    {
        public new HediffResourceDef def => base.def as HediffResourceDef;
        private float resourceAmount;
        public int duration;
        public int delayTicks;
        public int lastHealingEffectTick;
        public int lastTendingEffectTick;
        public int lastDamagingEffectTick;

        public int lastStageSwitchTick;
        public int lastStageActivatedTick;
        public int stageSwitchTickToActivate;
        public int stageIndexToActivate = -1;
        public int curCooldownPeriod;
        public int curChangeTime;
        public int previousStageIndex = -1;
        public List<AbilityDef> grantedRandomAbilities;
        public List<AbilityDef> grantedStaticAbilities;

        public Dictionary<int, SavedSkillRecordCollection> savedSkillRecordsByStages;
        public List<Thing> amplifiers = new List<Thing>();
        public HediffStageResource CurStageResource => CurStage as HediffStageResource;
        public IEnumerable<Tuple<CompAdjustHediffs, ResourceProperties, ResourceStorage>> GetResourceStorages()
        {
            foreach (var adjustResource in pawn.GetAllAdjustResources())
            {
                if (adjustResource is CompAdjustHediffs comp)
                {
                    foreach (var pair in comp.GetResourceStoragesFor(def))
                    {
                        yield return new Tuple<CompAdjustHediffs, ResourceProperties, ResourceStorage>(comp, pair.Item2, pair.Item3);
                    }
                }
            }
        }
        public float ResourceFromStorages
        {
            get
            {
                float amount = 0f;
                foreach (var tuple in GetResourceStorages())
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
                float amount = 0f;
                foreach (var tuple in GetResourceStorages())
                {
                    amount += tuple.Item3.ResourceCapacity;
                }
                return amount;
            }
        }
        public float ResourceAmountNoStorages => resourceAmount;
        public float ResourceAmount => resourceAmount + ResourceFromStorages;
        public void SetResourceAmount(float value, ResourceProperties source = null)
        {
            ChangeResourceAmount(value - ResourceAmount, source);
        }
        public void ChangeResourceAmount(float offset, ResourceProperties source = null)
        {
            if (def.isResource is false)
            {
                return;
            }
            string hediffDefnameToCheck = "";// "FE_FuelPackHediff";
            float resourceCapacity = ResourceCapacity;
            float resourceFromStorages = ResourceFromStorages;
            if (def.defName == hediffDefnameToCheck)
            {
                //resourceCapacity = 10;
                //resourceAmount = 10;
                //offset = -15;
            }

            var storages = GetResourceStorages();
            float totalValue = resourceAmount + resourceFromStorages;

            if (def.defName == hediffDefnameToCheck)
            {
                ARTLog.Message("START");
                ARTLog.Message("1 offset: " + offset);
                ARTLog.Message("resourceAmount: " + resourceAmount);
                ARTLog.Message("resourceFromStorages: " + resourceFromStorages);
                ARTLog.Message("resourceCapacity: " + resourceCapacity);
                ARTLog.Message("totalValue: " + totalValue);
                ARTLog.Message("-----------------");
            }
            if (offset > 0)
            {
                float toAdd = Mathf.Min(offset, resourceCapacity - resourceAmount);
                if (def.restrictResourceCap && resourceAmount + toAdd > resourceCapacity)
                {
                    toAdd = resourceCapacity - resourceAmount;
                }
                offset -= toAdd;
                if (def.defName == hediffDefnameToCheck)
                {
                    ARTLog.Message("toAdd: " + toAdd);
                    ARTLog.Message("2 offset: " + offset);
                }
                resourceAmount += toAdd;
            }
            else if (offset < 0)
            {
                float toSubtract = -Mathf.Min(-offset, resourceAmount);
                offset -= toSubtract;
                if (def.defName == hediffDefnameToCheck)
                {
                    ARTLog.Message("toSubtract: " + toSubtract);
                    ARTLog.Message("2 offset: " + offset);
                }
                resourceAmount += toSubtract;
            }
            if (def.defName == hediffDefnameToCheck)
            {
                ARTLog.Message("this.resourceAmount: " + resourceAmount);
                ARTLog.Message("END");
            }

            if (source is null || source.canRefillStorage)
            {
                while (offset > 0)
                {
                    bool changed = false;
                    foreach (var storage in storages)
                    {
                        if (storage.Item2.hediff == def)
                        {
                            float toAdd = Mathf.Min(offset, storage.Item3.ResourceCapacity - storage.Item3.ResourceAmount);
                            if (toAdd > 0)
                            {
                                changed = true;
                                storage.Item3.ResourceAmount += toAdd;
                                offset -= toAdd;
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

            while (offset < 0)
            {
                if (def.defName == hediffDefnameToCheck)
                {
                    ARTLog.Message("WHILE: offset: " + offset);
                }

                bool changed = false;
                foreach (var storage in storages)
                {
                    if (def.defName == hediffDefnameToCheck)
                    {
                        ARTLog.Message("STORAGE: " + storage.Item2.hediff);
                    }

                    if (storage.Item2.hediff == def)
                    {
                        float toSubtract = -Mathf.Min(-offset, storage.Item3.ResourceAmount);
                        if (def.defName == hediffDefnameToCheck)
                        {
                            ARTLog.Message("toSubtract: " + toSubtract);
                        }

                        if (toSubtract < 0)
                        {
                            changed = true;
                            if (def.defName == hediffDefnameToCheck)
                            {
                                ARTLog.Message("storage.Item3: " + storage.Item3.parent + " - " + storage.Item3.resourceProperties.hediff);
                                ARTLog.Message("1 storage.Item3.ResourceAmount: " + storage.Item3.ResourceAmount);
                                ARTLog.Message("1 offset: " + offset);
                            }

                            storage.Item3.ResourceAmount += toSubtract;
                            offset -= toSubtract;
                            if (def.defName == hediffDefnameToCheck)
                            {
                                ARTLog.Message("2 storage.Item3.ResourceAmount: " + storage.Item3.ResourceAmount);
                                ARTLog.Message("2 offset: " + offset);
                            }
                        }
                    }
                }
                if (!changed)
                {
                    break;
                }
            }

            var storagesToDestroy = new List<CompAdjustHediffs>();
            foreach (var storage in storages)
            {
                if (def.defName == hediffDefnameToCheck)
                {
                    ARTLog.Message(storage.Item2.hediff + " - storage.Item3.ResourceAmount: " + storage.Item3.ResourceAmount);
                }

                if ((storage.Item2.destroyWhenEmpty && storage.Item3.ResourceAmount <= 0) ||
                    (storage.Item2.destroyWhenFull && storage.Item3.ResourceAmount >= storage.Item3.ResourceCapacity))
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
                if ((storage.Item2.dropWhenEmpty && storage.Item3.ResourceAmount <= 0) ||
                    (storage.Item2.dropWhenFull && storage.Item3.ResourceAmount >= storage.Item3.ResourceCapacity))
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
                if ((storage.Item2.unforbidWhenEmpty && storage.Item3.ResourceAmount <= 0) ||
                    (storage.Item2.unforbidWhenFull && storage.Item3.ResourceAmount >= storage.Item3.ResourceCapacity))
                {
                    storagesToUnforbid.Add(storage.Item1);
                }
            }

            foreach (var comp in storagesToUnforbid)
            {
                comp.parent.SetForbidden(false);
            }

            if (resourceAmount <= 0 && ResourceFromStorages <= 0 && !def.keepWhenEmpty)
            {
                pawn.health.RemoveHediff(this);
            }
            else
            {
                UpdateResourceData();
            }
            Gizmo_ResourceStatus.updateNow = true;
        }

        public void UpdateResourceData()
        {
            if (def.isResource)
            {
                if (def.useAbsoluteSeverity)
                {
                    Severity = ResourceAmount / ResourceCapacity;
                }
                else
                {
                    Severity = ResourceAmount;
                }
            }
        }

        public override float Severity
        {
            get => base.Severity;
            set => base.Severity = value;
        }

        public bool CanGainResource => Find.TickManager.TicksGame > delayTicks;
        public float ResourceCapacity => def.maxResourceCapacity + Utils.GetHediffResourceCapacityGainFor(pawn, def, out _) + GetHediffResourceCapacityGainFromAmplifiers(out _);

        public CompAbilities compAbilities;
        private void PreInit()
        {
            compAbilities = pawn?.GetComp<CompAbilities>();
            if (amplifiers is null)
            {
                amplifiers = new List<Thing>();
            }
            if (savedSkillRecordsByStages is null)
            {
                savedSkillRecordsByStages = new Dictionary<int, SavedSkillRecordCollection>();
            }
            if (grantedStaticAbilities is null)
            {
                grantedStaticAbilities = new List<AbilityDef>();
            }
            if (grantedRandomAbilities is null)
            {
                grantedRandomAbilities = new List<AbilityDef>();
            }
            Register();
        }
        public bool CanGainCapacity(float newCapacity)
        {
            return ResourceCapacity > newCapacity || ResourceCapacity > 0;
        }

        public void AddDelay(int newDelayTicks)
        {
            delayTicks = Find.TickManager.TicksGame + newDelayTicks;
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

        public bool CanUse(UseProps useProps, out string failReason)
        {
            failReason = "";
            if (useProps.resourceOnComplete != -1f && ResourceAmount < -useProps.resourceOnComplete)
            {
                failReason = "ART.ConsumesOnComplete".Translate(-useProps.resourceOnComplete, def.label);
                return false;
            }
            else if (useProps.resourcePerSecond != -1 && ResourceAmount < -useProps.resourcePerSecond)
            {
                failReason = "ART.ConsumesPerSecond".Translate(-useProps.resourcePerSecond, def.label);
                return false;
            }
            return true;
        }

        public Dictionary<Thing, IAdjustResouceInArea> cachedAmplifiers = new Dictionary<Thing, IAdjustResouceInArea>();
        public float GetHediffResourceCapacityGainFromAmplifiers(out StringBuilder explanation)
        {
            explanation = new StringBuilder();
            float num = 0;
            foreach (var compAmplifier in Amplifiers)
            {
                float gain = compAmplifier.GetResourceCapacityGainFor(def);
                explanation.AppendLine("ART.CapacityAmplifier".Translate(compAmplifier.Parent, gain));
                num += gain;
            }
            return num;
        }
        public IAdjustResouceInArea GetCompAmplifierFor(Thing thing)
        {
            if (!cachedAmplifiers.TryGetValue(thing, out var comp))
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
                        if (comp != null && comp.InRadiusFor(pawn.Position, def) && ARTManager.Instance.resourceAdjusters.Contains(comp))
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
                string label = base.Label;
                if (!def.hideResourceAmount)
                {
                    label += ": " + resourceAmount.ToStringDecimalIfSmall() + "/" + ResourceCapacity.ToStringDecimalIfSmall();
                    if (StoragesTotalCapacity > 0)
                    {
                        label += " (" + ResourceFromStorages.ToStringDecimalIfSmall() + "/" + StoragesTotalCapacity.ToStringDecimalIfSmall() + " " + "ART.Stored".Translate() + ")";
                    }
                }
                if (def.lifetimeTicks != -1)
                {
                    label += " (" + Mathf.CeilToInt(def.lifetimeTicks - duration).ToStringTicksToPeriod() + ")";
                }
                if (CurStage is HediffStageResource hediffStageResource && hediffStageResource.effectWhenDowned != null && hediffStageResource.effectWhenDowned.ticksBetweenActivations > 0)
                {
                    if (ARTManager.Instance.pawnDownedStates.TryGetValue(pawn, out var state))
                    {
                        if (state.lastDownedEffectTicks.TryGetValue(def, out int value))
                        {
                            int enabledInTick = value + hediffStageResource.effectWhenDowned.ticksBetweenActivations;
                            if (enabledInTick > Find.TickManager.TicksGame)
                            {
                                label += " (" + "ART.WillBeActiveIn".Translate((enabledInTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()) + ")";
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
                string baseText = base.TipStringExtra;
                if (def.isResource)
                {
                    var allCapacityAdjusters = "ART.NativeCapacity".Translate(def.maxResourceCapacity);
                    Utils.GetHediffResourceCapacityGainFor(pawn, def, out var sbExplanation);
                    string explanation = sbExplanation.ToString().TrimEndNewlines();
                    if (!explanation.NullOrEmpty())
                    {
                        allCapacityAdjusters += "\n" + explanation;
                    }
                    GetHediffResourceCapacityGainFromAmplifiers(out sbExplanation);
                    explanation = sbExplanation.ToString().TrimEndNewlines();
                    if (!explanation.NullOrEmpty())
                    {
                        allCapacityAdjusters += "\n" + explanation;
                    }
                    return baseText + allCapacityAdjusters;
                }
                return baseText;
            }
        }

        public override bool ShouldRemove
        {
            get
            {
                if (def.lifetimeTicks != -1 && duration > def.lifetimeTicks)
                {
                    return true;
                }
                if (SourceOnlyAmplifiers())
                {
                    return true;
                }
                if (def.keepWhenEmpty)
                {
                    if (comps != null)
                    {
                        for (int i = 0; i < comps.Count; i++)
                        {
                            if (comps[i].CompShouldRemove)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                bool value = base.ShouldRemove;
                if (value)
                {
                    Log.Message("Removing: " + this + " this.ResourceAmount: " + ResourceAmount + " - this.Severity: " + Severity);
                }
                return value;
            }
        }

        public bool SourceOnlyAmplifiers()
        {
            var amplifiers = Amplifiers;
            if (!def.keepWhenEmpty && amplifiers.Any())
            {
                foreach (var amplifier in amplifiers)
                {
                    var comp = GetCompAmplifierFor(amplifier.Parent);
                    if (comp != null && !comp.InRadiusFor(pawn.Position, def))
                    {
                        var option = comp.GetFirstResourcePropertiesFor(def);
                        if (option.removeOutsideArea)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public float TotalResourceGainAmount()
        {
            float num = 0;

            var comps = Utils.GetAllAdjustResources(pawn);
            foreach (var comp in comps)
            {
                var resourceSettings = comp.ResourceSettings;
                if (resourceSettings != null)
                {
                    foreach (var option in resourceSettings)
                    {
                        if (option.hediff == def)
                        {
                            float num2 = option.GetResourceGain(comp);
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
            if (CurStage is HediffStageResource hediffStageResource)
            {
                if (hediffStageResource.resourceAdjustPerDamages != null)
                {
                    foreach (var value in hediffStageResource.resourceAdjustPerDamages)
                    {
                        if (value.damageDef is null || (dinfo.Def != null && value.damageDef == dinfo.Def))
                        {
                            float resourceToGain = value.GetResourceGain(totalDamageDealt);
                            var hediff = value.hediff != null ? pawn.health.hediffSet.GetFirstHediffOfDef(value.hediff) as HediffResource : this;
                            if (hediff != null)
                            {
                                hediff.ChangeResourceAmount(resourceToGain);
                            }
                        }
                    }
                }
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            PreInit();
            resourceAmount = def.initialResourceAmount;
            UpdateResourceData();
            duration = 0;
            if (def.sendLetterWhenGained && pawn.Faction == Faction.OfPlayerSilentFail)
            {
                Find.LetterStack.ReceiveLetter(def.letterTitleKey.Translate(pawn.Named("PAWN"), def.Named("RESOURCE")),
                    def.letterMessageKey.Translate(pawn.Named("PAWN"), def.Named("RESOURCE")), def.letterType, pawn);
            }
            var hediffStageResource = CurStageResource;
            if (hediffStageResource != null)
            {
                if (hediffStageResource.healingProperties != null && hediffStageResource.healingProperties.healOnApply)
                {
                    DoHeal(hediffStageResource.healingProperties);
                }
                if (hediffStageResource.tendingProperties != null && hediffStageResource.tendingProperties.tendOnApply)
                {
                    DoTend(hediffStageResource.tendingProperties);
                }
            }
            ARTLog.Message("PostAdd");
            OnStageSwitch(hediffStageResource);
            Register();
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            Notify_Removed();
            Deregister();
            var comps = Utils.GetAllAdjustResources(pawn);
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
        public override void Tick()
        {
            base.Tick();
            duration++;
            if (pawn.IsHashIntervalTick(30))
            {
                UpdateResourceData();
                if (ResourceCapacity < 0)
                {
                    Utils.TryDropExcessHediffGears(pawn);
                }
            }

            var hediffStageResource = CurStage as HediffStageResource;
            if (previousStageIndex != CurStageIndex)
            {
                OnStageSwitch(hediffStageResource);
            }

            if (hediffStageResource != null)
            {
                if (hediffStageResource.needAdjustProperties != null && pawn.IsHashIntervalTick(hediffStageResource.needAdjustProperties.tickRate))
                {
                    foreach (var needToAdjust in hediffStageResource.needAdjustProperties.needsToAdjust)
                    {
                        var need = pawn.needs.TryGetNeed(needToAdjust.need);
                        if (need != null)
                        {
                            need.CurLevelPercentage += needToAdjust.adjustValue;
                        }
                    }
                }
                if (hediffStageResource.healingProperties != null && Find.TickManager.TicksGame >= lastHealingEffectTick + hediffStageResource.healingProperties.ticksPerEffect)
                {
                    DoHeal(hediffStageResource.healingProperties);
                }
                if (hediffStageResource.tendingProperties != null && Find.TickManager.TicksGame >= lastTendingEffectTick + hediffStageResource.tendingProperties.ticksPerEffect)
                {
                    DoTend(hediffStageResource.tendingProperties);
                }
                if (hediffStageResource.damageAuraProperties != null && Find.TickManager.TicksGame >= lastDamagingEffectTick + hediffStageResource.damageAuraProperties.ticksPerEffect)
                {
                    DoDamage(hediffStageResource.damageAuraProperties);
                }
            }

            if (stageIndexToActivate != -1 && Find.TickManager.TicksGame >= stageSwitchTickToActivate)
            {
                SwitchToStage(stageIndexToActivate);
            }
        }

        private void OnStageSwitch(HediffStageResource hediffStageResource)
        {
            ARTLog.Message(pawn + " switching stage " + CurStageIndex + " - " + previousStageIndex);
            var previousStage = previousStageIndex > -1 ? def.stages[previousStageIndex] as HediffStageResource : null;
            if (previousStage != null)
            {
                if (previousStage.skillAdjustProperties != null)
                {
                    foreach (var skillAdjust in previousStage.skillAdjustProperties)
                    {
                        RemoveSkillAdjust(previousStageIndex, skillAdjust);
                    }
                }
            }

            previousStageIndex = CurStageIndex;
            if (hediffStageResource != null && hediffStageResource.skillAdjustProperties != null)
            {
                foreach (var skillAdjust in hediffStageResource.skillAdjustProperties)
                {
                    AddSkillAdjust(CurStageIndex, skillAdjust);
                }
            }

            if (compAbilities != null)
            {
                if (hediffStageResource != null)
                {
                    if (def.randomAbilitiesPool != null)
                    {
                        int amount = hediffStageResource.randomAbilitiesAmountToGain.RandomInRange;
                        var abilityCandidates = def.randomAbilitiesPool.Where(x => !compAbilities.HasAbility(x)).Take(amount);
                        if (!def.retainRandomLearnedAbilities)
                        {
                            var abilitiesToRemove = compAbilities.LearnedAbilities.Where(x => grantedRandomAbilities.Contains(x.def));
                            foreach (var ability in abilitiesToRemove)
                            {
                                ARTLog.Message(pawn + " - removing random ability: " + ability);
                                compAbilities.LearnedAbilities.Remove(ability);
                            }
                        }

                        foreach (var ability in abilityCandidates)
                        {
                            ARTLog.Message(pawn + " - gaining random ability: " + ability);
                            compAbilities.GiveAbility(ability);
                            grantedRandomAbilities.Add(ability);
                        }
                    }
                    else
                    {
                        var abilitiesToRemove = compAbilities.LearnedAbilities.Where(x => grantedRandomAbilities.Contains(x.def));
                        foreach (var ability in abilitiesToRemove)
                        {
                            ARTLog.Message(pawn + " - removing random ability: " + ability);
                            compAbilities.LearnedAbilities.Remove(ability);
                        }
                    }

                    if (hediffStageResource.staticAbilitiesToGain != null)
                    {
                        var abilitiesToRemove = compAbilities.LearnedAbilities.Where(x => grantedStaticAbilities.Contains(x.def)
                            && !hediffStageResource.staticAbilitiesToGain.Contains(x.def));
                        foreach (var ability in abilitiesToRemove)
                        {
                            ARTLog.Message(pawn + " - removing static ability: " + ability);
                            compAbilities.LearnedAbilities.Remove(ability);
                        }

                        var abilityCandidates = hediffStageResource.staticAbilitiesToGain.Where(x => !compAbilities.HasAbility(x));
                        foreach (var ability in abilityCandidates)
                        {
                            ARTLog.Message(pawn + " - gaining static ability: " + ability);
                            compAbilities.GiveAbility(ability);
                            grantedStaticAbilities.Add(ability);
                        }
                    }
                    else
                    {
                        var abilitiesToRemove = compAbilities.LearnedAbilities.Where(x => grantedStaticAbilities.Contains(x.def));
                        foreach (var ability in abilitiesToRemove)
                        {
                            ARTLog.Message(pawn + " - removing static ability: " + ability);
                            compAbilities.LearnedAbilities.Remove(ability);
                        }
                    }
                }
                else
                {
                    var abilitiesToRemove = compAbilities.LearnedAbilities.Where(x => grantedRandomAbilities.Contains(x.def));
                    foreach (var ability in abilitiesToRemove)
                    {
                        ARTLog.Message(pawn + " - removing random ability: " + ability);
                        compAbilities.LearnedAbilities.Remove(ability);
                    }
                    abilitiesToRemove = compAbilities.LearnedAbilities.Where(x => grantedStaticAbilities.Contains(x.def));
                    foreach (var ability in abilitiesToRemove)
                    {
                        ARTLog.Message(pawn + " - removing static ability: " + ability);
                        compAbilities.LearnedAbilities.Remove(ability);
                    }

                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (def.stages != null)
            {
                var toggleableStages = def.stages.OfType<HediffStageResource>().Where(x => x.togglingProperties != null);
                if (toggleableStages.Any())
                {
                    yield return new Command_SwitchHediffStageResource(this)
                    {
                        defaultLabel = def.label + " - " + CurStage.label,
                        icon = GetIcon(),
                        action = delegate
                        {
                            var options = new List<FloatMenuOption>();
                            var otherStages = toggleableStages.Where(x => x != CurStage).ToList();
                            foreach (var otherStage in otherStages)
                            {
                                options.Add(new FloatMenuOption(def.label + " - " + otherStage.label, delegate
                                {
                                    lastStageSwitchTick = Find.TickManager.TicksGame;
                                    stageSwitchTickToActivate = Find.TickManager.TicksGame + otherStage.togglingProperties.changeTime;
                                    curChangeTime = otherStage.togglingProperties.changeTime;
                                    curCooldownPeriod = otherStage.togglingProperties.cooldownTime;
                                    stageIndexToActivate = def.stages.IndexOf(otherStage);
                                }));
                            }
                            Find.WindowStack.Add(new FloatMenu(options));
                        },
                        disabled = !IsActive()
                    };
                    Texture2D GetIcon()
                    {
                        if (CurStage is HediffStageResource stageResource && stageResource.togglingProperties.graphicData != null)
                        {
                            return ContentFinder<Texture2D>.Get(stageResource.togglingProperties.graphicData.texPath);
                        }
                        return ContentFinder<Texture2D>.Get(def.fallbackTogglingGraphicData.texPath);
                    }

                    bool IsActive()
                    {
                        if (lastStageActivatedTick > 0)
                        {
                            int cooldownTicksRemaining = Find.TickManager.TicksGame - lastStageActivatedTick;
                            if (cooldownTicksRemaining < curCooldownPeriod)
                            {
                                return false;
                            }
                        }
                        if (lastStageSwitchTick > 0)
                        {
                            int cooldownTicksRemaining = Find.TickManager.TicksGame - lastStageSwitchTick;
                            if (cooldownTicksRemaining < curChangeTime)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
        }

        private void SwitchToStage(int stageIndex)
        {
            var stage = def.stages[stageIndex] as HediffStageResource;
            if (def.useAbsoluteSeverity)
            {
                SetResourceAmount(ResourceCapacity * stage.minSeverity);
                Severity = ResourceAmount;
            }
            else
            {
                SetResourceAmount(stage.minSeverity);
                Severity = ResourceAmount;
            }
            if (stage.togglingProperties.soundOnToggle != null)
            {
                stage.togglingProperties.soundOnToggle.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }
            lastStageActivatedTick = Find.TickManager.TicksGame;
            stageIndexToActivate = -1;
        }
        private void DoDamage(DamageAuraProperties damagingProperties)
        {
            lastDamagingEffectTick = Find.TickManager.TicksGame;
            foreach (var victim in Utils.GetPawnsAround(pawn, damagingProperties))
            {
                if (CanDamage(victim, damagingProperties))
                {
                    var damageDef = damagingProperties.damageFromEquippedWeapon
                        ? pawn.equipment.PrimaryEq.PrimaryVerb.GetDamageDef()
                        : damagingProperties.damageDef;
                    var damageInfo = new DamageInfo(damageDef, damagingProperties.damageAmount, 1f, instigator: pawn,
                        weapon: damagingProperties.damageFromEquippedWeapon ? pawn.equipment.Primary.def : pawn.def);
                    victim.TakeDamage(damageInfo);
                    if (victim.MapHeld != null)
                    {
                        if (damagingProperties.selfDamageMote != null && pawn == victim)
                        {
                            MoteMaker.MakeStaticMote(pawn.Position, pawn.Map, damagingProperties.selfDamageMote);
                        }
                        else if (damagingProperties.otherDamageMote != null && pawn != victim)
                        {
                            MoteMaker.MakeStaticMote(victim.PositionHeld, victim.MapHeld, damagingProperties.otherDamageMote);
                        }

                        if (damagingProperties.soundOnEffect != null)
                        {
                            damagingProperties.soundOnEffect.PlayOneShot(new TargetInfo(victim.PositionHeld, victim.MapHeld));
                        }
                    }
                }
            }
        }

        public bool CanApplyOn(GeneralProperties properties, Thing thing)
        {
            if (thing.def.tradeTags.NullOrEmpty() is false)
            {
                if (properties.blacklistTradeTags.NullOrEmpty() is false && properties.blacklistTradeTags.Any(x => thing.def.tradeTags.Contains(x)))
                {
                    return false;
                }
                if (properties.whitelistTradeTags.NullOrEmpty() is false && !properties.whitelistTradeTags.Any(x => thing.def.tradeTags.Contains(x)))
                {
                    return false;
                }
            }
            if (!properties.affectsSelf && pawn == thing)
            {
                return false;
            }
            if (!properties.affectsSelf && !properties.worksThroughWalls && !GenSight.LineOfSight(pawn.Position, thing.Position, pawn.Map))
            {
                return false;
            }
            return true;
        }

        private bool CanDamage(Pawn pawn, DamageAuraProperties damagingProperties)
        {
            if (!CanApplyOn(damagingProperties, pawn))
            {
                return false;
            }
            if (!damagingProperties.affectsSelf)
            {
                bool isAllyOrColonist = pawn.Faction != null && !pawn.HostileTo(this.pawn);
                if (!damagingProperties.affectsAllies && isAllyOrColonist)
                {
                    return false;
                }
                if (!damagingProperties.affectsEnemies && isAllyOrColonist == false)
                {
                    return false;
                }
            }
            return true;
        }
        private void AddSkillAdjust(int stageIndex, SkillAdjustProperties skillAdjust)
        {
            if (pawn.skills != null)
            {
                var skillRecord = pawn.skills.GetSkill(skillAdjust.skill);
                if (skillRecord != null && !skillRecord.TotallyDisabled)
                {
                    if (!savedSkillRecordsByStages.TryGetValue(stageIndex, out var list))
                    {
                        savedSkillRecordsByStages[stageIndex] = list = new SavedSkillRecordCollection();
                    }
                    list.savedSkillRecords.Add(new SavedSkillRecord
                    {
                        def = skillAdjust.skill,
                        levelInt = skillRecord.levelInt,
                        passion = skillRecord.passion
                    });
                    skillRecord.levelInt += skillAdjust.skillLevelOffset;
                    skillRecord.passion = skillAdjust.forcedPassion;
                    if (skillRecord.levelInt > skillAdjust.maxSkillLevel)
                    {
                        skillRecord.levelInt = skillAdjust.maxSkillLevel;
                    }
                    skillRecord.levelInt = Mathf.Clamp(skillRecord.levelInt, 0, 20);
                }
            }
        }

        private void RemoveSkillAdjust(int stageIndex, SkillAdjustProperties skillAdjust)
        {
            var skillRecord = pawn.skills.GetSkill(skillAdjust.skill);
            if (skillRecord != null && !skillRecord.TotallyDisabled)
            {
                if (savedSkillRecordsByStages.ContainsKey(stageIndex))
                {
                    var savedSkillRecord = savedSkillRecordsByStages[stageIndex].savedSkillRecords.FirstOrDefault(x => x.def == skillAdjust.skill);
                    if (savedSkillRecord != null)
                    {
                        savedSkillRecordsByStages[stageIndex].savedSkillRecords.Remove(savedSkillRecord);
                        skillRecord.levelInt = Mathf.Max(skillRecord.levelInt - skillAdjust.skillLevelOffset,
                            savedSkillRecord.levelInt);
                        skillRecord.levelInt = Mathf.Clamp(skillRecord.levelInt, 0, 20);
                        skillRecord.passion = savedSkillRecord.passion;
                    }
                }
            }
        }

        public void DoTend(TendingProperties tendingProperties)
        {
            lastTendingEffectTick = Find.TickManager.TicksGame;
            var hediffs = tendingProperties.affectConditions
                ? pawn.health.hediffSet.hediffs.Where(x => x.TendableNow()).ToList()
                : pawn.health.hediffSet.hediffs.Where(x => x is Hediff_Injury && x.TendableNow()).ToList();
            if (hediffs.Any())
            {
                hediffs = hediffs.Take(tendingProperties.tendCount).ToList();
                TendUtility_DoTend_Patch.hardCodedTendData = new TendUtility_DoTend_Patch.HardCodedTendData
                {
                    quality = tendingProperties.tendQuality.RandomInRange,
                    maxQuality = tendingProperties.tendQuality.max
                };
                foreach (var hediff in hediffs)
                {
                    TendUtility.DoTend(null, pawn, null);
                }
                TendUtility_DoTend_Patch.hardCodedTendData = null;
            }
        }

        public void DoHeal(HealingProperties healingProperties)
        {
            lastHealingEffectTick = Find.TickManager.TicksGame;
            float totalSpentPoints = healingProperties.healPoints;
            foreach (var pawn in Utils.GetPawnsAround(pawn, healingProperties))
            {
                if (CanHeal(pawn, healingProperties))
                {
                    var hediffs = GetHediffsToHeal(pawn, healingProperties).ToList();
                    if (hediffs.Any())
                    {
                        var hediffsToHeal = healingProperties.hediffsToHeal > 0 ? hediffs.Take(healingProperties.hediffsToHeal).ToList() : hediffs;
                        Utils.HealHediffs(this.pawn, ref totalSpentPoints, hediffsToHeal, healingProperties.pointsOverflow,
                            healingProperties.healPriority, healingProperties.hediffsToHeal > 0, healingProperties.soundOnEffect);
                    }
                }
            }
        }

        private bool CanHeal(Pawn pawn, HealingProperties healingProperties)
        {
            if (!CanApplyOn(healingProperties, pawn))
            {
                return false;
            }
            if (pawn.health?.hediffSet?.hediffs is null)
            {
                return false;
            }
            bool isFlesh = pawn.RaceProps.IsFlesh;
            if (!healingProperties.affectMechanical && isFlesh == false)
            {
                return false;
            }
            if (!healingProperties.affectOrganic && isFlesh)
            {
                return false;
            }
            if (!healingProperties.affectsSelf)
            {
                bool isAllyOrColonist = pawn.Faction != null && !pawn.HostileTo(this.pawn);
                if (!healingProperties.affectsAllies && isAllyOrColonist)
                {
                    return false;
                }
                if (!healingProperties.affectsEnemies && isAllyOrColonist == false)
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerable<Hediff> GetHediffsToHeal(Pawn pawn, HealingProperties healingProperties)
        {
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (healingProperties.affectIllness && !(hediff is Hediff_Injury) && !(hediff is Hediff_MissingPart)
                    && ((hediff.def.PossibleToDevelopImmunityNaturally() && !hediff.FullyImmune()) || (hediff.def.makesSickThought && hediff.def.tendable)))
                {
                    yield return hediff;
                }
                else if (healingProperties.affectInjuries && hediff is Hediff_Injury)
                {
                    yield return hediff;
                }
                else if (healingProperties.affectPermanent && hediff.IsPermanent())
                {
                    yield return hediff;
                }
                else if (healingProperties.affectChronic && hediff.def.chronic)
                {
                    yield return hediff;
                }
            }
        }

        private Vector3 impactAngleVect;

        private int lastAbsorbDamageTick = -9999;
        public void AbsorbedDamage(DamageInfo dinfo)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            var loc = base.pawn.TrueCenter() + (impactAngleVect.RotatedBy(180f) * 0.5f);
            float num = Mathf.Min(10f, 2f + (dinfo.Amount / 10f));
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
                    var stage = CurStageResource;
                    bubbleMat = MaterialPool.MatFrom(stage.shieldProperties.texPath, ShaderDatabase.Transparent, stage.shieldProperties.shieldColor);
                }
                return bubbleMat;
            }
        }
        public Dictionary<HediffResource, HediffResouceDisable> PostUseDelayTicks => null;
        public Thing Parent => pawn;
        public Pawn PawnHost => pawn;
        public List<ResourceProperties> ResourceSettings => CurStageResource?.resourceSettings ?? new List<ResourceProperties>();
        public string DisablePostUse => "";

        private static Dictionary<GraphicData, Material> auraGraphics = new Dictionary<GraphicData, Material>();
        public static Material GetAuraMaterial(GraphicData graphicData)
        {
            if (!auraGraphics.TryGetValue(graphicData, out var material))
            {
                auraGraphics[graphicData] = material = MaterialPool.MatFrom(graphicData.texPath, graphicData.shaderType?.Shader ?? ShaderDatabase.Mote, graphicData.color);
            }
            return material;
        }
        public virtual void Draw()
        {
            if (CurStage is HediffStageResource hediffStageResource)
            {
                if (hediffStageResource.ShieldIsActive(pawn) && ResourceAmount > 0)
                {
                    float num = Mathf.Lerp(1.2f, 1.55f, def.lifetimeTicks != -1 ? (def.lifetimeTicks - duration) / def.lifetimeTicks : 1);
                    var drawPos = base.pawn.Drawer.DrawPos;
                    drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                    int num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
                    if (num2 < 8)
                    {
                        float num3 = (8 - num2) / 8f * 0.05f;
                        drawPos += impactAngleVect * num3;
                        num -= num3;
                    }
                    float angle = Rand.Range(0, 360);
                    var s = new Vector3(num, 1f, num);
                    var matrix = default(Matrix4x4);
                    matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat, 0);
                }
                if (hediffStageResource.damageAuraProperties?.auraGraphic != null)
                {
                    var drawPos = base.pawn.Drawer.DrawPos;
                    drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                    var s = new Vector3(hediffStageResource.damageAuraProperties.auraGraphic.drawSize.x, 1f, hediffStageResource.damageAuraProperties.auraGraphic.drawSize.y);
                    var matrix = default(Matrix4x4);
                    matrix.SetTRS(drawPos, Quaternion.identity, s);
                    var auraMaterial = GetAuraMaterial(hediffStageResource.damageAuraProperties.auraGraphic);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, auraMaterial, 0);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref resourceAmount, "resourceAmount");
            Scribe_Values.Look(ref duration, "duration");
            Scribe_Values.Look(ref delayTicks, "delayTicks");
            Scribe_Values.Look(ref lastHealingEffectTick, "lastHealingEffectTick");
            Scribe_Values.Look(ref lastTendingEffectTick, "lastTendingEffectTick");
            Scribe_Values.Look(ref lastDamagingEffectTick, "lastDamagingEffectTick");

            Scribe_Values.Look(ref stageIndexToActivate, "stageIndexToActivate", -1);
            Scribe_Values.Look(ref lastStageActivatedTick, "lastStageActivatedTick");
            Scribe_Values.Look(ref stageSwitchTickToActivate, "stageSwitchTickToActivate");
            Scribe_Values.Look(ref curCooldownPeriod, "curCooldownPeriod");
            Scribe_Values.Look(ref curChangeTime, "curChangeTime");
            Scribe_Values.Look(ref lastStageSwitchTick, "lastStageSwitchTick");

            Scribe_Values.Look(ref previousStageIndex, "previousStageIndex", -1);
            Scribe_Collections.Look(ref amplifiers, "amplifiers", LookMode.Reference);
            Scribe_Collections.Look(ref savedSkillRecordsByStages, "savedSkillRecordsByStages", LookMode.Value, LookMode.Deep);

            Scribe_Collections.Look(ref grantedStaticAbilities, "grantedStaticAbilities", LookMode.Def);
            Scribe_Collections.Look(ref grantedRandomAbilities, "grantedRandomAbilities", LookMode.Def);
            PreInit();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                foreach (var amplifier in amplifiers)
                {
                    cachedAmplifiers[amplifier] = amplifier.TryGetComp<CompAdjustHediffsArea>();
                }
            }
        }

        public void Register()
        {
            ARTManager.Instance.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            ARTManager.Instance.DeregisterAdjuster(this);
        }
        public bool TryGetQuality(out QualityCategory qc)
        {
            qc = QualityCategory.Normal;
            return false;
        }

        public void Drop()
        {
            PawnHost.health.RemoveHediff(this);
            Notify_Removed();
        }
        public void Notify_Removed()
        {
            Deregister();
            if (PawnHost != null)
            {
                Utils.RemoveExcessHediffResources(PawnHost, this);
            }
        }
        public void ResourceTick()
        {
            var pawn = PawnHost;
            if (pawn != null)
            {
                foreach (var resourceProperties in ResourceSettings)
                {
                    resourceProperties.AdjustResource(pawn, this, PostUseDelayTicks);
                }
            }
        }

        public void Update()
        {
        }

        public ThingDef GetStuff()
        {
            return null;
        }

        public bool IsStorageFor(ResourceProperties resourceProperties, out ResourceStorage resourceStorages)
        {
            resourceStorages = null;
            return false;
        }
    }
}
