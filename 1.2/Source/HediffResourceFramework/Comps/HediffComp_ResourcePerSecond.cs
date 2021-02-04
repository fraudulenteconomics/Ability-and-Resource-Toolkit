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

		public bool showDaysToRecover;

		public bool showHoursToRecover;

		public HediffCompProperties_ResourcePerSecond()
		{
			compClass = typeof(HediffComp_ResourcePerSecond);
		}
	}

	public class HediffComp_ResourcePerSecond : HediffComp
	{
		private HediffCompProperties_ResourcePerSecond Props => (HediffCompProperties_ResourcePerSecond)props;

		public HediffResource HediffResource => parent as HediffResource;
		public override string CompLabelInBracketsExtra
		{
			get
			{
				if (props is HediffCompProperties_ResourcePerSecond && Props.showHoursToRecover && ResourceChangePerDay() < 0f)
				{
					return Mathf.RoundToInt(HediffResource.ResourceAmount / Mathf.Abs(ResourceChangePerDay()) * 24f) + (string)"LetterHour".Translate();
				}
				return null;
			}
		}

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			if (base.Pawn.IsHashIntervalTick(60))
			{
				float num = ResourceChangePerDay();
				num *= 0.00333333341f;
				num /= 3.33f;
				HediffResource.ResourceAmount += num;
			}
		}

		public float ResourceChangePerDay()
		{
			return Props.resourcePerSecond;
		}

		public override string CompDebugString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.CompDebugString());
			if (!base.Pawn.Dead)
			{
				stringBuilder.AppendLine("resource/day: " + ResourceChangePerDay().ToString("F3"));
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}
	}
}