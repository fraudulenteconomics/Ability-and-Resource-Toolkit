﻿using System.Linq;
using RimWorld;
using Verse;

namespace FraudeconCode
{
    internal class Verb_FallenServant : BaseVerb
    {
        public VerbProps Props => verbProps as VerbProps;

        private Corpse GetCorpse(LocalTargetInfo target)
        {
            return target.Cell.GetThingList(caster.Map).OfType<Corpse>().FirstOrDefault(c =>
                Props.requireRotStage == null || c.GetRotStage() == Props.requireRotStage);
        }

        protected override bool TryCastShot()
        {
            var corpse = GetCorpse(currentTarget);
            if (corpse == null) return false;
            var pawn = PawnGenerator.GeneratePawn(Props.servantDef, caster.Faction);
            pawn.drafter = pawn.drafter ?? new Pawn_DraftController(pawn);
            GenSpawn.Spawn(pawn, currentTarget.Cell, caster.Map);
            var tracker =
                (RemovalTracker) GenSpawn.Spawn(ThingDef.Named("RemovalTracker"), caster.Position, caster.Map);
            tracker.ToTrack = pawn;
            tracker.RemoveTick = Find.TickManager.TicksGame + Props.servantDuration;
            corpse.Destroy();
            return true;
        }

        public override bool ValidateTarget(LocalTargetInfo target)
        {
            return base.ValidateTarget(target) && GetCorpse(target) != null;
        }

        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            return base.CanHitTarget(targ) && GetCorpse(targ) != null;
        }
    }
}