using HarmonyLib;
using RimWorld;
using Verse;

namespace ART
{
    [HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing")]
    public static class Patch_NewFrameDef_Thing
    {
        private static void Postfix(ref ThingDef __result, ref ThingDef def)
        {
            var options = def.GetModExtension<FacilityInProgress>();
            if (options != null)
            {
                var props = new CompProperties_ThingInUse
                {
                    useProperties = options.useProperties
                };
                __result.comps.Add(props);
            }
        }
    }
}
