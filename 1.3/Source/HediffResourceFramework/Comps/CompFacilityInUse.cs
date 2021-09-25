using HarmonyLib;
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
    public class GlowerOptions
    {
        public ColorInt glowColor;
        public float glowRadius;
        public float overlightRadius;
    }
    public class StatBooster
    {
        public HediffResourceDef hediff;
        public bool preventUseIfHediffMissing;
        public string cannotUseMessageKey;

        public bool toggleResourceUse;
        public string toggleResourceGizmoTexPath;
        public string toggleResourceLabel;
        public string toggleResourceDesc;
        public string texPathToggledOn;
        public GlowerOptions glowerOptions;
        public bool glowOnlyPowered;
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
        public bool defaultToggleState;
    }

    public class CompProperties_FacilityInUse_StatBoosters : CompProperties
    {
        public List<StatBooster> statBoosters;
        public CompProperties_FacilityInUse_StatBoosters()
        {
            this.compClass = typeof(CompFacilityInUse);
        }
    }
    public class CompFacilityInUse : ThingComp, IAdjustResource
    {
        public static Dictionary<Thing, CompFacilityInUse> thingBoosters = new Dictionary<Thing, CompFacilityInUse>();

        public static HashSet<StatDef> statsWithBoosters = new HashSet<StatDef> { };

        public CompPowerTrader compPower;
        public CompGlower compParentGlower;

        public bool powerIsOn;
        public bool StatBoosterIsEnabled(StatBooster statBooster)
        {
            var ind = this.Props.statBoosters.IndexOf(statBooster);
            if (statBooster.toggleResourceUse && resourceUseToggleStates != null && resourceUseToggleStates.TryGetValue(ind, out bool state) && !state)
            {
                return false;
            }
            return true;
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                resourceUseToggleStates = new Dictionary<int, bool>();
                foreach (var statBooster in Props.statBoosters)
                {
                    var ind = Props.statBoosters.IndexOf(statBooster);
                    resourceUseToggleStates[ind] = statBooster.defaultToggleState;
                }
            }
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
            var gameComp = HediffResourceUtils.HediffResourceManager;
            gameComp.UpdateAdjuster(this);
            this.compPower = this.parent.TryGetComp<CompPowerTrader>();
            this.compParentGlower = this.parent.TryGetComp<CompGlower>();
            gameComp.RegisterFacilityInUse(this);
            boolValueCache = new BoolPawnsValueCache(InUseInt(out IEnumerable<Pawn> claimants), claimants);
        }

        private BoolPawnsValueCache boolValueCache;

        public bool InUse(out IEnumerable<Pawn> claimants)
        {
            if (Find.TickManager.TicksGame + 60 > boolValueCache.updateTick)
            {
                boolValueCache.Value = InUseInt(out IEnumerable<Pawn> pawns);
                boolValueCache.pawns = pawns;
            }
            claimants = boolValueCache.pawns;
            return boolValueCache.value;
        }
        private bool InUseInt(out IEnumerable<Pawn> claimants)
        {
            claimants = Claimants;
            if (claimants != null)
            {
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
                if (curTicks > lastClaimantCacheTick + 60 || cachedClaimants is null)
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
                        if (resourceUseToggleStates.ContainsKey(ind))
                        {
                            resourceUseToggleStates[ind] = !resourceUseToggleStates[ind];
                        }
                        else
                        {
                            resourceUseToggleStates[ind] = false;
                        }
                        UpdateGraphics();
                    };
                    toggle.isActive = (() => resourceUseToggleStates is null || resourceUseToggleStates.ContainsKey(ind) ? resourceUseToggleStates[ind] : true);
                    yield return toggle;
                }
            }
        }

        public void UpdateGraphics()
        {
            bool changedGraphics = false;
            bool changedGlower = false;
            if (base.parent.Map != null)
            {
                if (resourceUseToggleStates is null)
                {
                    foreach (var statBooster in Props.statBoosters)
                    {
                        if (!changedGraphics && !statBooster.texPathToggledOn.NullOrEmpty())
                        {
                            ChangeGraphic(statBooster.texPathToggledOn);
                            changedGraphics = true;
                        }

                        if (!changedGlower && statBooster.glowerOptions != null)
                        {
                            if (!statBooster.glowOnlyPowered || (this.parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false))
                            {
                                UpdateGlower(statBooster.glowerOptions);
                                changedGlower = true;
                            }
                            else
                            {
                                Log.Message("Can't update glower");
                            }
                        }
                    }
                }
                else
                {
                    foreach (var statBooster in Props.statBoosters)
                    {
                        if (StatBoosterIsEnabled(statBooster))
                        {
                            var ind = Props.statBoosters.IndexOf(statBooster);
                            if (!changedGraphics && !statBooster.texPathToggledOn.NullOrEmpty() && resourceUseToggleStates.ContainsKey(ind) && resourceUseToggleStates[ind])
                            {
                                ChangeGraphic(statBooster.texPathToggledOn);
                                changedGraphics = true;
                            }

                            if (!changedGlower && statBooster.glowerOptions != null)
                            {
                                if (statBooster.glowOnlyPowered && (this.parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false))
                                {
                                    UpdateGlower(statBooster.glowerOptions);
                                    changedGlower = true;
                                }
                                else
                                {
                                    Log.Message("Can't update glower");
                                }
                            }
                        }
                    }
                }

                if (!changedGraphics && (!parent.def.graphicData?.texPath.NullOrEmpty() ?? false))
                {
                    ChangeGraphic(parent.def.graphicData.texPath);
                }

                if (!changedGlower)
                {
                    Log.Message("Updating glower: " + this);
                    if (this.compGlower != null)
                    {
                        base.parent.Map.glowGrid.DeRegisterGlower(this.compGlower);
                        this.compGlower = null;
                    }

                    if (compParentGlower != null)
                    {
                        base.parent.Map.glowGrid.RegisterGlower(compParentGlower);
                    }
                }
            }
        }

        public void ChangeGraphic(string texPath)
        {
            var graphicData = new GraphicData();
            graphicData.graphicClass = this.parent.def.graphicData.graphicClass;
            graphicData.texPath = texPath;
            graphicData.shaderType = this.parent.def.graphicData.shaderType;
            graphicData.drawSize = this.parent.def.graphicData.drawSize;
            graphicData.color = this.parent.def.graphicData.color;
            graphicData.colorTwo = this.parent.def.graphicData.colorTwo;

            var newGraphic = graphicData.GraphicColoredFor(this.parent);
            Traverse.Create(this.parent).Field("graphicInt").SetValue(newGraphic);
            base.parent.Map.mapDrawer.MapMeshDirty(this.parent.Position, MapMeshFlag.Things);
        }

        public CompGlower compGlower;
        public void RemoveGlower()
        {
            if (this.compGlower != null)
            {
                base.parent.Map.glowGrid.DeRegisterGlower(this.compGlower);
                this.compGlower = null;
            }
            var parentGlower = this.parent.TryGetComp<CompGlower>();
            if (parentGlower != null)
            {
                base.parent.Map.glowGrid.DeRegisterGlower(parentGlower);
            }
        }
        public void UpdateGlower(GlowerOptions glowerOptions)
        {
            RemoveGlower();
            this.compGlower = new CompGlower();
            this.compGlower.parent = this.parent;
            this.compGlower.Initialize(new CompProperties_Glower()
            {
                glowColor = glowerOptions.glowColor,
                glowRadius = glowerOptions.glowRadius,
                overlightRadius = glowerOptions.overlightRadius
            });
            base.parent.Map.mapDrawer.MapMeshDirty(base.parent.Position, MapMeshFlag.Things);
            base.parent.Map.glowGrid.RegisterGlower(this.compGlower);
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

        public void Update()
        {
            UpdateGraphics();
        }
    }
}