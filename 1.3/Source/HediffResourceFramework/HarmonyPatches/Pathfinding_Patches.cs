using AnimalBehaviours;
using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) })]
    public static class PathFinder_FindPath_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var found = false;
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (!found && codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder lb && lb.LocalIndex == 53)
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 41);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 42);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 46);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PathFinder_FindPath_Patch).GetMethod(nameof(PathFinder_FindPath_Patch.ChangePathCostIfNeeded)));
                    yield return new CodeInstruction(OpCodes.Stloc_S, 46);
                }
            }
            if (!found)
            {
                Log.Error("PathFinder.FindPath Transpiler failed. The code won't work.");
            }
        }

        static public int ChangePathCostIfNeeded(Pawn pawn, int xCell, int zCell, int cost)
        {
            var cell = new IntVec3(xCell, 0, zCell);
            if (pawn.IgnoresTerrain() && pawn.CanPassOver(cell))
            {
                return pawn.GetPawnBasePathCost(cell);
            }
            return cost;
        }

        public static bool IgnoresTerrain(this Pawn pawn)
        {
            if (pawn.health?.hediffSet?.hediffs != null)
            {
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff is HediffResource hediffResource && hediffResource.def.ignoreTerrain)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static int GetPawnBasePathCost(this Pawn pawn, IntVec3 c)
        {
            if (c.x == pawn.Position.x || c.z == pawn.Position.z)
            {
                return pawn.TicksPerMoveCardinal;
            }
            return pawn.TicksPerMoveDiagonal;
        }

        public static bool CanPassOver(this Pawn pawn, IntVec3 c)
        {
            List<Thing> list = pawn.Map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing.def.passability == Traversability.Impassable)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[] { typeof(Pawn), typeof(IntVec3) })]
    public static class Pawn_PathFollower_CostToMoveIntoCell_Patch
    {
        public static void Postfix(Pawn pawn, IntVec3 c, ref int __result)
        {
            if (pawn.Map != null && pawn.IgnoresTerrain() && pawn.CanPassOver(c))
            {
                __result = pawn.GetPawnBasePathCost(c);
            }
        }
    }
}
