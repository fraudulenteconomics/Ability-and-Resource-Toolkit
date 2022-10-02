﻿using Verse;

namespace ART
{
    public class HediffCompProperties_ResourcePerSecond : HediffCompProperties
    {
        public float resourcePerSecond;
        public HediffCompProperties_ResourcePerSecond()
        {
            compClass = typeof(HediffComp_ResourcePerSecond);
        }
    }

    public class HediffComp_ResourcePerSecond : HediffComp
    {
        public HediffCompProperties_ResourcePerSecond Props => (HediffCompProperties_ResourcePerSecond)props;
        public HediffResource HediffResource => parent as HediffResource;
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            ARTLog.Message("base.Pawn: " + base.Pawn + ", HediffResource: " + HediffResource);

            if (base.Pawn?.IsHashIntervalTick(60) ?? (false && HediffResource.CanGainResource))
            {
                HediffResource.ChangeResourceAmount(Props.resourcePerSecond);
            }
        }
    }
}