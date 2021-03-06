using HediffResourceFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FraudeconCode
{
    internal class Verb_ConsumeCorpse : Verb_CastBase
    {
        public VerbProps Props => verbProps as VerbProps;

        protected override bool TryCastShot()
        {
            var corpse = currentTarget.Cell.GetFirstThing<Corpse>(caster.Map);
            if (corpse == null ||
                Props.requireRotStage != null && corpse.GetRotStage() != Props.requireRotStage.Value ||
                Props.TargetResourceSettings.NullOrEmpty()) return false;
            foreach (var option in Props.TargetResourceSettings)
                HediffResourceUtils.AdjustResourceAmount(CasterPawn,
                    option.hediff, option.resourcePerUse,
                    option.addHediffIfMissing);

            corpse.Destroy();

            return true;
        }

        public override bool ValidateTarget(LocalTargetInfo target)
        {
            return base.ValidateTarget(target) && target.Cell.GetFirstThing<Corpse>(caster.Map) != null;
        }

        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            return base.CanHitTarget(targ) && targ.Cell.GetFirstThing<Corpse>(caster.Map) != null;
        }
    }
}