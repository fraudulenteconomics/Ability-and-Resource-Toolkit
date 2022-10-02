using RimWorld;
using System.Text;
using Verse;

namespace ART
{
    public class StatWorker_ResourceOffset : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            var thing = req.Thing;
            if (thing?.def != null)
            {
                var options = stat.GetModExtension<StatWorkerExtender>();
                if (thing.def.IsApparel)
                {
                    var comp = thing.TryGetComp<CompApparelAdjustHediffs>();
                    if (comp != null)
                    {
                        foreach (var resourceProperties in comp.Props.resourceSettings)
                        {
                            if (options.hediffResource == resourceProperties.hediff && GetValue(resourceProperties, thing) != 0)
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
                        foreach (var resourceProperties in comp.Props.resourceSettings)
                        {
                            if (options.hediffResource == resourceProperties.hediff && GetValue(resourceProperties, thing) != 0)
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
                    foreach (var resourceProperties in comp.Props.resourceSettings)
                    {
                        if (options.hediffResource == resourceProperties.hediff)
                        {
                            val += GetValue(resourceProperties, thing);
                        }
                    }
                }
            }
            else if (thing.def.IsWeapon)
            {
                var comp = thing.TryGetComp<CompWeaponAdjustHediffs>();
                if (comp != null)
                {
                    foreach (var resourceProperties in comp.Props.resourceSettings)
                    {
                        if (options.hediffResource == resourceProperties.hediff)
                        {
                            val += GetValue(resourceProperties, thing);
                        }
                    }
                }
            }
            base.FinalizeValue(req, ref val, applyPostProcess);
        }

        public float GetValue(ResourceProperties resourceProperties, Thing thing)
        {
            if (resourceProperties.qualityScalesResourcePerSecond && thing.TryGetQuality(out var qc))
            {
                return resourceProperties.resourcePerSecond * Utils.GetQualityMultiplier(qc);
            }
            else
            {
                return resourceProperties.resourcePerSecond;
            }
        }
        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetExplanationFinalizePart(req, numberSense, finalVal));
            return stringBuilder.ToString();
        }
    }
}
