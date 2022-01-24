using System.Collections.Generic;
using System.Xml;
using Verse;

namespace HediffResourceFramework
{

    public class ResourceGainPerDamage
    {
		public float? flat;
		public float? point;
		public DamageDef damageDef;

		public float GetResourceGain(float damageAmount)
        {
            float num = 0f;
			if (flat.HasValue)
            {
                num += flat.Value;
            }
            if (point.HasValue)
            {
                num += damageAmount * point.Value;
            }
            return num;
        }
	}
}
