using Verse;

namespace FraudeconCode
{
    public class DamageWorker_AddInternalInjury : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            dinfo.SetBodyRegion(depth: BodyPartDepth.Inside);
            dinfo.SetAllowDamagePropagation(false);
            return base.Apply(dinfo, thing);
        }
    }
}