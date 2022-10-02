using HarmonyLib;
using Verse;

namespace FraudeconCode
{
    public class VerbMod : Mod
    {
        public VerbMod(ModContentPack content) : base(content)
        {
            var harm = new Harmony("fradulenteconomics.verbs");
            GraveblossomHelpers.DoPatches(harm);
            IndestructibleHediffs.DoPatches(harm);
        }
    }
}