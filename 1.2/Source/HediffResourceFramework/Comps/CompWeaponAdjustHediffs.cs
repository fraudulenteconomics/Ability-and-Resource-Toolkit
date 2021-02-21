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
        public Pawn Pawn
        {
            get
            {
                if (CompEquippable.ParentHolder is Pawn_EquipmentTracker equipmentTracker && equipmentTracker.pawn != null)
                {
                    return equipmentTracker.pawn;
                }
                return null;
            }
        }
        public override void Notify_Removed()
        {
            base.Notify_Removed();
            if (Pawn != null)
            {
                HediffResourceUtils.RemoveExcessHediffResources(Pawn, this);
            }
        }

        public override void Drop()
        {
            base.Drop();
            var pawn = Pawn;
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
            var pawn = Pawn;
            if (pawn != null)
            {
                foreach (var option in Props.resourceSettings)
                {
                    var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                    if (hediffResource != null && (PostUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame)
                        || !hediffResource.CanGainResource))
                    {
                        continue;
                    }
                    else
                    {
                        float num = option.resourcePerSecond;
                        if (option.qualityScalesResourcePerSecond && this.parent.TryGetQuality(out QualityCategory qc))
                        {
                            num *= HediffResourceUtils.GetQualityMultiplier(qc);
                        }
                        HediffResourceUtils.AdjustResourceAmount(pawn, option.hediff, num, option.addHediffIfMissing);
                    }
                }
            }
        }
    }
}
