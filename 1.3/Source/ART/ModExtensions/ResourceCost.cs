using System.Xml;
using Verse;

namespace ART
{
    public class ResourceCost
	{
		public HediffResourceDef resource;

		public float cost;
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "resource", xmlRoot);
			cost = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
		}
	}
}
