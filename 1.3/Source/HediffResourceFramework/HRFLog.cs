using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HediffResourceFramework
{
	public static class HRFLog
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