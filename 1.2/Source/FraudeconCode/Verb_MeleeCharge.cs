using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FraudeconCode
{
    public class Verb_MeleeCharge : BaseVerb
    {
        protected override bool TryCastShot()
        {
            var cell = CurrentTarget.HasThing ? CurrentTarget.Thing.RandomAdjacentCellCardinal() : CurrentTarget.Cell;
            var map = caster.Map;
            var flyer = (ChargingPawn) PawnFlyer.MakeFlyer(ThingDef.Named("ChargingPawn"), CasterPawn, cell);
            if (flyer == null) return false;
            flyer.Props = verbProps as VerbProps;
            flyer.Params = targetParams;
            flyer.Weapon = EquipmentSource?.def;
            GenSpawn.Spawn(flyer, cell, map);
            return true;
        }
    }

    public class ChargingPawn : PawnMover
    {
        private HashSet<Thing> hitTargets = new HashSet<Thing>();
        public TargetingParameters Params;
        public VerbProps Props;
        public ThingDef Weapon;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref hitTargets, "hitTargets");
            Scribe_Deep.Look(ref Props, "props");
            Scribe_Deep.Look(ref Params, "params");
            Scribe_Defs.Look(ref Weapon, "weapon");
        }

        public override void AffectCell(IntVec3 cell)
        {
            base.AffectCell(cell);
            Log.Message("Affecting cell: " + cell);
            foreach (var thing in GenRadial.RadialDistinctThingsAround(cell, Map, Props.chargeWidth, true)
                .Except(hitTargets).Where(t => Params.CanTarget(new TargetInfo(t))))
            {
                var dinfo = new DamageInfo(Props.chargeDamageDef, Props.chargeDamageAmount,
                    Props.chargeDamageDef.defaultArmorPenetration, effectivePos.AngleToFlat(thing.DrawPos), FlyingPawn,
                    null, Weapon);
                var log = new BattleLogEntry_RangedImpact(FlyingPawn, thing, thing, Weapon, def, null);
                thing.TakeDamage(dinfo).AssociateWithLog(log);
                hitTargets.Add(thing);
            }
        }

        protected override void RespawnPawn()
        {
            GenExplosion.DoExplosion(IntVec3.FromVector3(DestinationPos), Map, Props.landingEffectRadius,
                Props.landingDamageDef,
                FlyingPawn, ignoredThings: new List<Thing> {this, FlyingPawn});
            base.RespawnPawn();
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Props.chargeGraphic?.Graphic.Draw(drawLoc, flip ? Rot4.North : Rot4.South, this,
                DestinationPos.AngleToFlat(effectivePos));
        }
    }
}