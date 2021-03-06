using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FraudeconCode
{
    public class Verb_MeteorCall : Verb_CastBase
    {
        public VerbProps Props => verbProps as VerbProps;

        protected override bool TryCastShot()
        {
            var cell = CurrentTarget.Cell;
            var things = Enumerable.Repeat(Props.meteorMaterial.RandomElement(),
                GenRadial.NumCellsInRadius(Props.meteorSize)).Select(def => ThingMaker.MakeThing(def));
            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, things, cell, caster.Map);
            return true;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return Props.meteorSize;
        }
    }
}