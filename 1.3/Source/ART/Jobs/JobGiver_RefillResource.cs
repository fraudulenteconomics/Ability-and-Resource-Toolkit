using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ART
{
    public class JobGiver_RefillResource : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike)
            {
                if (pawn.health?.hediffSet.hediffs.OfType<HediffResource>().Any() ?? false)
                {
                    return 8f;
                }
            }
            return 0f;
        }
        public override Job TryGiveJob(Pawn pawn)
        {
            if (ARTManager.Instance.hediffResourcesPolicies.TryGetValue(pawn, out var policy))
            {
                foreach (var hediffResource in pawn.health?.hediffSet.hediffs.OfType<HediffResource>())
                {
                    if (policy.satisfyPolicies.TryGetValue(hediffResource.def, out var satisfyPolicy))
                    {
                        if (satisfyPolicy.seekingIsEnabled && (hediffResource.ResourceAmount / hediffResource.ResourceCapacity)
                            < satisfyPolicy.resourceSeekingThreshold.max)
                        {
                            var ingestibles = IngestiblesFor(pawn, hediffResource);
                            var ingestible = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, ingestibles, PathEndMode.OnCell, TraverseParms.For(pawn));
                            if (ingestible != null)
                            {
                                var job = JobMaker.MakeJob(JobDefOf.Ingest, ingestible);
                                job.count = 1;
                                return job;
                            }
                        }
                    }
                }
            }

            foreach (var item in pawn.inventory.innerContainer.ToList())
            {
                foreach (var hediffResource in Utils.HediffResourcesRefuelable(pawn, item.def))
                {
                    if (hediffResource.def.refuelHediff.useFromInventory)
                    {
                        float pctFilled = hediffResource.ResourceAmount / hediffResource.ResourceCapacity;
                        if (hediffResource.def.refuelHediff.useType == RefuelUseType.Over && pctFilled > hediffResource.def.refuelHediff.useThreshold)
                        {
                            Utils.RefuelHediff(item, hediffResource);
                        }
                        else if (hediffResource.def.refuelHediff.useType == RefuelUseType.Under && pctFilled < hediffResource.def.refuelHediff.useThreshold)
                        {
                            Utils.RefuelHediff(item, hediffResource);
                        }
                    }
                }
            }
            return null;
        }
        private IEnumerable<Thing> IngestiblesFor(Pawn pawn, HediffResource hediffResource)
        {
            foreach (var thing in pawn.Map.listerThings.AllThings)
            {
                if (thing.def.ingestible?.outcomeDoers != null && thing.def.ingestible.outcomeDoers.Any(y => y is IngestionOutcomeDoer_GiveHediffResource outcomeDoer
                && outcomeDoer.hediffDef == hediffResource.def && (outcomeDoer.resourceAdjust > 0 || outcomeDoer.resourcePercent > 0)))
                {
                    yield return thing;
                }
            }
        }
    }
}