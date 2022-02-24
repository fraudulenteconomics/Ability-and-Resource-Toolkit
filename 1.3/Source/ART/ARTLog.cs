using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ART
{
	public static class ARTLog
	{
		private static bool enabled = true;
		public static void Message(string message)
        {
			if (enabled)
            {
				Log.Message(message);
            }
        }
	}
}