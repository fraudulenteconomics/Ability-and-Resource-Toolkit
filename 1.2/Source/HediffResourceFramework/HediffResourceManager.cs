using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace HediffResourceFramework
{
    public class StatBonus : IExposable
    {
        public StatDef stat;
        public float value;
        public float statOffset;
        public float statFactor;
        public StatBonus()
        {

        }

        public StatBonus(StatDef stat)
        {
            this.stat = stat;
        }
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stat", xmlRoot.Name);
            value = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref stat, "stat");
            Scribe_Values.Look(ref value, "value");
            Scribe_Values.Look(ref statOffset, "statOffset");
            Scribe_Values.Look(ref statFactor, "statFactor");
        }
    }
    public class StatBonuses : IExposable
    {
        public Dictionary<StatDef, StatBonus> statBonuses;
        public StatBonuses()
        {

        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref statBonuses, "statBonuses", LookMode.Def, LookMode.Deep, ref statDefsKeys, ref statBonusesValues);
        }

        private List<StatDef> statDefsKeys;
        private List<StatBonus> statBonusesValues;
    }
    public class HediffResourceManager : GameComponent
    {
        private List<IAdjustResource> resourceAdjusters = new List<IAdjustResource>();
        private List<IAdjustResource> resourceAdjustersToUpdate = new List<IAdjustResource>();
        public Dictionary<Thing, StatBonuses> thingsWithBonuses = new Dictionary<Thing, StatBonuses>();
        public bool dirtyUpdate;
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

        public void UpdateAdjuster(IAdjustResource adjuster)
        {
            if (!resourceAdjustersToUpdate.Contains(adjuster))
            {
                HRFLog.Message("Update: " + adjuster);
                resourceAdjustersToUpdate.Add(adjuster);
                dirtyUpdate = true;
            }
        }
        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            if (dirtyUpdate)
            {
                foreach (var adjuster in resourceAdjustersToUpdate)
                {
                    adjuster.Update();
                }
                resourceAdjustersToUpdate.Clear();
                dirtyUpdate = false;
            }
        }
        private void PreInit()
        {
            if (resourceAdjusters is null) resourceAdjusters = new List<IAdjustResource>();
            if (thingsWithBonuses is null) thingsWithBonuses = new Dictionary<Thing, StatBonuses>();
        }

        public override void LoadedGame()
        {
            PreInit();
            base.LoadedGame();
        }

        public override void StartedNewGame()
        {
            PreInit();
            base.StartedNewGame();
        }

        private List<CompFacilityInUse_StatBoosters> facilities = new List<CompFacilityInUse_StatBoosters>();

        public void RegisterFacilityInUse(CompFacilityInUse_StatBoosters comp)
        {
            if (!facilities.Contains(comp))
            {
                facilities.Add(comp);
            }
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            for (int num = resourceAdjusters.Count - 1; num >= 0; num--)
            {
                var adjuster = resourceAdjusters[num];
                var parent = adjuster.Parent;
                if (parent != null && !parent.Destroyed)
                {
                    if (parent.IsHashIntervalTick(60))
                    {
                        adjuster.ResourceTick();
                    }
                }
                else
                {
                    resourceAdjusters.RemoveAt(num);
                }
            }
            for (int num = facilities.Count - 1; num >= 0; num--)
            {
                var facility = facilities[num];
                if (facility.compPower != null)
                {
                    if (facility.compGlower is null && !facility.powerIsOn && facility.compPower.PowerOn)
                    {
                        facility.powerIsOn = true;
                        Log.Message("1 update");
                        facility.UpdateGraphics();
                    }
                    else if (facility.compGlower != null && facility.powerIsOn && !facility.compPower.PowerOn)
                    {
                        facility.powerIsOn = false;
                        Log.Message("2 update");
                        facility.UpdateGraphics();
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref thingsWithBonuses, "thingsWithBonuses", LookMode.Reference, LookMode.Deep, ref thingKeys, ref thingStatsValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (thingsWithBonuses is null)
                {
                    thingsWithBonuses = new Dictionary<Thing, StatBonuses>();
                }
            }
        }

        private List<Thing> thingKeys;
        private List<StatBonuses> thingStatsValues;
    }
}
