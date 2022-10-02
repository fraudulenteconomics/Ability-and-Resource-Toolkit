using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ART
{
	public class JobDriver_RefuelResource : JobDriver
	{
		public Thing Fuel => job.targetA.Thing;
		public int RefuelTime
		{
			get
			{

				foreach (var hediffResource in Utils.HediffResourcesRefuelable(pawn, Fuel.def))
				{
					return hediffResource.def.refuelHediff.refuelTime;
				}
				throw new Exception("Couldn't find wait period");
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOn(() => Utils.HediffResourcesRefuelable(pawn, Fuel.def).Any() is false);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			var toil = Toils_General.Wait(RefuelTime);
			toil.WithProgressBarToilDelay(TargetIndex.A);
			toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			yield return toil;
			var refuel = new Toil
			{
				initAction = delegate
				{
					var fuel = Fuel;
					var hediff = Utils.HediffResourcesRefuelable(pawn, Fuel.def).FirstOrDefault();
					Utils.RefuelHediff(fuel, hediff);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return refuel;
		}
	}
}