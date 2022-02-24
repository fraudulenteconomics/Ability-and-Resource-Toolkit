using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ART
{
    public class ResourceEffect
    {
        public HediffResourceDef hediffDef;
        public float adjustTargetResource;
        public IntRange delayTargetOnDamage = IntRange.zero;
        public bool addHediffIfMissing;
        public float severityPerDamage;
        public BodyPartDef applyToPart;
        public bool applyToDamagedPart;
    }
    public class EffectOnImpact : DefModExtension
    {
        public List<ResourceEffect> resourceEffects;
    }
}
