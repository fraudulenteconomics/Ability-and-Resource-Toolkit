﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace HediffResourceFramework
{
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
    public class HediffResourceDef : HediffDef
    {
        public float maxResourceCapacity;
        public float initialResourceAmount;
		public ResourceGainPerDamage resourceGainPerDamages;
	}
}
