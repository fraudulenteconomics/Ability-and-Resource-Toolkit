using HarmonyLib;
using PipeSystem;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ART
{

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class AddHumanlikeOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            var c = IntVec3.FromVector3(clickPos);
            var thingList = c.GetThingList(pawn.Map);
            foreach (var apparel in thingList.OfType<Apparel>())
            {
                var toCheck = "ForceWear".Translate(apparel.LabelCap, apparel);
                var floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                (toCheck));
                if (floatMenuOption != null && !Utils.CanWear(pawn, apparel, out string reason))
                {
                    opts.Remove(floatMenuOption);
                    var newOption = new FloatMenuOption("ART.CannotWear".Translate(apparel.def.label) + " (" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    opts.Add(newOption);
                }
            }

            if (pawn.equipment != null)
            {
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i].TryGetComp<CompEquippable>() != null)
                    {
                        var equipment = (ThingWithComps)thingList[i];
                        var toCheck = "Equip".Translate(equipment.LabelShort);
                        var floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                        (toCheck));
                        if (floatMenuOption != null && !Utils.CanEquip(pawn, equipment, out string reason))
                        {
                            opts.Remove(floatMenuOption);
                            var newOption = new FloatMenuOption("CannotEquip".Translate(equipment.LabelShort) + " (" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                            opts.Add(newOption);
                        }
                    }
                }
            }

            for (int i = 0; i < thingList.Count; i++)
            {
                var t = thingList[i];
                if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                {
                    string text = (!t.def.ingestible.ingestCommandString.NullOrEmpty()) ? string.Format(t.def.ingestible.ingestCommandString, t.LabelShort) : ((string)"ConsumeThing".Translate(t.LabelShort, t));
                    var floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains(text));
                    if (floatMenuOption != null && !Utils.CanDrink(pawn, t, out string reason, out bool preventFromUsage))
                    {
                        floatMenuOption.Label += ": " + reason;
                        if (preventFromUsage)
                        {
                            floatMenuOption.action = null;
                        }
                    }
                }

                var compConvertToResource = t.TryGetComp<CompConvertToResource>();
                if (compConvertToResource != null)
                {
                    foreach (var hediffResource in Utils.HediffResourcesInteractableWithPipes(pawn, compConvertToResource.Props.thing, (HediffResource x) => x.ResourceAmount > 0))
                    {
                        opts.Add(new FloatMenuOption("ART.Fill".Translate("ART.Network".Translate(compConvertToResource.Props.pipeNet.resource.name.UncapitalizeFirst()), hediffResource.def.label), delegate
                        {
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(ART_DefOf.ART_FillResourceToNetwork, t));
                        }));
                    }
                }
                var compConvertToThing = t.TryGetComp<CompConvertToThing>();
                if (compConvertToThing != null)
                {
                    foreach (var hediffResource in Utils.HediffResourcesInteractableWithPipes(pawn, compConvertToThing.Props.thing, (HediffResource x) => x.ResourceAmount < x.ResourceCapacity))
                    {
                        opts.Add(new FloatMenuOption("ART.Extract".Translate(hediffResource.def.label, "ART.Network".Translate(compConvertToThing.Props.pipeNet.resource.name.UncapitalizeFirst())), delegate
                        {
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(ART_DefOf.ART_ExtractResourceFromNetwork, t));
                        }));
                    }
                }

                foreach (var hediffResource in Utils.HediffResourcesRefuelable(pawn, t.def))
                {
                    opts.Add(new FloatMenuOption("ART.Refuel".Translate(hediffResource.def.label, t.def.label), delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(ART_DefOf.ART_RefuelResource, t));
                    }));
                }
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders")]
    public static class AddJobGiverWorkOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool drafted)
        {
            var c = IntVec3.FromVector3(clickPos);
            var thingList = c.GetThingList(pawn.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                var t = thingList[i];
                if (!pawn.CanUseIt(t, out string cannotUseMessage))
                {
                    var floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.ToLower().Contains(t.Label.ToLower()));
                    if (floatMenuOption?.action != null)
                    {
                        floatMenuOption.action = null;
                        floatMenuOption.Label = cannotUseMessage;
                    }
                }
            }
        }
    }
}
