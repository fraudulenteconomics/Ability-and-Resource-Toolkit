using System.Collections.Generic;
using Verse;

namespace ART
{
    public class VerbResourceProps : VerbProperties, IResourceProps
    {
        public List<ResourceProperties> resourceSettings;

        public List<ResourceProperties> targetResourceSettings;

        public List<ChargeSettings> chargeSettings;
        public List<ResourceProperties> ResourceSettings => resourceSettings;
        public List<ResourceProperties> TargetResourceSettings => targetResourceSettings;
        public List<ChargeSettings> ChargeSettings => chargeSettings;
    }
}
