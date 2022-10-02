﻿using System.Collections.Generic;
using Verse;

namespace ART
{
    public class CompProperties_ChargeResource : CompProperties
    {
        public CompProperties_ChargeResource()
        {
            compClass = typeof(CompChargeResource);
        }
    }
    public class CompChargeResource : ThingComp, IChargeResource
    {
        private Dictionary<Projectile, ChargeResources> projectilesWithChargedResource = new Dictionary<Projectile, ChargeResources>();
        public Dictionary<Projectile, ChargeResources> ProjectilesWithChargedResource
        {
            get
            {
                if (projectilesWithChargedResource is null)
                {
                    projectilesWithChargedResource = new Dictionary<Projectile, ChargeResources>();
                }
                return projectilesWithChargedResource;
            }
        }
        public CompProperties_ChargeResource Props => (CompProperties_ChargeResource)props;
        public override void PostExposeData()
        {
            base.PostExposeData();
            projectilesWithChargedResource?.RemoveAll(x => x.Key.DestroyedOrNull());
            Scribe_Collections.Look(ref projectilesWithChargedResource, "projectilesWithChargedResource", LookMode.Reference, LookMode.Deep, ref projectileValues, ref projectileVlaues);
        }

        private List<Projectile> projectileValues;
        private List<ChargeResources> projectileVlaues;
    }
}
