using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace FraudeconCode
{
	public class Verb_SpawnFaction : Verb_CastBase
	{
		protected override bool TryCastShot()
		{
			if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
			{
				return false;
			}
			var thing = GenSpawn.Spawn(verbProps.spawnDef, currentTarget.Cell, caster.Map);
			thing.SetFaction(this.Caster.Faction);
			if (verbProps.colonyWideTaleDef != null)
			{
				Pawn pawn = caster.Map.mapPawns.FreeColonistsSpawned.RandomElementWithFallback();
				TaleRecorder.RecordTale(verbProps.colonyWideTaleDef, caster, pawn);
			}
			base.ReloadableCompSource?.UsedOnce();
			return true;
		}
	}
}
