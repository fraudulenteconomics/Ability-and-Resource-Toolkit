using System.Collections.Generic;
using Verse;

namespace ART
{
    public class MaterialReplace
    {
        public TerrainDef floorDef;
        public ThingDef replaceWithThingDef;
        public ThingDef replaceWithStuffDef;
    }
    public class BuildingSpawnOptions : DefModExtension
    {
        public List<MaterialReplace> materialReplaces;
    }
}
