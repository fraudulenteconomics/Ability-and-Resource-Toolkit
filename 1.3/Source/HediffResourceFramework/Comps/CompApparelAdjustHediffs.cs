using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HediffResourceFramework
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
            if (Apparel.Wearer != null)
            {
                HediffResourceUtils.RemoveExcessHediffResources(Apparel.Wearer, this);
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
                foreach (var hediffOption in Props.resourceSettings)
                {
                    var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
                    if (hediffResource != null && (PostUseDelayTicks.TryGetValue(hediffResource, out var disable) && (disable.delayTicks > Find.TickManager.TicksGame)
                        || !hediffResource.CanGainResource))
                    {
                        Log.Message("Can't gain resource: " + hediffResource);
                        continue;
                    }
                    else
                    {
                        float num = hediffOption.GetResourceGain(this);
                        if (this.IsStorageFor(hediffOption, out var resourceStorage))
                        {
                            if (hediffOption.addHediffIfMissing && pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) is null)
                            {
                                BodyPartRecord bodyPartRecord = null;
                                if (hediffOption.applyToPart != null)
                                {
                                    bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == hediffOption.applyToPart);
                                }
                                var hediff = HediffMaker.MakeHediff(hediffOption.hediff, pawn, bodyPartRecord) as HediffResource;
                                pawn.health.AddHediff(hediff);
                            }
                            var toRefill = Mathf.Min(num, resourceStorage.ResourceCapacity - resourceStorage.ResourceAmount);
                            if (resourceStorage.ResourceAmount < resourceStorage.ResourceCapacity)
                            {
                                resourceStorage.ResourceAmount += toRefill;
                            }
                        }
                        else
                        {
                            HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, num, hediffOption.addHediffIfMissing, hediffOption.applyToPart);
                        }
                    }
                }
            }
        }
    }
}
