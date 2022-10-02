using System.Collections.Generic;

namespace ART
{
    public interface IResourceStorage
    {
        Dictionary<int, ResourceStorage> ResourceStorages { get; }
        HediffResource GetResourceFor(ResourceProperties resourceProperties);
    }
}