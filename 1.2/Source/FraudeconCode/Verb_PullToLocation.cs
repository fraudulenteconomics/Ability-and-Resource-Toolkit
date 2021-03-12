using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FraudeconCode
{
    public class Verb_PullToLocation : BaseVerb
    {
        public VerbProps Props => verbProps as VerbProps;

        protected override bool TryCastShot()
        {
            var puller = (Puller) GenSpawn.Spawn(ThingDef.Named("Puller"), CurrentTarget.Cell, caster.Map);
            puller.Caster = caster;
            puller.Weapon = EquipmentSource?.def;
            puller.Props = Props;
            return true;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return Props.effectRadius;
        }
    }

    public class Puller : Thing
    {
        public Thing Caster;
        private int lastDamageTick = Find.TickManager.TicksGame;
        private List<PawnMover> movers = new List<PawnMover>();
        public VerbProps Props;
        private int startTick = Find.TickManager.TicksGame;
        public ThingDef Weapon;

        public override void Tick()
        {
            if (lastDamageTick + Props.effectRate <= Find.TickManager.TicksGame)
            {
                movers.RemoveAll(mover => mover.FlyingPawn == null);
                foreach (var pawn in GenRadial.RadialDistinctThingsAround(Position, Map, Props.effectRadius, false)
                    .OfType<Pawn>().Except(movers.Select(mover => mover.FlyingPawn)))
                {
                    Log.Message("Pulling pawn: " + pawn + " from " + pawn.Position + " to " + Position);
                    var mover = (PulledPawn) PawnFlyer.MakeFlyer(ThingDef.Named("PulledPawn"), pawn, Position);
                    if (mover == null) continue;
                    GenSpawn.Spawn(mover, Position, Map);
                    movers.Add(mover);
                }

                lastDamageTick = Find.TickManager.TicksGame;
                movers.ForEach(mover =>
                {
                    if (mover.FlyingPawn == null) return;
                    var dinfo = new DamageInfo(Props.effectDamageDef, Props.effectDamageAmount,
                        Props.effectDamageDef.defaultArmorPenetration, DrawPos.AngleToFlat(mover.DrawPos), Caster, null,
                        Weapon);
                    var log = new BattleLogEntry_RangedImpact(Caster, mover.FlyingPawn, mover.FlyingPawn, Weapon, def,
                        null);
                    mover.FlyingPawn.TakeDamage(dinfo).AssociateWithLog(log);
                });
            }

            if (startTick + Props.effectDuration <= Find.TickManager.TicksGame)
            {
                Log.Message("Ending!");
                movers.ForEach(mover => mover.End());
                movers.Clear();
                Destroy();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Caster, "caster");
            Scribe_Deep.Look(ref Props, "props");
            Scribe_Defs.Look(ref Weapon, "weapon");
            Scribe_Collections.Look(ref movers, "movers", LookMode.Reference);
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref lastDamageTick, "lastDamageTick");
        }
    }
}