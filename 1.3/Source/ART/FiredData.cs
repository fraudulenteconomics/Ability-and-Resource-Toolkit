using Verse;

namespace ART
{
    public class FiredData : IExposable
    {
        public Thing equipment;
        public Thing caster;
        public void ExposeData()
        {
            Scribe_References.Look(ref equipment, "launcher");
            Scribe_References.Look(ref caster, "caster");
        }
    }
}
