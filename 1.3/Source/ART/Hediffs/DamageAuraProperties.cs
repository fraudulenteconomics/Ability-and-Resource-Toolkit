using Verse;

namespace ART
{
    public class DamageAuraProperties
    {
        public DamageDef damageDef;
        public int damageAmount;
        public int ticksPerEffect;
        public float effectRadius;
        public bool worksThroughWalls;
        public bool affectsAllies;
        public bool affectsEnemies = true;
        public bool affectsSelf;
        public ThingDef otherDamageMote;
        public ThingDef selfDamageMote;
        public GraphicData auraGraphic;
        public SoundDef soundOnEffect;
    }
}