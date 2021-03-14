using System.Linq;
using HediffResourceFramework;
using RimWorld;
using Verse;

namespace FraudeconCode
{
    internal class Verb_ConsumeCorpse : BaseVerb
    {
        public VerbProps Props => verbProps as VerbProps;

        private Corpse GetCorpse(LocalTargetInfo target)
        {
            return target.Cell.GetThingList(caster.Map).OfType<Corpse>().FirstOrDefault(c =>
                Props.requireRotStage == null || c.GetRotStage() == Props.requireRotStage);
        }

        protected override bool TryCastShot()
        {
            var corpse = GetCorpse(currentTarget);
            if (corpse == null) return false;
            foreach (var option in Props.TargetResourceSettings)
                HediffResourceUtils.AdjustResourceAmount(CasterPawn,
                    option.hediff, option.resourcePerUse,
                    option.addHediffIfMissing, null);

            corpse.Destroy();

            return true;
        }

        public override bool ValidateTarget(LocalTargetInfo target)
        {
            return base.ValidateTarget(target) && GetCorpse(target) != null;
        }

        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            return base.CanHitTarget(targ) && GetCorpse(targ) != null;
        }
    }
}