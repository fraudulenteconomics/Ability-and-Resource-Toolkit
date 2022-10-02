using Verse;

namespace ART
{
    public interface IAdjustResouceInArea : IAdjustResource
    {
        bool InRadiusFor(IntVec3 position, HediffResourceDef def);
        ResourceProperties GetFirstResourcePropertiesFor(HediffResourceDef def);
        float GetResourceCapacityGainFor(HediffResourceDef def);
    }
}