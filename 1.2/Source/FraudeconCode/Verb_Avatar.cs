using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FraudeconCode
{
    public class Verb_Avatar : Verb_CastBase
    {
        public VerbProps Props => verbProps as VerbProps;

        protected override bool TryCastShot()
        {
            var pawns = GenRadial.RadialDistinctThingsAround(caster.Position, caster.Map, Props.effectRadius, true)
                .OfType<Pawn>().Where(p => p.Faction == caster.Faction).Take(Props.maxTargets).ToList();
            Props.effectCount.SortBy(mcd => mcd.minCount);
            var minCountDef = Props.effectCount.Last(mcd => mcd.minCount <= pawns.Count);
            Thing thing;
            if (!minCountDef.spawnDef.NullOrEmpty())
                thing = ThingMaker.MakeThing(ThingDef.Named(minCountDef.spawnDef));
            else if (!minCountDef.pawnKindDef.NullOrEmpty())
                thing = PawnGenerator.GeneratePawn(PawnKindDef.Named(minCountDef.pawnKindDef), Faction.OfPlayer);
            else return false;
            GenSpawn.Spawn(thing, CurrentTarget.Cell, caster.Map);
            thing.SetFaction(caster.Faction);
            if (thing is Pawn pwn) pwn.drafter = pwn.drafter ?? new Pawn_DraftController(pwn);
            foreach (var pawn in pawns) pawn.health.AddHediff(Props.effectHediff);
            return true;
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            GenDraw.DrawRadiusRing(caster.Position, Props.effectRadius);
        }
    }
}