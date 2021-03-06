using RimWorld;
using UnityEngine;
using Verse;

namespace FraudeconCode
{
    public class PawnMover : PawnFlyer
    {
        protected Vector3 effectivePos;
        private IntVec3 lastPos;
        private int positionLastComputedTick;

        public override Vector3 DrawPos
        {
            get
            {
                RecomputePosition();
                return effectivePos;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (FlyingPawn != null) RecomputePosition();
            var pos = IntVec3.FromVector3(effectivePos);
            if (pos != lastPos)
            {
                AffectCell(pos);
                lastPos = pos;
            }
        }

        public virtual void AffectCell(IntVec3 cell)
        {
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