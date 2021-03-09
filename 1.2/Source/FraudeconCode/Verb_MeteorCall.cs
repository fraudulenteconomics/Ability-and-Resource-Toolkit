using System.Linq;
using RimWorld;
using UnityEngine;
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
            // if (verbProps.forcedMissRadius > 0.5f)
            // {
            //     var num = VerbUtility.CalculateAdjustedForcedMiss(verbProps.forcedMissRadius,
            //         currentTarget.Cell - caster.Position);
            //     if (num > 0.5f)
            //     {
            //         var num2 = Rand.Range(0, GenRadial.NumCellsInRadius(num));
            //         if (num2 > 0) cell += GenRadial.RadialPattern[num2];
            //     }
            // }

            var things = Enumerable.Repeat(Props.meteorMaterial.RandomElement(),
                GenRadial.NumCellsInRadius(Props.meteorSize)).Select(def => ThingMaker.MakeThing(def));
            var meteor = (MeteorIncoming) SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("MeteorIncoming"), things, cell,
                caster.Map);
            meteor.Props = Props;
            return true;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return Props.meteorSize;
        }
    }

    public class MeteorIncoming : Skyfaller
    {
        public VerbProps Props;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref Props, "props");
        }

        protected override void SpawnThings()
        {
            if (!Props.allowCrushingRocks || !Props.spawnRocks)
                foreach (var pawn in GenRadial.RadialDistinctThingsAround(Position, Map, Props.meteorSize, true)
                    .OfType<Pawn>())
                {
                    var theta = Position.ToVector3().AngleToFlat(pawn.Position.ToVector3()) * Mathf.Deg2Rad;
                    var radius = Mathf.CeilToInt(Props.meteorSize + 1);
                    pawn.Position = Position + IntVec3.FromVector3((Vector3.up * radius).RotatedBy(theta));
                    pawn.stances.stunner.StunFor_NewTmp(60, this);
                    pawn.Notify_Teleported(false, false);
                }

            if (Props.spawnRocks) base.SpawnThings();
        }
    }
}