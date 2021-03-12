using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Verse.AI.ReservationManager;

namespace HediffResourceFramework
{
    public class StatBooster
    {
        public HediffResourceDef hediff;
        public bool preventUseIfHediffMissing;
        public string cannotUseMessageKey;

        public bool toggleResourceUse;
        public string toggleResourceGizmoTexPath;
        public string toggleResourceLabel;
        public string toggleResourceDesc;

        public float resourcePerSecond = -1f;
        public float resourceOnComplete = -1f;
        public BodyPartDef applyToPart;
        public bool addHediffIfMissing;
        public bool qualityScalesResourcePerSecond;
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;

        public int increaseQuality = -1;
        public QualityCategory increaseQualityCeiling = QualityCategory.Legendary;

        public List<StatBonus> outputStatOffsets;
        public List<StatBonus> outputStatFactors;
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

        public bool StatBoosterIsEnabled(StatBooster statBooster)
        {
            var ind = this.Props.statBoosters.IndexOf(statBooster);
            if (resourceUseToggleStates != null && resourceUseToggleStates.TryGetValue(ind, out bool state) && !state)
            {
                return false;
            }
            return true;
        }
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
            if (this.parent is Frame)
            {
                foreach (var claimant in claimants)
                {
                    if (!claimant.pather.MovingNow && claimant.CurJobDef == JobDefOf.FinishFrame 
                        && claimant.CurJob.targetA.Thing == this.parent
                        && this.parent.OccupiedRect().Cells.Any(x => x.DistanceTo(claimant.Position) <= 1.5f))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (var claimant in claimants)
                {
                    var pawnPosition = claimant.Position;
                    if (pawnPosition == this.parent.Position || pawnPosition == this.parent.InteractionCell)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerable<Pawn> GetActualUsers(IEnumerable<Pawn> claimants)
        {
            if (this.parent is Frame)
            {
                foreach (var claimant in claimants)
                {
                    if (!claimant.pather.MovingNow && claimant.CurJobDef == JobDefOf.FinishFrame
                        && claimant.CurJob.targetA.Thing == this.parent
                        && this.parent.OccupiedRect().Cells.Any(x => x.DistanceTo(claimant.Position) <= 1.5f))
                    {
                        yield return claimant;
                    }
                }
            }
            else
            {
                foreach (var claimant in claimants)
                {
                    var pawnPosition = claimant.Position;
                    if (pawnPosition == this.parent.Position || pawnPosition == this.parent.InteractionCell)
                    {
                        yield return claimant;
                    }
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
            Log.Message(this + ", inUse: " + inUse + " - claimaints: " + claimaints.Count());
            if (inUse)
            {
                var users = GetActualUsers(claimaints);
                foreach (var user in users)
                {
                    foreach (var statBooster in Props.statBoosters)
                    {
                        if (statBooster.resourcePerSecond != -1f && this.StatBoosterIsEnabled(statBooster))
                        {
                            float num = statBooster.resourcePerSecond;
                            if (statBooster.qualityScalesResourcePerSecond && this.parent.TryGetQuality(out QualityCategory qc))
                            {
                                num *= HediffResourceUtils.GetQualityMultiplierInverted(qc);
                            }
                            HediffResourceUtils.AdjustResourceAmount(user, statBooster.hediff, num, statBooster.addHediffIfMissing, statBooster.applyToPart);
                        }

                    }
                }
            }
        }

        public Dictionary<int, bool> resourceUseToggleStates = new Dictionary<int, bool>();
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            foreach (var statBooster in Props.statBoosters)
            {
                if (statBooster.toggleResourceUse)
                {
                    var ind = Props.statBoosters.IndexOf(statBooster);
                    var toggle = new Command_Toggle();
                    toggle.defaultLabel = statBooster.toggleResourceLabel;
                    toggle.defaultDesc = statBooster.toggleResourceDesc;
                    toggle.icon = ContentFinder<Texture2D>.Get(statBooster.toggleResourceGizmoTexPath);
                    toggle.toggleAction = delegate ()
                    {
                        if (resourceUseToggleStates is null) resourceUseToggleStates = new Dictionary<int, bool>();
                        if (resourceUseToggleStates.ContainsKey(ind))
                        {
                            resourceUseToggleStates[ind] = !resourceUseToggleStates[ind];
                        }
                        else
                        {
                            resourceUseToggleStates[ind] = false;
                        }
                    };
                    toggle.isActive = (() => resourceUseToggleStates is null || resourceUseToggleStates.ContainsKey(ind) ? resourceUseToggleStates[ind] : true);
                    yield return toggle;
                }
            }
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
            var gameComp = HediffResourceUtils.HediffResourceManager;
            gameComp.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            var gameComp = HediffResourceUtils.HediffResourceManager;
            gameComp.DeregisterAdjuster(this);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Deregister();
            base.PostDestroy(mode, previousMap);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref resourceUseToggleStates, "resourceUseStates", LookMode.Value, LookMode.Value, ref intKeys, ref boolValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Register();
            }
        }

        private List<int> intKeys;
        private List<bool> boolValues;
        public override void PostPostMake()
        {
            base.PostPostMake();
            Register();
        }
    }
}
