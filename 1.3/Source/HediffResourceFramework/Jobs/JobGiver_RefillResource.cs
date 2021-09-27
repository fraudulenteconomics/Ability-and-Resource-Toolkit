﻿using RimWorld;
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
            if (pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer)
            {
                if (HediffResourceUtils.HediffResourceManager.hediffResourcesPolicies.ContainsKey(pawn))
                {
                    if (pawn.health?.hediffSet.hediffs.OfType<HediffResource>().Any() ?? false)
                    {
                        return 8f;
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