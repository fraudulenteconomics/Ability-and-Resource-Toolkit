using System.Collections.Generic;
using System.Xml;

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
}
