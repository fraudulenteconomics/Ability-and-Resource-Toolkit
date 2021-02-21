using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Verse.AI.ReservationManager;

namespace HediffResourceFramework
{

    //[StaticConstructorOnStartup]
    //public static class StatBoostersPatch
    //{
    //    static StatBoostersPatch()
    //    {
    //        foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
    //        {
    //            if (thingDef.IsBed || thingDef.IsWorkTable)
    //            {
    //                var compProps = new CompProperties_FacilityInUse_StatBoosters();
    //                compProps.statBoosters = new List<StatBooster>();
    //                foreach (var hediffResource in DefDatabase<HediffResourceDef>.AllDefs)
    //                {
    //                    var statBooster = new StatBooster();
    //                    statBooster.hediff = hediffResource;
    //                    statBooster.resourcePerSecond = -5;
    //                    statBooster.qualityScalesResourcePerSecond = true;
    //                    statBooster.addHediffIfMissing = true;
    //                    statBooster.statOffsets = new List<StatModifier>()
    //                    {
    //                        new StatModifier
    //                        {
    //                            stat = StatDefOf.BedRestEffectiveness,
    //                            value = 2f
    //                        },
    //                        new StatModifier
    //                        {
    //                            stat = StatDefOf.WorkTableEfficiencyFactor,
    //                            value = 2f
    //                        },
    //                        new StatModifier
    //                        {
    //                            stat = StatDefOf.WorkTableWorkSpeedFactor,
    //                            value = 2f
    //                        },
    //                    };
    //                    statBooster.statFactors = new List<StatModifier>()
    //                    {
    //                        new StatModifier
    //                        {
    //                            stat = StatDefOf.BedRestEffectiveness,
    //                            value = 2f
    //                        },
    //                        new StatModifier
    //                        {
    //                            stat = StatDefOf.WorkTableEfficiencyFactor,
    //                            value = 2f
    //                        },
    //                        new StatModifier
    //                        {
    //                            stat = StatDefOf.WorkTableWorkSpeedFactor,
    //                            value = 2f
    //                        },
    //                    };
    //                    compProps.statBoosters.Add(statBooster);
    //                }
    //                thingDef.comps.Add(compProps);
    //            }
    //        }
    //    }
    //}
    public class StatBooster
    {
        public HediffResourceDef hediff;
        public float resourcePerSecond;
        public bool addHediffIfMissing;
        public bool qualityScalesResourcePerSecond;
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;
    }
    public class CompProperties_FacilityInUse_StatBoosters : CompProperties
    {
        public List<StatBooster> statBoosters;
        public CompProperties_FacilityInUse_StatBoosters()
        {
            this.compClass = typeof(CompFacilityInUse_StatBoosters);
        }
    }
    public class CompFacilityInUse_StatBoosters : ThingComp, IAdjustResource
    {
        public static Dictionary<Thing, CompFacilityInUse_StatBoosters> thingBoosters = new Dictionary<Thing, CompFacilityInUse_StatBoosters>();

        public static HashSet<StatDef> statsWithBoosters = new HashSet<StatDef> { };
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            thingBoosters[this.parent] = this;

            foreach (var statBooster in Props.statBoosters)
            {
                if (statBooster.statOffsets != null)
                {
                    foreach (var stat in statBooster.statOffsets)
                    {
                        statsWithBoosters.Add(stat.stat);
                    }
                }

                if (statBooster.statFactors != null)
                {
                    foreach (var stat in statBooster.statFactors)
                    {
                        statsWithBoosters.Add(stat.stat);
                    }
                }
            }

            Register();
        }
        public bool InUse(out IEnumerable<Pawn> claimants)
        {
            claimants = Claimants;
            foreach (var claimant in claimants)
            {
                var pawnPosition = claimant.Position;
                if (pawnPosition == this.parent.Position || pawnPosition == this.parent.InteractionCell)
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<Pawn> GetActualUsers(IEnumerable<Pawn> claimaints)
        {
            foreach (var claimant in claimaints)
            {
                var pawnPosition = claimant.Position;

                if (pawnPosition == this.parent.Position || pawnPosition == this.parent.InteractionCell)
                {
                    yield return claimant;
                }
            }
        }
        private IEnumerable<Reservation> Reservations => this.parent.Map.reservationManager.ReservationsReadOnly.Where(x => x.Target.Thing == this.parent);

        private IEnumerable<Pawn> cachedClaimants;

        private int lastClaimantCacheTick;
        private IEnumerable<Pawn> Claimants
        {
            get
            {
                var curTicks = Find.TickManager.TicksGame;
                if (curTicks > lastClaimantCacheTick + 60)
                {
                    cachedClaimants = Reservations.Select(x => x.Claimant);
                    lastClaimantCacheTick = curTicks;
                }
                return cachedClaimants;
            }
        }
            
        public CompProperties_FacilityInUse_StatBoosters Props => (CompProperties_FacilityInUse_StatBoosters)this.props;
        public List<HediffOption> ResourceSettings => throw new NotImplementedException();
        public Dictionary<HediffResource, HediffResouceDisable> PostUseDelayTicks => throw new NotImplementedException();
        public string DisablePostUse => throw new NotImplementedException();
        public Thing Parent => this.parent;
        public void ResourceTick()
        {
            bool inUse = InUse(out var claimaints);
            if (inUse)
            {
                var users = GetActualUsers(claimaints);
                foreach (var user in users)
                {
                    foreach (var statBooster in Props.statBoosters)
                    {
                        float num = statBooster.resourcePerSecond;
                        if (statBooster.qualityScalesResourcePerSecond && this.parent.TryGetQuality(out QualityCategory qc))
                        {
                            num *= HediffResourceUtils.GetQualityMultiplier(qc);
                        }
                        HediffResourceUtils.AdjustResourceAmount(user, statBooster.hediff, num, statBooster.addHediffIfMissing);
                    }
                }
            }
            Log.Message($"{this.parent} - in use: {inUse}, claimants: {string.Join(", ", claimaints)}, users: {string.Join(", ", GetActualUsers(claimaints))}");
        }
        public void Drop()
        {
            throw new NotImplementedException();
        }

        public void Notify_Removed()
        {
            throw new NotImplementedException();
        }

        public bool TryGetQuality(out QualityCategory qc)
        {
            return this.parent.TryGetQuality(out qc);
        }


        public void Register()
        {
            var gameComp = Current.Game.GetComponent<HediffResourceManager>();
            gameComp.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            var gameComp = Current.Game.GetComponent<HediffResourceManager>();
            gameComp.DeregisterAdjuster(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Register();
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            Register();
        }
    }
}
