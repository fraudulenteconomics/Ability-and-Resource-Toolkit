using Verse;
using Verse.AI;

namespace FraudeconCode
{
    public abstract class Verb_AreaEffect : Verb_CastBase
    {
        public VerbProps Props => verbProps as VerbProps;

        protected override bool TryCastShot()
        {
            foreach (var cell in GenRadial.RadialCellsAround(currentTarget.Cell, Props.effectRadius, true))
                AffectCell(cell);
            return true;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return Props.effectRadius;
        }

        protected abstract void AffectCell(IntVec3 cell);
    }
}