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
    public class HediffResourceSatisfyPolicy : IExposable
    {
        public FloatRange resourceSeekingThreshold;
        public bool seekingIsEnabled;
        public HediffResourceSatisfyPolicy()
        {

        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref resourceSeekingThreshold, "resourceSeekingThreshold");
            Scribe_Values.Look(ref seekingIsEnabled, "seekingIsEnabled");
        }
    }

    public class HediffResourcePolicy : IExposable
    {
        public HediffResourcePolicy()
        {

        }

        public Dictionary<HediffResourceDef, HediffResourceSatisfyPolicy> satisfyPolicies;
        public void ExposeData()
        {
            Scribe_Collections.Look(ref satisfyPolicies, "satisfyPolicies");
        }
    }

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
        public Dictionary<Pawn, HediffResourcePolicy> hediffResourcesPolicies = new Dictionary<Pawn, HediffResourcePolicy>();
        private List<IAdjustResource> resourceAdjusters = new List<IAdjustResource>();
        private List<IAdjustResource> resourceAdjustersToUpdate = new List<IAdjustResource>();
        public Dictionary<Thing, StatBonuses> thingsWithBonuses = new Dictionary<Thing, StatBonuses>();
        public bool dirtyUpdate;

        public static HediffResourceManager Instance;
        public HediffResourceManager(Game game)
        {
            Instance = this;
        }

        public void RegisterAdjuster(IAdjustResource adjuster)
        {
            if (!resourceAdjusters.Contains(adjuster))
            {
                resourceAdjusters.Add(adjuster);
            }
        }

        public void DeregisterAdjuster(IAdjustResource adjuster)
        {
            if (resourceAdjusters.Contains(adjuster))
            {
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
            Instance = this;
            if (resourceAdjusters is null) resourceAdjusters = new List<IAdjustResource>();
            if (thingsWithBonuses is null) thingsWithBonuses = new Dictionary<Thing, StatBonuses>();
            if (hediffResourcesPolicies is null) hediffResourcesPolicies = new Dictionary<Pawn, HediffResourcePolicy>();
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

        private List<CompFacilityInUse> facilities = new List<CompFacilityInUse>();

        public void RegisterFacilityInUse(CompFacilityInUse comp)
        {
            if (!facilities.Contains(comp))
            {
                facilities.Add(comp);
            }
        }

        public void RegisterAndRecheckForPolicies(Pawn pawn)
        {
            if (!this.hediffResourcesPolicies.TryGetValue(pawn, out var policy))
            {
                policy = new HediffResourcePolicy();
                policy.satisfyPolicies = new Dictionary<HediffResourceDef, HediffResourceSatisfyPolicy>();
                foreach (var hediffResourceDef in DefDatabase<HediffResourceDef>.AllDefs)
                {
                    policy.satisfyPolicies[hediffResourceDef] = new HediffResourceSatisfyPolicy();
                }
                HediffResourceManager.Instance.hediffResourcesPolicies[pawn] = policy;
            }
            else
            {
                foreach (var hediffResourceDef in DefDatabase<HediffResourceDef>.AllDefs)
                {
                    if (!policy.satisfyPolicies.ContainsKey(hediffResourceDef))
                    {
                        policy.satisfyPolicies[hediffResourceDef] = new HediffResourceSatisfyPolicy();
                    }
                }
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
            Scribe_Collections.Look(ref hediffResourcesPolicies, "hediffResourcesPolicies", LookMode.Reference, LookMode.Deep, ref pawnKeys, ref hediffResourcePolicyValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                PreInit();
            }
        }

        private List<Thing> thingKeys;
        private List<StatBonuses> thingStatsValues;

        private List<Pawn> pawnKeys;
        private List<HediffResourcePolicy> hediffResourcePolicyValues;

    }
}
