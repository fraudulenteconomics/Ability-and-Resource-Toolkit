using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class AddHumanlikeOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            List<Thing> thingList = c.GetThingList(pawn.Map);
            foreach (var apparel in thingList.OfType<Apparel>())
            {
                TaggedString toCheck = "ForceWear".Translate(apparel.LabelCap, apparel);
                FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                (toCheck));
                if (floatMenuOption != null && !HediffResourceUtils.CanWear(pawn, apparel, out string reason))
                {
                    opts.Remove(floatMenuOption);
                    var newOption = new FloatMenuOption("HRF.CannotWear".Translate(apparel.def.label) + " (" + reason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
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
                        TaggedString toCheck = "Equip".Translate(equipment.LabelShort);
                        FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains
                        (toCheck));
                        if (floatMenuOption != null && !HediffResourceUtils.CanEquip(pawn, equipment, out string reason))
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
                    FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains(text));
                    if (floatMenuOption != null && !HediffResourceUtils.CanDrink(pawn, t, out string reason))
                    {
                        floatMenuOption.Label += "HRF.DrinkWarning".Translate();
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders_NewTmp")]
    public static class AddJobGiverWorkOrders_NewTmp_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool drafted)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            List<Thing> thingList = c.GetThingList(pawn.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                var t = thingList[i];
                if (CompFacilityInUse_StatBoosters.thingBoosters.TryGetValue(t, out var comp))
                {
                    foreach (var statBooster in comp.Props.statBoosters)
                    {
                        if (comp.StatBoosterIsEnabled(statBooster) && statBooster.preventUseIfHediffMissing)
                        {
                            var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(statBooster.hediff) as HediffResource;
                            if (hediffResource is null || !hediffResource.CanApplyStatBooster(statBooster))
                            {
                                FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.ToLower().Contains(t.Label.ToLower()));
                                if (floatMenuOption != null)
                                {
                                    floatMenuOption.action = null;
                                    floatMenuOption.Label = statBooster.cannotUseMessageKey.Translate(t);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
