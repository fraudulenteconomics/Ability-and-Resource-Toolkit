using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ART
{
	public class JobDriver_MaintainResourceBuilding : JobDriver
	{
		private const int MaintainTicks = 180;
		private CompMaintainableResourceBuilding compMaintainable;
		public CompMaintainableResourceBuilding CompMaintainable
		{
			get
			{
				if (compMaintainable == null)
				{
					compMaintainable = TargetA.Thing.TryGetComp<CompMaintainableResourceBuilding>();
				}
				return compMaintainable;
			}
		}
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOn(() => !CompMaintainable.CanMaintain(pawn, out string failReason));
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			var toil = Toils_General.Wait(180);
			toil.WithProgressBarToilDelay(TargetIndex.A);
			toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			yield return toil;
			var maintain = new Toil
			{
				initAction = delegate
			{
				CompMaintainable.Maintained(pawn);
			},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return maintain;
		}
	}
}