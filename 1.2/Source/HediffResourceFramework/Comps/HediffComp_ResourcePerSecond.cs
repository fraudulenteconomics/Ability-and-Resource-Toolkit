using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HediffResourceFramework
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
			if (base.Pawn.IsHashIntervalTick(60) && HediffResource.CanGainResource)
			{
				HediffResource.ResourceAmount += Props.resourcePerSecond;
			}
		}
	}
}