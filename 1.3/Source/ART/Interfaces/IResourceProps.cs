using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ART
{
    public interface IResourceProps
    {
        List<HediffOption> ResourceSettings { get; }

        List<HediffOption> TargetResourceSettings { get; }

        List<ChargeSettings> ChargeSettings { get; }
    }
}
