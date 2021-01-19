using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public static class HediffResourceUtils
    {
		public static bool VerbMatches(Verb verb, HediffOption option)
        {
			if (!option.verbLabel.NullOrEmpty())
			{
				if (verb.ReportLabel == option.verbLabel)
				{
					return true;
				}
				return false;
			}
			else if (option.verbIndex != -1)
			{
				if (verb.EquipmentSource.def.Verbs.IndexOf(verb.verbProps) == option.verbIndex)
				{
					return true;
				}
				return false;
			}
			return true;
		}
        public static bool IsUsableBy(Verb verb)
        {
			if (verb.CasterIsPawn && verb.EquipmentSource != null)
            {
				var options = verb.EquipmentSource.def.GetModExtension<HediffAdjustOptions>();
				if (options != null)
				{
					foreach (var option in options.hediffOptions)
					{
						if (VerbMatches(verb, option))
                        {
							var manaHediff = verb.CasterPawn.health.hediffSet.GetFirstHediffOfDef(option.hediff);
							if (option.disableOnEmptyOrMissingHediff)
							{
								bool manaIsEmptyOrNull = manaHediff != null ? manaHediff.Severity <= 0 : true;
								Log.Message("disableOnEmptyOrMissingHediff: " + verb.CasterPawn + " - " + option.hediff + " - manaHediff: " + manaHediff + " - " + manaIsEmptyOrNull + " - " + manaHediff?.Severity);
								if (manaIsEmptyOrNull)
								{
									return false;
								}
							}
							if (option.minimumSeverityCastRequirement != -1f)
							{
								Log.Message("minimumSeverityCastRequirement: " + verb.CasterPawn + " - " + option.hediff + " - manaHediff: " + manaHediff + " - " + " - " + manaHediff?.Severity);
								if (manaHediff.Severity < option.minimumSeverityCastRequirement)
								{
									return false;
								}
							}
						}
					}
				}
			}
			return true;
		}
    }
}
