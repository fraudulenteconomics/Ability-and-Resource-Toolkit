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
    public class HediffResourceDef : HediffDef
    {
        public float maxResourceCapacity;
        public float initialResourceAmount;
		public List<ResourceGainPerDamage> resourceGainPerDamages;
		public float resourceGainPerAllDamages;
		public ShieldProperties shieldProperties;
		public bool keepWhenEmpty;
		public int lifetimeTicks = -1;
		public bool hideResourceAmount;

		public bool showResourceBar;
		public Color? backgroundBarColor;
		public Color? progressBarColor;
		public Color? resourceBarTextColor;
		public string fulfilsTranslationKey;

		public bool sendLetterWhenGained;
		public LetterDef letterType;
		public string letterTitleKey;
		public string letterMessageKey;

		public List<RequiredHediff> requiredHediffs;

		public bool showInResourceTab;
		public float resourcePerSecondFactor = 1f;

		public bool ShieldIsActive(Pawn pawn)
        {
			if (shieldProperties != null)
            {
				if (shieldProperties.activeWithHediffs != null && pawn.health.hediffSet.hediffs.Any())
                {
					return pawn.health.hediffSet.hediffs.All(x => shieldProperties.activeWithHediffs.Contains(x.def));
				}
				return true;
            }
			return false;
        }
	}
}
