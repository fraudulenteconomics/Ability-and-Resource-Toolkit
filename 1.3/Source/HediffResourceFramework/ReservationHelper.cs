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
                        if (hediffResource is null || !hediffResource.CanApplyStatBooster(statBooster, out _))
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
                        if (hediffResource is null)
                        {
                            var noResourceReason = "HRF.NoResource".Translate(pawn.Named("PAWN"), statBooster.hediff.label);
                            cannotUseMessage = statBooster.cannotUseMessageKey.Translate(pawn.Named("PAWN"), t.Label, noResourceReason);
                            return false;
                        }
                        else if (!hediffResource.CanApplyStatBooster(statBooster, out string failReason))
                        {
                            cannotUseMessage = statBooster.cannotUseMessageKey.Translate(pawn.Named("PAWN"), t.Label, failReason);
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
