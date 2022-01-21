using Verse;

namespace HediffResourceFramework
{
    public static class ReservationHelper
    {
        public static bool CanUseIt(this Pawn pawn, Thing thing)
        {
            if (CompFacilityInUse.thingBoosters.TryGetValue(thing, out var comp))
            {
                foreach (var statBooster in comp.Props.statBoosters)
                {
                    if (comp.StatBoosterIsEnabled(statBooster) && statBooster.preventUseIfHediffMissing)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(statBooster.hediff) as HediffResource;
                        if (hediffResource is null || !hediffResource.CanApplyStatBooster(statBooster))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static bool CanUseIt(this Pawn pawn, Thing t, out string cannotUseMessage)
        {
            if (CompFacilityInUse.thingBoosters.TryGetValue(t, out var comp))
            {
                foreach (var statBooster in comp.Props.statBoosters)
                {
                    if (comp.StatBoosterIsEnabled(statBooster) && statBooster.preventUseIfHediffMissing)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(statBooster.hediff) as HediffResource;
                        if (hediffResource is null || !hediffResource.CanApplyStatBooster(statBooster))
                        {
                            cannotUseMessage = statBooster.cannotUseMessageKey.Translate(t);
                            return false;
                        }
                    }
                }
            }
            cannotUseMessage = null;
            return true;
        }
    }
}
