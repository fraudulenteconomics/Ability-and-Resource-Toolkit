using PipeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ART
{
	public class JobDriver_ExtractResourceFromNetwork : JobDriver
	{
		public Thing PipeBuilding => job.targetA.Thing;

		public CompConvertToThing CompConvertToThing => PipeBuilding.TryGetComp<CompConvertToThing>();
		public int InteractionTime
		{
			get
			{
				foreach (var hediffResource in Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToThing.Props.thing, (HediffResource x) => x.ResourceAmount < x.ResourceCapacity))
				{
					return (int)(hediffResource.def.pipeInteraction.pipeInteractionPerResource * (hediffResource.ResourceCapacity - hediffResource.ResourceAmount));
				}
				throw new Exception("Couldn't find wait period");
			}
		}

		public override string GetReport()
		{
			foreach (var hediffResource in Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToThing.Props.thing, (HediffResource x) => x.ResourceAmount < x.ResourceCapacity))
			{
				return "ART.Extracting".Translate(hediffResource.def.label, "ART.Network".Translate(CompConvertToThing.Props.pipeNet.resource.name.UncapitalizeFirst()));
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
			this.FailOn(() => Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToThing.Props.thing, (HediffResource x) => x.ResourceAmount < x.ResourceCapacity).Any() is false);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			var toil = Toils_General.Wait(InteractionTime);
			toil.WithProgressBarToilDelay(TargetIndex.A);
			toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			toil.tickAction = delegate
			{
				var hediff = Utils.HediffResourcesInteractableWithPipes(pawn, CompConvertToThing.Props.thing, (HediffResource x) => x.ResourceAmount < x.ResourceCapacity).FirstOrDefault();
				if (Find.TickManager.TicksGame % hediff.def.pipeInteraction.pipeInteractionPerResource == 0)
				{
					int num = 1 / CompConvertToThing.Props.ratio;
					CompConvertToThing.PipeNet.DistributeAmongStorage(1);
					hediff.ChangeResourceAmount(num);
				}
			};
			yield return toil;
		}
	}
}