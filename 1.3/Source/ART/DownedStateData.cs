﻿using System.Collections.Generic;
using Verse;

namespace ART
{
    public class DownedStateData : IExposable
    {
        public Dictionary<HediffResourceDef, int> lastDownedEffectTicks;
        public void ExposeData()
        {
            Scribe_Collections.Look(ref lastDownedEffectTicks, "lastDownedEffectTicks", LookMode.Def, LookMode.Value);
        }
    }
}
