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
        public BodyPartDef applyToPart;
        public List<HediffResourceDef> blacklistHediffsPreventAdd;
        public HediffDef blacklistHediffPoison;
        public string blacklistHediffPoisonMessage;
        public string cannotDrinkReason;
        public bool addHediffIfMissing;

        public ChemicalDef toleranceChemical;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
        {
            if (blacklistHediffsPreventAdd != null)
            {
                foreach (var blacklistHediff in blacklistHediffsPreventAdd)
                {
                    HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - DoIngestionOutcomeSpecial - var hd = pawn.health.hediffSet.GetFirstHediffOfDef(blacklistHediff); - 3", true);
                    var hd = pawn.health.hediffSet.GetFirstHediffOfDef(blacklistHediff);
                    HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - DoIngestionOutcomeSpecial - if (hd != null) - 4", true);
                    if (hd != null)
                    {
                        HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - DoIngestionOutcomeSpecial - if (blacklistHediffPoison != null) - 5", true);
                        if (blacklistHediffPoison != null)
                        {
                            HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - DoIngestionOutcomeSpecial - var poison = HediffMaker.MakeHediff(blacklistHediffPoison, pawn); - 6", true);
                            var poison = HediffMaker.MakeHediff(blacklistHediffPoison, pawn);
                            HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - DoIngestionOutcomeSpecial - pawn.health.AddHediff(poison); - 7", true);
                            pawn.health.AddHediff(poison);
                            HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - DoIngestionOutcomeSpecial - Messages.Message(blacklistHediffPoisonMessage.Translate(pawn.Named(\"PAWN\"), ingested.Named(\"INGESTED\")), pawn, MessageTypeDefOf.NegativeHealthEvent); - 8", true);
                            Messages.Message(blacklistHediffPoisonMessage.Translate(pawn.Named("PAWN"), ingested.Named("INGESTED")), pawn, MessageTypeDefOf.NegativeHealthEvent);
                        }
                        return;
                    }
                }
            }
            HediffResource hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) as HediffResource;
            if (hediff is null && addHediffIfMissing)
            {
                BodyPartRecord bodyPartRecord = null;
                if (applyToPart != null)
                {
                    bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == applyToPart);
                }

                hediff = HediffMaker.MakeHediff(hediffDef, pawn, bodyPartRecord) as HediffResource;
                pawn.health.AddHediff(hediff);
            }
            if (hediff != null)
            {
                if (resourceAdjust != 0f)
                {
                    if (toleranceChemical != null)
                    {
                        AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(pawn, toleranceChemical, ref resourceAdjust);
                    }
                    hediff.ResourceAmount += resourceAdjust;
                }
                if (resourcePercent != -1f)
                {
                    var resourceAmount = hediff.ResourceCapacity * resourcePercent;
                    if (toleranceChemical != null)
                    {
                        AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(pawn, toleranceChemical, ref resourceAmount);
                    }
                    hediff.ResourceAmount += resourceAdjust;
                }
            }
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
        {
            HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - SpecialDisplayStats - if (parentDef.IsDrug && chance >= 1f) - 18", true);
            if (parentDef.IsDrug && chance >= 1f)
            {
                HRFLog.Message("IngestionOutcomeDoer_GiveHediffResource : IngestionOutcomeDoer - SpecialDisplayStats - foreach (StatDrawEntry item in hediffDef.SpecialDisplayStats(StatRequest.ForEmpty())) - 19", true);
                foreach (StatDrawEntry item in hediffDef.SpecialDisplayStats(StatRequest.ForEmpty()))
                {
                    yield return item;
                }
            }
        }
    }
}