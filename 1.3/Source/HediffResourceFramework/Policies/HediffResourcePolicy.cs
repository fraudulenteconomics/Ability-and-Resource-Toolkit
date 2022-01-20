using System.Collections.Generic;
using Verse;

namespace HediffResourceFramework
{
    public class HediffResourcePolicy : IExposable
    {
        public HediffResourcePolicy()
        {

        }

        public Dictionary<HediffResourceDef, HediffResourceSatisfyPolicy> satisfyPolicies;
        public void ExposeData()
        {
            Scribe_Collections.Look(ref satisfyPolicies, "satisfyPolicies");
        }
    }
}
