using RimWorld;
using Verse;

namespace ART
{
    public class CompProperties_ApparelAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_ApparelAdjustHediffs()
        {
            compClass = typeof(CompApparelAdjustHediffs);
        }
    }

    public class CompApparelAdjustHediffs : CompAdjustHediffs
    {
        public Apparel Apparel => parent as Apparel;
        public override Pawn PawnHost => Apparel.Wearer;
        public override void Notify_Removed()
        {
            if (PawnHost != null)
            {
                Utils.RemoveExcessHediffResources(PawnHost, this);
            }
        }

        public override void Drop()
        {
            base.Drop();
            var pawn = Apparel.Wearer;
            if (pawn != null)
            {
                if (pawn.Map != null)
                {
                    pawn.apparel.TryDrop(Apparel);
                }
                else
                {
                    pawn.inventory.TryAddItemNotForSale(Apparel);
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
