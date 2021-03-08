using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace HediffResourceFramework
{
	public enum VerbType
    {
		None,
		Both,
		Range,
		Melee
    }
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
	}
    public class ResourceGainPerDamage
    {
		public Dictionary<string, float> resourceGainOffsets = new Dictionary<string, float>();
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			foreach (XmlNode childNode in xmlRoot.ChildNodes)
			{
				if (!(childNode is XmlComment))
				{
					resourceGainOffsets[childNode.Name] = float.Parse(childNode.InnerText);
				}
			}
		}
	}

	public class RequiredHediff
    {
		public HediffDef hediff;
		public int minCount;
		public float minSeverity;
	}
    public class HediffResourceDef : HediffDef
    {
        public float maxResourceCapacity;
        public float initialResourceAmount;
		public ResourceGainPerDamage resourceGainPerDamages;
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
	}
}
