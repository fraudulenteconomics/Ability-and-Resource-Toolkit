using System.Collections.Generic;

namespace ART
{
    public interface IResourceProps
    {
        List<ResourceProperties> ResourceSettings { get; }

        List<ResourceProperties> TargetResourceSettings { get; }

        List<ChargeSettings> ChargeSettings { get; }
    }
}
