using Verse;

namespace ART
{
    public enum RefuelUseType { Over, Under }
    public class RefuelHediffProperties
    {
        public ThingDef fuelThingDef;
        public float resourcePerThing;
        public int refuelTime;
        public bool useFromInventory;
        public float useThreshold;
        public RefuelUseType useType;
        public SoundDef refuelSound;
    }
}
