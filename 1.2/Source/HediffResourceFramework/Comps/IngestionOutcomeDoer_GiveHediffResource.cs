using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
	public class IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer
	{
		public HediffResourceDef hediffDef;

		public float resourceAdjust = 0f;

		public float resourcePercent = -1f;

		public List<HediffResourceDef> blacklistHediffsPreventAdd;
		public HediffDef blacklistHediffPoison;
		public string blacklistHediffPoisonMessage;
		public string cannotDrinkReason;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
        {
            if (blacklistHediffsPreventAdd != null)
            {
                foreach (var blacklistHediff in blacklistHediffsPreventAdd)
                {
                    var hd = pawn.health.hediffSet.GetFirstHediffOfDef(blacklistHediff);
                    if (hd != null)
                    {
                        if (blacklistHediffPoison != null)
                        {
                            var poison = HediffMaker.MakeHediff(blacklistHediffPoison, pawn);
                            pawn.health.AddHediff(poison);
                            Messages.Message(blacklistHediffPoisonMessage.Translate(pawn.Named("PAWN"), ingested.Named("INGESTED")), pawn, MessageTypeDefOf.NegativeHealthEvent);
                        }
                        return;
                    }
                }
            }
            HediffResource hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) as HediffResource;
            if (hediff is null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn) as HediffResource;
            }
            pawn.health.AddHediff(hediff);
            if (resourceAdjust != 0f)
            {
                hediff.ResourceAmount += resourceAdjust;
            }
            if (resourcePercent != -1f)
            {
                hediff.ResourceAmount += hediff.ResourceCapacity * resourcePercent;
            }
        }

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
		{
			if (parentDef.IsDrug && chance >= 1f)
			{
				foreach (StatDrawEntry item in hediffDef.SpecialDisplayStats(StatRequest.ForEmpty()))
				{
					yield return item;
				}
			}
		}
	}
}
