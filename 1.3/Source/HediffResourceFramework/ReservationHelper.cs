using Verse;

namespace HediffResourceFramework
{
    public static class ReservationHelper
    {
        public static bool CanUseIt(this Pawn pawn, Thing thing)
        {
            if (CompThingInUse.things.TryGetValue(thing, out var comp))
            {
                foreach (var useProps in comp.Props.useProperties)
                {
                    if (comp.UseIsEnabled(useProps) && useProps.hediffRequired)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(useProps.hediff) as HediffResource;
                        if (hediffResource is null || !hediffResource.CanUse(useProps, out _))
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
            if (CompThingInUse.things.TryGetValue(t, out var comp))
            {
                foreach (var useProps in comp.Props.useProperties)
                {
                    if (comp.UseIsEnabled(useProps) && useProps.hediffRequired)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(useProps.hediff) as HediffResource;
                        if (hediffResource is null)
                        {
                            var noResourceReason = "HRF.NoResource".Translate(pawn.Named("PAWN"), useProps.hediff.label);
                            cannotUseMessage = useProps.cannotUseMessageKey.Translate(pawn.Named("PAWN"), t.Label, noResourceReason);
                            return false;
                        }
                        else if (!hediffResource.CanUse(useProps, out string failReason))
                        {
                            cannotUseMessage = useProps.cannotUseMessageKey.Translate(pawn.Named("PAWN"), t.Label, failReason);
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
