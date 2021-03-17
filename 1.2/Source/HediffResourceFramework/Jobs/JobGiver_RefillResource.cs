using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
    public class JobGiver_RefillResource : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike)
            {
                if (HediffResourceUtils.HediffResourceManager.hediffResourcesPolicies.ContainsKey(pawn))
                {
                    Log.Message("JobGiver_RefillResource : ThinkNode_JobGiver - GetPriority - if (pawn.health?.hediffSet.hediffs.OfType<HediffResource>().Any() ?? false) - 2", true);
                    if (pawn.health?.hediffSet.hediffs.OfType<HediffResource>().Any() ?? false)
                    {
                        Log.Message("JobGiver_RefillResource : ThinkNode_JobGiver - GetPriority - return 8f; - 3", true);
                        return 8f;
                    }
                }
                else
                {
                    foreach (var data in HediffResourceUtils.HediffResourceManager.hediffResourcesPolicies)
                    {
                        Log.Message(pawn + " - " + data.Key + " - " + data.Value);
                    }
                }
            }

            return 0f;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (HediffResourceUtils.HediffResourceManager.hediffResourcesPolicies.TryGetValue(pawn, out var policy))
            {
                foreach (var hediffResource in pawn.health?.hediffSet.hediffs.OfType<HediffResource>())
                {
                    var satisfyPolicy = policy.satisfyPolicies[hediffResource.def];
                    if (satisfyPolicy.seekingIsEnabled && (hediffResource.ResourceAmount / hediffResource.ResourceCapacity) < satisfyPolicy.resourceSeekingThreshold.max)
                    {
                        var ingestibles = IngestiblesFor(pawn, hediffResource);
                        var ingestible = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, ingestibles, PathEndMode.OnCell, TraverseParms.For(pawn));
                        if (ingestible != null)
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.Ingest, ingestible);
                            job.count = 1;
                            return job;
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