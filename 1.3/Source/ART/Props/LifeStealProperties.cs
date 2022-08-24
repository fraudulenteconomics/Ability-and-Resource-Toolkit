using RimWorld;
using System.Linq;
using Verse;

namespace ART
{
    public class LifeStealProperties
    {
        public DamageDef damageDef;
        public float flatHeal;
        public float percentHeal;
        public bool checkOrganic;
        public bool healOverflow;
        public HealPriority healPriority;
        public float effectRadius;
        public bool affectSelf;
        public bool affectsAllies;
        public bool affectsEnemies = true;
        public bool worksThroughWalls;
        public SoundDef soundOnEffect;
        public bool affectMelee;
        public bool affectRanged;

        public void StealLife(Pawn instigator, Pawn targetPawn, DamageInfo source)
        {
            ARTLog.Message("Stealing life: " + instigator + " - " + targetPawn + " - " + source);
            if (damageDef is null || damageDef == source.Def)
            {
                foreach (var pawn in Utils.GetPawnsAround(instigator, effectRadius))
                {
                    ARTLog.Message("Checking pawn: " + pawn);
                    if (CanExtractLife(instigator, pawn, targetPawn))
                    {
                        ARTLog.Message("Healing pawn: " + pawn);
                        var hediffsToHeal = pawn.health.hediffSet.hediffs.Where(x => x is Hediff_Injury).ToList();
                        var healPoints = flatHeal > 0 ? flatHeal : source.Amount * percentHeal;
                        Utils.HealHediffs(pawn, ref healPoints, hediffsToHeal, healOverflow, healPriority, false, soundOnEffect);
                    }
                    else
                    {
                        ARTLog.Message("Cannot work on " + pawn);
                    }
                }
            }
        }
        private bool CanExtractLife(Pawn instigator, Pawn toGive, Pawn targetPawn)
        {
            if (checkOrganic && toGive.RaceProps.IsFlesh != targetPawn.RaceProps.IsFlesh
                || !worksThroughWalls && !GenSight.LineOfSight(toGive.Position, targetPawn.Position, toGive.Map)
                || !affectSelf && instigator == toGive 
                || !affectsAllies && targetPawn.Faction != null && !toGive.HostileTo(instigator)
                || !affectsEnemies && toGive.HostileTo(instigator))
            {
                return false;
            }
            return true;
        }
    }
}