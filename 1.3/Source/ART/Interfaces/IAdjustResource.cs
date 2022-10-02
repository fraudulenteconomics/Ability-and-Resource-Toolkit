using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ART
{
    public interface IAdjustResource
    {
        void Register();
        void Deregister();
        void Notify_Removed();
        void ResourceTick();
        void Update();
        Dictionary<HediffResource, HediffResouceDisable> PostUseDelayTicks { get; }
        Thing Parent { get; }
        Pawn PawnHost { get; }
        List<ResourceProperties> ResourceSettings { get; }
        string DisablePostUse { get; }
        bool TryGetQuality(out QualityCategory qc);
        ThingDef GetStuff();
        void Drop();
        bool IsStorageFor(ResourceProperties resourceProperties, out ResourceStorage resourceStorages);
    }
}