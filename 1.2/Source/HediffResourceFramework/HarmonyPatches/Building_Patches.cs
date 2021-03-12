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
	[HarmonyPatch(typeof(Building), "SpawnSetup")]
	internal static class SpawnSetup_Patch
	{
		private static void Postfix(Building __instance)
		{
			var options = __instance.def.GetModExtension<BuildingSpawnOptions>();
			if (options != null)
            {
				if (options.materialReplaces != null)
                {
					var terrainDef = __instance.Position.GetTerrain(__instance.Map);
					foreach (var materialReplace in options.materialReplaces)
                    {
						if (materialReplace.floorDef == terrainDef)
                        {
							if (__instance.def != materialReplace.replaceWithThingDef)
                            {
								var map = __instance.Map;
								var position = __instance.Position;
								var rotation = __instance.Rotation;
								var stuff = materialReplace.replaceWithStuffDef != null ? materialReplace.replaceWithStuffDef : GenStuff.DefaultStuffFor(materialReplace.replaceWithThingDef);
								var newBuilding = ThingMaker.MakeThing(materialReplace.replaceWithThingDef, stuff);
								__instance.Destroy();
								GenPlace.TryPlaceThing(newBuilding, position, map, ThingPlaceMode.Direct, null, null, rotation);
							}
							else if (__instance.Stuff != null && materialReplace.replaceWithStuffDef != null && __instance.Stuff != materialReplace.replaceWithStuffDef)
                            {
								var map = __instance.Map;
								var position = __instance.Position;
								var rotation = __instance.Rotation;
								var stuff = materialReplace.replaceWithStuffDef;
								var newBuilding = ThingMaker.MakeThing(__instance.def, stuff);
								__instance.Destroy();
								GenPlace.TryPlaceThing(newBuilding, position, map, ThingPlaceMode.Direct, null, null, rotation);
							}
                        }
                    }
                }
			}
		}
	}
}
