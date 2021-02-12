using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_WeaponAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_WeaponAdjustHediffs()
        {
            this.compClass = typeof(CompWeaponAdjustHediffs);
        }
    }
    public class CompWeaponAdjustHediffs : CompAdjustHediffs
    {

        private CompEquippable compEquippable;
        private CompEquippable CompEquippable
        {
            get
            {
                if (compEquippable is null)
                {
                    compEquippable = this.parent.GetComp<CompEquippable>();
                }
                return compEquippable;
            }
        }

        public CompEquippable Equipment => this.parent.GetComp<CompEquippable>();
        public override void Notify_Removed()
        {
            base.Notify_Removed();
            if (Equipment?.PrimaryVerb?.CasterPawn != null)
            {
                HediffResourceUtils.RemoveExcessHediffResources(Equipment?.PrimaryVerb?.CasterPawn, this);
            }
        }

        public override void Drop()
        {
            base.Drop();
            var pawn = Equipment?.PrimaryVerb?.CasterPawn;
            if (pawn != null)
            {
                if (pawn.Map != null)
                {
                    pawn.equipment.TryDropEquipment(this.parent, out ThingWithComps result, pawn.Position);
                }
                else
                {
                    pawn.inventory.TryAddItemNotForSale(this.parent);
                }
            }
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            this.Notify_Removed();
            base.PostDestroy(mode, previousMap);
        }
        public override void ResourceTick()
        {
            base.ResourceTick();
            if (this.parent is ThingWithComps equipment && CompEquippable.PrimaryVerb.CasterPawn != null)
            {
                if (CompEquippable.PrimaryVerb.CasterPawn.IsHashIntervalTick(60))
                {
                    if (!this.PostUseDelayTicks?.Values?.Select(x => x.delayTicks).Any(y => y > Find.TickManager.TicksGame) ?? true)
                    {
                        foreach (var option in Props.resourceSettings)
                        {
                            var hediffResource = CompEquippable.PrimaryVerb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                            if (hediffResource != null && !hediffResource.CanGainResource)
                            {
                                continue;
                            }
                            else 
                            {
                                float num = option.resourcePerSecond;
                                if (option.qualityScalesResourcePerSecond && equipment.TryGetQuality(out QualityCategory qc))
                                {
                                    num *= HediffResourceUtils.GetQualityMultiplier(qc);
                                }
                                HediffResourceUtils.AdjustResourceAmount(CompEquippable.PrimaryVerb.CasterPawn, option.hediff, num, option.addHediffIfMissing);
                            }
                        }
                    }
                }
            }
        }
    }
}
