using Verse;

namespace ART
{
    public class HealingProperties
    {
        public float healPoints;
        public int hediffsToHeal;
        public bool affectPermanent;
        public bool affectChronic;
        public bool affectIllness;
        public bool affectInjuries;
        public bool pointsOverflow;
        public HealPriority healPriority;
        public bool affectMechanical;
        public bool affectOrganic = true;
        public float effectRadius;
        public bool affectsAllies = true;
        public bool affectsEnemies = false;
        public int ticksPerEffect;
        public bool healOnApply;
        public SoundDef soundOnEffect;
    }
}