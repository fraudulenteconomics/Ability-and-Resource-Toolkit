using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ART
{
    public interface IAdjustResouceInArea : IAdjustResource
    {
        bool InRadiusFor(IntVec3 position, HediffResourceDef def);
        HediffOption GetFirstHediffOptionFor(HediffResourceDef def);
        float GetResourceCapacityGainFor(HediffResourceDef def);
    }
}