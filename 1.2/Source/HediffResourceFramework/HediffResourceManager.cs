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
        private List<CompAdjustHediffs> compHediffResources = new List<CompAdjustHediffs>();
        public HediffResourceManager(Game game)
        {

        }

        public void RegisterComp(CompAdjustHediffs comp)
        {
            Log.Message("Registering: " + comp);
            if (!compHediffResources.Contains(comp))
            {
                compHediffResources.Add(comp);
            }
        }

        private void PreInit()
        {
            if (compHediffResources is null)
            {
                compHediffResources = new List<CompAdjustHediffs>();
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

            for (int num = compHediffResources.Count - 1; num >= 0; num--)
            {
                var comp = compHediffResources[num];
                if (!comp.parent.Destroyed)
                {
                    comp.ResourceTick();
                }
                else
                {
                    compHediffResources.RemoveAt(num);
                }
            }
        }
    }
}
