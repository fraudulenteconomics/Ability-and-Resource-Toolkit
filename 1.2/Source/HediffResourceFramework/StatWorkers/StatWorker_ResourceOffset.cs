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
			var options = stat.GetModExtension<StatWorkerExtender>();
			if (thing.def.IsApparel)
			{
				var comp = thing.TryGetComp<CompApparelAdjustHediffs>();
				if (comp != null)
				{
					foreach (var hediffOption in comp.Props.hediffOptions)
					{
						if (options.hediffResource == hediffOption.hediff)
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
					foreach (var hediffOption in comp.Props.hediffOptions)
					{
						if (options.hediffResource == hediffOption.hediff)
						{
							return true;
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
					foreach (var hediffOption in comp.Props.hediffOptions)
                    {
						if (options.hediffResource == hediffOption.hediff)
                        {
							if (hediffOption.qualityScalesResourcePerTick && thing.TryGetQuality(out QualityCategory qc))
							{
								val += hediffOption.resourcePerTick * HediffResourceUtils.GetQualityMultiplier(qc);
							}
							else
							{
								val += hediffOption.resourcePerTick;
							}
						}
                    }
                }
			}
			else if (thing.def.IsWeapon)
            {
				var comp = thing.TryGetComp<CompWeaponAdjustHediffs>();
				if (comp != null)
				{
					foreach (var hediffOption in comp.Props.hediffOptions)
					{

						if (options.hediffResource == hediffOption.hediff)
						{
							if (hediffOption.qualityScalesResourcePerTick && thing.TryGetQuality(out QualityCategory qc))
							{
								val += hediffOption.resourcePerTick * HediffResourceUtils.GetQualityMultiplier(qc);
							}
							else
							{
								val += hediffOption.resourcePerTick;
							}
						}
					}
				}
			}
			base.FinalizeValue(req, ref val, applyPostProcess);
		}
        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(base.GetExplanationFinalizePart(req, numberSense, finalVal));
			return stringBuilder.ToString();
		}
	}
}
