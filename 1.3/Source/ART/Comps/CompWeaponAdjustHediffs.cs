using Verse;

namespace ART
{
    public class CompProperties_WeaponAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_WeaponAdjustHediffs()
        {
            compClass = typeof(CompWeaponAdjustHediffs);
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
                    compEquippable = parent.GetComp<CompEquippable>();
                }
                return compEquippable;
            }
        }
        public override Pawn PawnHost => (CompEquippable.ParentHolder as Pawn_EquipmentTracker)?.pawn;
        public override void Notify_Removed()
        {
            base.Notify_Removed();
            if (PawnHost != null)
            {
                Utils.RemoveExcessHediffResources(PawnHost, this);
            }
        }

        public override void Drop()
        {
            base.Drop();
            var pawn = PawnHost;
            if (pawn != null)
            {
                if (pawn.Map != null)
                {
                    pawn.equipment.TryDropEquipment(parent, out _, pawn.Position);
                }
                else
                {
                    pawn.inventory.TryAddItemNotForSale(parent);
                }
            }
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Notify_Removed();
            base.PostDestroy(mode, previousMap);
        }
        public override void ResourceTick()
        {
            base.ResourceTick();
            var pawn = PawnHost;
            if (pawn != null)
            {
                foreach (var resourceProperties in Props.resourceSettings)
                {
                    resourceProperties.AdjustResource(pawn, this, PostUseDelayTicks);
                }
            }
        }
    }
}
