using Verse;

namespace ART
{
    public class DamageAuraProperties : GeneralProperties
    {
        public DamageDef damageDef;
        public int damageAmount;
        public bool affectsAllies;
        public bool affectsEnemies = true;
        public bool affectsSelf;
        public ThingDef otherDamageMote;
        public ThingDef selfDamageMote;
        public GraphicData auraGraphic;
    }
}