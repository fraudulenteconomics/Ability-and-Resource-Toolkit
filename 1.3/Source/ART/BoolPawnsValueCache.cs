using System.Collections.Generic;
using Verse;

namespace ART
{

    public class BoolPawnsValueCache
    {
        public BoolPawnsValueCache(bool value, IEnumerable<Pawn> pawns)
        {
            this.value = value;
            this.pawns = pawns;
        }
        public bool value;

        public IEnumerable<Pawn> pawns;
        public bool Value
        {
            get => value;
            set
            {
                this.value = value;
                updateTick = Find.TickManager.TicksGame;
            }
        }
        public int updateTick;
    }
}