using System.Collections.Generic;
using Verse;

namespace ART
{
    public interface IChargeResource
    {
        Dictionary<Projectile, ChargeResources> ProjectilesWithChargedResource { get; }
    }
}