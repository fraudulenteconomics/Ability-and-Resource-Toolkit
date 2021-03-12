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
	public class HediffResourceFrameworkMod : Mod
	{
		public HediffResourceFrameworkMod(ModContentPack content) : base(content)
		{
			Harmony harmony = new Harmony("Fraudecon.HediffResourceFramework");
			harmony.PatchAll();
		}
	}
}
