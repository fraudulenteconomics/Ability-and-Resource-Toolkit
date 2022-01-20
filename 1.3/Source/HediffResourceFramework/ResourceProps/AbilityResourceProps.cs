using System.Collections.Generic;
using Verse;

namespace HediffResourceFramework
{
    public class AbilityResourceProps : DefModExtension, IResourceProps
    {
        public List<HediffOption> resourceSettings;

        public List<HediffOption> targetResourceSettings;

        public List<ChargeSettings> chargeSettings;
        public List<HediffOption> ResourceSettings => resourceSettings;
        public List<HediffOption> TargetResourceSettings => targetResourceSettings;
        public List<ChargeSettings> ChargeSettings => chargeSettings;
    }
}
