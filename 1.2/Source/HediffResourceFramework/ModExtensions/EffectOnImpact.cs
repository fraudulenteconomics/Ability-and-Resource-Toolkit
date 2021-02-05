using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class ResourceEffect
    {
        public HediffResourceDef hediffDef;
        public float adjustTargetResource;
        public bool removeTargetResource;
        public IntRange delayTargetOnDamage = IntRange.zero;
        public List<HediffDef> additionalHediffs;
    }
    public class EffectOnImpact : DefModExtension
    {
        public List<ResourceEffect> resourceEffects;
    }
}
