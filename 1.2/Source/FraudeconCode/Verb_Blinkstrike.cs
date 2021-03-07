using Verse;
using Verse.AI;

namespace FraudeconCode
{
    public class Verb_Blinkstrike : Verb_CastBase
    {
        protected override bool TryCastShot()
        {
            var cell = CurrentTarget.HasThing ? CurrentTarget.Thing.RandomAdjacentCellCardinal() : CurrentTarget.Cell;
            var marker = (TeleMarker) GenSpawn.Spawn(ThingDef.Named("TeleportMarker"), caster.Position, caster.Map);
            marker.Target = caster;
            marker.EndTick = Find.TickManager.TicksGame + 300;
            caster.Position = cell;
            if (CasterIsPawn) CasterPawn.Notify_Teleported();
            return true;
        }
    }

    public class TeleMarker : Thing
    {
        public int EndTick;
        public Thing Target;

        public override void Tick()
        {
            if (EndTick > Find.TickManager.TicksGame) return;
            Target.Position = Position;
            if (Target is Pawn p) p.Notify_Teleported();
            Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EndTick, "endTick");
            Scribe_References.Look(ref Target, "target");
        }
    }
}