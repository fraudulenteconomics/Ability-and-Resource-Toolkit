using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FraudeconCode
{
    public class Verb_PullTarget : Verb_CastBase
    {
        protected override bool TryCastShot()
        {
            var pawn = CurrentTarget.Pawn;
            if (pawn == null) return false;
            var cell = caster.RandomAdjacentCell8Way();
            var flyer = PawnFlyer.MakeFlyer(ThingDef.Named("PulledPawn"), pawn, cell);
            if (flyer == null) return false;
            GenSpawn.Spawn(flyer, cell, caster.Map);
            return true;
        }
    }

    public class PulledPawn : PawnFlyer
    {
        private Vector3 effectivePos;
        private int positionLastComputedTick;

        public override Vector3 DrawPos
        {
            get
            {
                RecomputePosition();
                return effectivePos;
            }
        }

        // Token: 0x06005226 RID: 21030 RVA: 0x001BB260 File Offset: 0x001B9460
        private void RecomputePosition()
        {
            if (positionLastComputedTick == ticksFlying) return;

            positionLastComputedTick = ticksFlying;
            effectivePos = Vector3.Lerp(startVec, DestinationPos, ticksFlying / (float) ticksFlightTime);
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            FlyingPawn.DrawAt(drawLoc, flip);
        }
    }
}