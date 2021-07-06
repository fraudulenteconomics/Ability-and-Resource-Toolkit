using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
	public class StatWorker_ResourceOffset : StatWorker
	{
		public override bool ShouldShowFor(StatRequest req)
		{
			Thing thing = req.Thing;
			if (thing?.def != null)
            {
				var options = stat.GetModExtension<StatWorkerExtender>();
				if (thing.def.IsApparel)
				{
					var comp = thing.TryGetComp<CompApparelAdjustHediffs>();
					if (comp != null)
					{
						foreach (var hediffOption in comp.Props.resourceSettings)
						{
							if (options.hediffResource == hediffOption.hediff && GetValue(hediffOption, thing) != 0)
							{
								return true;
							}
						}
					}
				}
				else if (thing.def.IsWeapon)
				{
					var comp = thing.TryGetComp<CompWeaponAdjustHediffs>();
					if (comp != null)
					{
						foreach (var hediffOption in comp.Props.resourceSettings)
						{
							if (options.hediffResource == hediffOption.hediff && GetValue(hediffOption, thing) != 0)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
		{
			var thing = req.Thing;
			var options = stat.GetModExtension<StatWorkerExtender>();
			if (thing.def.IsApparel)
            {
				var comp = thing.TryGetComp<CompApparelAdjustHediffs>();
				if (comp != null)
                {
					foreach (var hediffOption in comp.Props.resourceSettings)
                    {
						if (options.hediffResource == hediffOption.hediff)
                        {
							val += GetValue(hediffOption, thing);
						}
                    }
                }
			}
			else if (thing.def.IsWeapon)
            {
				var comp = thing.TryGetComp<CompWeaponAdjustHediffs>();
				if (comp != null)
				{
					foreach (var hediffOption in comp.Props.resourceSettings)
					{
						if (options.hediffResource == hediffOption.hediff)
						{
							val += GetValue(hediffOption, thing);
						}
					}
				}
			}
			base.FinalizeValue(req, ref val, applyPostProcess);
		}

		public float GetValue(HediffOption hediffOption, Thing thing)
        {
			if (hediffOption.qualityScalesResourcePerSecond && thing.TryGetQuality(out QualityCategory qc))
			{
				return hediffOption.resourcePerSecond * HediffResourceUtils.GetQualityMultiplier(qc);
			}
			else
			{
				return hediffOption.resourcePerSecond;
			}
		}
        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(base.GetExplanationFinalizePart(req, numberSense, finalVal));
			return stringBuilder.ToString();
		}
	}
}
