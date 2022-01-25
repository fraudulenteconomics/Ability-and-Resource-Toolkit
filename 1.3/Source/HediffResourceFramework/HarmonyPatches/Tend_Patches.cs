using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{

    [HarmonyPatch(typeof(HealthAIUtility), "FindBestMedicine")]
    public static class Patch_FindBestMedicine
    {
        private static bool Prefix(Pawn healer, Pawn patient, bool onlyUseInventory = false)
        {
            if (healer.HasEnoughResourceToTend(out _))
            {
                Log.Message("Has enough resource to tend");
                return false;
            }
            return true;
        }

        public static bool HasEnoughResourceToTend(this Pawn healer, out (HediffResource, TendProperties) toConsume)
        {
            foreach (var hediff in healer.health.hediffSet.hediffs)
            {
                if (hediff is HediffResource hediffResource && hediffResource.def.tendProperties != null)
                {
                    var hediffSource = hediffResource.def.tendProperties.hediffResource != null
                        ? healer.health.hediffSet.GetFirstHediffOfDef(hediffResource.def.tendProperties.hediffResource) as HediffResource
                        : hediffResource;

                    if (hediffSource != null)
                    {
                        bool canTend = hediffSource.ResourceAmount >= hediffResource.def.tendProperties.resourceOnTend;
                        if (canTend)
                        {
                            toConsume = (hediffSource, hediffResource.def.tendProperties);
                            return true;
                        }
                    }
                }
            }
            toConsume = default;
            return false;
        }
    }

    [HarmonyPatch(typeof(TendUtility), "GetOptimalHediffsToTendWithSingleTreatment")]
    public static class Patch_GetOptimalHediffsToTendWithSingleTreatment
    {
        private static void Prefix(Pawn patient, ref bool usingMedicine, List<Hediff> outHediffsToTend, List<Hediff> tendableHediffsInTendPriorityOrder = null)
        {
            if (TendUtility_DoTend_Patch.healer != null && TendUtility_DoTend_Patch.healer.HasEnoughResourceToTend(out _))
            {
                usingMedicine = true;
            }
        }
    }

    [HarmonyPatch(typeof(TendUtility), "DoTend")]
    public static class TendUtility_DoTend_Patch
    {
        public static Pawn healer;
        private static void Prefix(Pawn doctor)
        {
            healer = doctor;
        }
        private static void Postfix()
        {
            healer = null;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].opcode == OpCodes.Stloc_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TendUtility_DoTend_Patch), nameof(DoTendingEffect)));
                }
            }
        }

        public static void DoTendingEffect(Pawn doctor, Pawn patient, ref float quality, ref float maxQuality)
        {
            if (doctor.HasEnoughResourceToTend(out var data))
            {
                quality = CalculateBaseTendQuality(doctor, patient, data.Item2);
                maxQuality = data.Item2.statBases.First(x => x.stat == StatDefOf.MedicalQualityMax).value;
                data.Item1.ResourceAmount -= data.Item2.resourceOnTend;
            }
        }

        public static float CalculateBaseTendQuality(Pawn doctor, Pawn patient, TendProperties tendEffect)
        {
            float medicinePotency = tendEffect.statBases.First(x => x.stat == StatDefOf.MedicalPotency).value;
            float medicineQualityMax = tendEffect.statBases.First(x => x.stat == StatDefOf.MedicalQualityMax).value;
            return TendUtility.CalculateBaseTendQuality(doctor, patient, medicinePotency, medicineQualityMax);
        }
    }


}