using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ART
{
    public class CompProperties_ApparelAdjustHediffs : CompProperties_AdjustHediffs
    {
        public CompProperties_ApparelAdjustHediffs()
        {
            this.compClass = typeof(CompApparelAdjustHediffs);
        }
    }

    public class CompApparelAdjustHediffs : CompAdjustHediffs
    {
        public Apparel Apparel => this.parent as Apparel;
        public override Pawn PawnHost => Apparel.Wearer;
        public override void Notify_Removed()
        {
            if (PawnHost != null)
            {
                HediffResourceUtils.RemoveExcessHediffResources(PawnHost, this);
            }
        }

        public override void Drop()
        {
            base.Drop();
            var pawn = Apparel.Wearer;
            if (pawn != null)
            {
                if (pawn.Map != null)
                {
                    pawn.apparel.TryDrop(Apparel);
                }
                else
                {
                    pawn.inventory.TryAddItemNotForSale(Apparel);
                }
            }
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            this.Notify_Removed();
            base.PostDestroy(mode, previousMap);
        }
        public override void ResourceTick()
        {
            base.ResourceTick();
            var pawn = PawnHost;
            if (pawn != null)
            {
                foreach (var resourceProperties in Props.resourceSettings)
                {
                    var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(resourceProperties.hediff) as HediffResource;
                    if (hediffResource != null && (PostUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame)
                        || !hediffResource.CanGainResource))
                    {
                        Log.Message("Can't gain resource: " + hediffResource);
                        continue;
                    }
                    else
                    {
                        float num = resourceProperties.GetResourceGain(this);
                        Log.Message(this + " resource gain for " + resourceProperties.hediff + " - " + num);
                        if (this.IsStorageFor(resourceProperties, out var resourceStorage))
                        {
                            if (resourceProperties.addHediffIfMissing && pawn.health.hediffSet.GetFirstHediffOfDef(resourceProperties.hediff) is null)
                            {
                                BodyPartRecord bodyPartRecord = null;
                                if (resourceProperties.applyToPart != null)
                                {
                                    bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == resourceProperties.applyToPart);
                                }
                                var hediff = HediffMaker.MakeHediff(resourceProperties.hediff, pawn, bodyPartRecord) as HediffResource;
                                pawn.health.AddHediff(hediff);
                            }
                            Log.Message("refilling storage");
                            var toRefill = Mathf.Min(num, resourceStorage.ResourceCapacity - resourceStorage.ResourceAmount);
                            Log.Message("refilling storage: " + toRefill); ;
                            resourceStorage.ResourceAmount += toRefill;
                        }
                        else
                        {
                            Log.Message(this + " adjusting resource for " + resourceProperties.hediff + " - " + num);
                            HediffResourceUtils.AdjustResourceAmount(pawn, resourceProperties.hediff, num, resourceProperties.addHediffIfMissing, resourceProperties, resourceProperties.applyToPart);
                        }
                    }
                }
            }
        }
    }
}
