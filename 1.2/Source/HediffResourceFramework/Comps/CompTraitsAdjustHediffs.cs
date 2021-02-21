using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    [StaticConstructorOnStartup]
    public static class AssignTraitCompToHumans
    {
        static AssignTraitCompToHumans()
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.race?.Humanlike ?? false))
            {
                thingDef.comps.Add(new CompProperties_TraitsAdjustHediffs());
            }
        }
    }
    public class CompProperties_TraitsAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_TraitsAdjustHediffs()
        {
            this.compClass = typeof(CompTraitsAdjustHediffs);
        }
    }

    public class CompTraitsAdjustHediffs : CompAdjustHediffs
    {
        public Pawn Pawn => this.parent as Pawn;

        public static Dictionary<TraitDef, TraitsAdjustHediff> cachedModExtensions = new Dictionary<TraitDef, TraitsAdjustHediff>();
        public override List<HediffOption> ResourceSettings
        {
            get
            {
                var resourceSettings = new List<HediffOption>();
                foreach (var trait in Pawn.story?.traits?.allTraits)
                {
                    if (!cachedModExtensions.TryGetValue(trait.def, out TraitsAdjustHediff traitAdjustOptions))
                    {
                        traitAdjustOptions = trait.def.GetModExtension<TraitsAdjustHediff>();
                        cachedModExtensions[trait.def] = traitAdjustOptions;
                    }
                    if (traitAdjustOptions?.resourceSettings != null)
                    {
                        resourceSettings.AddRange(traitAdjustOptions.resourceSettings);
                    }
                }
                return resourceSettings;
            }
        }

        public override void Drop()
        {
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            this.Notify_Removed();
            base.PostDestroy(mode, previousMap);
        }
        public override void ResourceTick()
        {
            base.ResourceTick();
            var pawn = Pawn;
            if (pawn != null && !pawn.Dead)
            {
                foreach (var option in ResourceSettings)
                {
                    var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(option.hediff) as HediffResource;
                    if (PostUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame))
                    {
                        continue;
                    }
                    if (hediffResource != null && !hediffResource.CanGainResource)
                    {
                        continue;
                    }
                    else
                    {
                        float num = option.resourcePerSecond;
                        HediffResourceUtils.AdjustResourceAmount(Pawn, option.hediff, num, option.addHediffIfMissing);
                    }
                }
            }
        }
    }
}
