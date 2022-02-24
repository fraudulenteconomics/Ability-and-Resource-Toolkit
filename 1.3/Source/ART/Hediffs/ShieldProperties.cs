using System.Collections.Generic;
using UnityEngine;

namespace ART
{
    public class ShieldProperties
	{
		public bool absorbMeleeDamage;
		public bool absorbRangeDamage;
		public int? maxAbsorb;
		public int? resourceConsumptionPerDamage;
		public float? damageAbsorbedPerResource;
		public int? postDamageDelay;
		public Color shieldColor = Color.white;
		public VerbType cannotUseVerbType;
		public List<HediffResourceDef> activeWithHediffs;
	}
}
