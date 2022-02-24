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
	public interface IResourceGenerator
	{
        float ResourceAmount { get; set; }
        bool CanGainResource { get; }
        float ResourceCapacity { get; }
    }
}