using PipeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ART
{
	public class JobDriver_FillResourceToNetwork : JobDriver
	{
		public Thing PipeBuilding => job.targetA.Thing;

		public CompConvertToResource CompConvertToResource => PipeBuilding.TryGetComp<CompConvertToResource>();
		public int InteractionTime
		{
			get
			{
				foreach (var hediffResource in Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToResource.Props.thing, (HediffResource x) => x.ResourceAmount > 0))
				{
					return (int)(hediffResource.def.pipeInteraction.pipeInteractionPerResource * hediffResource.ResourceAmount);
				}
				throw new Exception("Couldn't find wait period");
			}
		}
		public override string GetReport()
		{
			foreach (var hediffResource in Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToResource.Props.thing, (HediffResource x) => x.ResourceAmount > 0))
			{
				return "ART.Filling".Translate("ART.Network".Translate(CompConvertToResource.Props.pipeNet.resource.name.UncapitalizeFirst()), hediffResource.def.label);
			}
			return base.GetReport();
		}
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOn(() => Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToResource.Props.thing, (HediffResource x) => x.ResourceAmount > 0).Any() is false);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			var toil = Toils_General.Wait(InteractionTime);
			toil.WithProgressBarToilDelay(TargetIndex.A);
			toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			toil.tickAction = delegate
			{
				var hediff = Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToResource.Props.thing, (HediffResource x) => x.ResourceAmount > 0).FirstOrDefault();
				if (Find.TickManager.TicksGame % hediff.def.pipeInteraction.pipeInteractionPerResource == 0)
				{
					int num = 1 / CompConvertToResource.Props.ratio;
					CompConvertToResource.PipeNet.DistributeAmongStorage(num);
					hediff.ChangeResourceAmount(-1);
				}
			};
			yield return toil;
		}
	}
}