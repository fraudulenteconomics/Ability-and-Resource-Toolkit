using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class VerbResourceProps : VerbProperties, IResourceProps
    {
        public List<HediffOption> resourceSettings;

        public List<HediffOption> targetResourceSettings;

        public List<ChargeSettings> chargeSettings;

        public List<HediffOption> ResourceSettings => resourceSettings;

        public List<HediffOption> TargetResourceSettings => targetResourceSettings;

        public List<ChargeSettings> ChargeSettings => chargeSettings;
    }
}
