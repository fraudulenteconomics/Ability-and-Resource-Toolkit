using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class HediffResourceManager : GameComponent
    {
        private List<IAdjustResource> resourceAdjusters = new List<IAdjustResource>();

        public HediffResourceManager(Game game)
        {

        }

        public void RegisterAdjuster(IAdjustResource adjuster)
        {
            if (!resourceAdjusters.Contains(adjuster))
            {
                HRFLog.Message("Registering: " + adjuster);
                resourceAdjusters.Add(adjuster);
            }
        }
        public void DeregisterAdjuster(IAdjustResource adjuster)
        {
            if (resourceAdjusters.Contains(adjuster))
            {
                HRFLog.Message("Deregistering: " + adjuster);
                resourceAdjusters.Remove(adjuster);
            }
        }
        private void PreInit()
        {
            if (resourceAdjusters is null)
            {
                resourceAdjusters = new List<IAdjustResource>();
            }
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            PreInit();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            PreInit();
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();

            for (int num = resourceAdjusters.Count - 1; num >= 0; num--)
            {
                var adjuster = resourceAdjusters[num];
                var parent = adjuster.Parent;
                if (parent != null && !parent.Destroyed && parent.IsHashIntervalTick(60))
                {
                    adjuster.ResourceTick();
                }
                else
                {
                    resourceAdjusters.RemoveAt(num);
                }
            }
        }
    }
}
