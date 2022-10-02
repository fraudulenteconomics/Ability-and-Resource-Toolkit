using Verse;

namespace ART
{
    public class GeneralFuelProperties
    {
        public ThingDef fuelThingDef;
        public float resourcePerThing;
    }
    public enum RefuelUseType { Over, Under }
    public class RefuelHediffProperties : GeneralFuelProperties
    {
        public int refuelTime;
        public bool useFromInventory;
        public float useThreshold;
        public RefuelUseType useType;
        public SoundDef refuelSound;
    }
}
