using RimWorld;
using Verse;

namespace HediffResourceFramework
{
    public class CompPlantInUse : CompThingInUse
    {
        public static bool PawnCanHarvestIt(Pawn pawn, Plant plant, UseProps useProps, out string failMessage)
        {
            failMessage = null;
            if (useProps.resourceOnHarvest < 0 && !pawn.HasResource(useProps.hediff, -useProps.resourceOnHarvest))
            {
                if (!useProps.cannotHarvestMessageKey.NullOrEmpty())
                {
                    failMessage = useProps.cannotHarvestMessageKey.Translate(pawn.Named("PAWN"), plant.Label, "HRF.NotEnoughResource".Translate());
                }
                return false;
            }
            return true;
        }

        public static bool PawnCanSowIt(Pawn pawn, ThingDef plant, UseProps useProps, out string failMessage)
        {
            failMessage = null;
            if (useProps.resourceOnSow < 0 && !pawn.HasResource(useProps.hediff, -useProps.resourceOnSow))
            {
                if (!useProps.cannotSowMessageKey.NullOrEmpty())
                {
                    failMessage = useProps.cannotSowMessageKey.Translate(pawn.Named("PAWN"), plant.label, "HRF.NotEnoughResource".Translate());
                }
                return false;
            }
            return true;
        }
    }
}