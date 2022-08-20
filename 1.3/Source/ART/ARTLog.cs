using Verse;

namespace ART
{
    public static class ARTLog
	{
		[TweakValue("0ART")] public static bool debug = false;
		public static void Message(string message)
		{
			if (debug) Verse.Log.Message(message);
		}

        public static void Error(string message)
        {
            if (debug) Verse.Log.Error(message);
        }
    }
}