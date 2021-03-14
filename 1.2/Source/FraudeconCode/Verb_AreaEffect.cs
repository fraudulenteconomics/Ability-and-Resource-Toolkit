using System.Linq;
using Verse;

namespace FraudeconCode
{
    public abstract class Verb_AreaEffect : BaseVerb
    {
        protected override bool TryCastShot()
        {
            foreach (var cell in GenRadial.RadialCellsAround(currentTarget.Cell, Props.effectRadius, true)
                .Where(cell => cell.InBounds(caster.Map)))
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