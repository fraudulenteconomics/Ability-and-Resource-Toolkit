using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static Verse.AI.ReservationManager;

namespace ART
{
    public class CompProperties_ThingInUse : CompProperties
    {
        public List<UseProps> useProperties;
        public CompProperties_ThingInUse()
        {
            compClass = typeof(CompThingInUse);
        }
    }
    public class CompThingInUse : ThingComp, IAdjustResource
    {
        public static Dictionary<Thing, CompThingInUse> things = new Dictionary<Thing, CompThingInUse>();

        public static HashSet<StatDef> statsWithBoosters = new HashSet<StatDef> { };

        public CompPowerTrader compPower;
        public CompGlower compParentGlower;

        public bool powerIsOn;
        public bool UseIsEnabled(UseProps useProps)
        {
            int ind = Props.useProperties.IndexOf(useProps);
            if (useProps.toggleResourceUse && resourceUseToggleStates != null && resourceUseToggleStates.TryGetValue(ind, out bool state) && !state)
            {
                return false;
            }
            return true;
        }

        public virtual bool PawnCanUseIt(Pawn pawn, UseProps useProps)
        {
            return true;
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder(base.CompInspectStringExtra());
            var useProps = Props.useProperties;
            foreach (var useProp in useProps)
            {
                if (useProp.hediffRequired)
                {
                    sb.AppendLine("ART.RequiresResource".Translate(useProp.hediff.label));
                }
            }
            return sb.ToString().TrimEndNewlines();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                resourceUseToggleStates = new Dictionary<int, bool>();
                foreach (var useProps in Props.useProperties)
                {
                    int ind = Props.useProperties.IndexOf(useProps);
                    resourceUseToggleStates[ind] = useProps.defaultToggleState;
                }
            }
            things[parent] = this;
            foreach (var useProps in Props.useProperties)
            {
                if (useProps.statOffsets != null)
                {
                    foreach (var stat in useProps.statOffsets)
                    {
                        statsWithBoosters.Add(stat.stat);
                    }
                }

                if (useProps.statFactors != null)
                {
                    foreach (var stat in useProps.statFactors)
                    {
                        statsWithBoosters.Add(stat.stat);
                    }
                }
            }
            Register();
            var gameComp = ARTManager.Instance;
            gameComp.UpdateAdjuster(this);
            compPower = parent.TryGetComp<CompPowerTrader>();
            compParentGlower = parent.TryGetComp<CompGlower>();
            gameComp.RegisterFacilityInUse(this);
            boolValueCache = new BoolPawnsValueCache(InUseInt(out var claimants), claimants);
        }

        private BoolPawnsValueCache boolValueCache;
        public bool InUse(out IEnumerable<Pawn> claimants)
        {
            if (boolValueCache is null || Find.TickManager.TicksGame + 60 > boolValueCache.updateTick)
            {
                boolValueCache = new BoolPawnsValueCache(InUseInt(out var pawns), pawns);
            }
            claimants = boolValueCache.pawns;
            return boolValueCache.value;
        }
        private bool InUseInt(out IEnumerable<Pawn> claimants)
        {
            claimants = Claimants;
            if (claimants != null)
            {
                if (parent is Frame)
                {
                    foreach (var claimant in claimants)
                    {
                        if (!claimant.pather.MovingNow && claimant.CurJobDef == JobDefOf.FinishFrame
                            && claimant.CurJob.targetA.Thing == parent
                            && parent.OccupiedRect().Cells.Any(x => x.DistanceTo(claimant.Position) <= 1.5f))
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
                        if (pawnPosition == parent.Position || pawnPosition == parent.InteractionCell)
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
            if (parent is Frame)
            {
                foreach (var claimant in claimants)
                {
                    if (!claimant.pather.MovingNow && claimant.CurJobDef == JobDefOf.FinishFrame
                        && claimant.CurJob.targetA.Thing == parent
                        && parent.OccupiedRect().Cells.Any(x => x.DistanceTo(claimant.Position) <= 1.5f))
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
                    if (pawnPosition == parent.Position || pawnPosition == parent.InteractionCell)
                    {
                        yield return claimant;
                    }
                }
            }
        }
        private IEnumerable<Reservation> Reservations => parent.Map != null
            ? parent.Map.reservationManager.ReservationsReadOnly.Where(x => x.Target.Thing == parent)
            : Enumerable.Empty<Reservation>();

        private IEnumerable<Pawn> cachedClaimants;

        private int lastClaimantCacheTick;
        private IEnumerable<Pawn> Claimants
        {
            get
            {
                int curTicks = Find.TickManager.TicksGame;
                if (curTicks > lastClaimantCacheTick + 60 || cachedClaimants is null)
                {
                    cachedClaimants = Reservations.Select(x => x.Claimant);
                    lastClaimantCacheTick = curTicks;
                }
                return cachedClaimants;
            }
        }

        public CompProperties_ThingInUse Props => (CompProperties_ThingInUse)props;
        public List<ResourceProperties> ResourceSettings => null;
        public Dictionary<HediffResource, HediffResouceDisable> PostUseDelayTicks => null;
        public string DisablePostUse => null;
        public Thing Parent => parent;
        public Pawn PawnHost => null;
        public void ResourceTick()
        {
            bool inUse = InUse(out var claimaints);
            if (inUse)
            {
                var users = GetActualUsers(claimaints);
                foreach (var user in users)
                {
                    foreach (var useProps in Props.useProperties)
                    {
                        if (useProps.resourcePerSecond != -1f && UseIsEnabled(useProps))
                        {
                            float num = useProps.resourcePerSecond;
                            if (useProps.qualityScalesResourcePerSecond && parent.TryGetQuality(out var qc))
                            {
                                num *= Utils.GetQualityMultiplierInverted(qc);
                            }
                            Utils.AdjustResourceAmount(user, useProps.hediff, num, useProps.addHediffIfMissing, null, useProps.applyToPart);
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
            foreach (var useProps in Props.useProperties)
            {
                if (useProps.toggleResourceUse)
                {
                    int ind = Props.useProperties.IndexOf(useProps);
                    var toggle = new Command_Toggle
                    {
                        defaultLabel = useProps.toggleResourceLabel,
                        defaultDesc = useProps.toggleResourceDesc,
                        icon = ContentFinder<Texture2D>.Get(useProps.toggleResourceGizmoTexPath),
                        toggleAction = delegate ()
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
                        },
                        isActive = () => (!(resourceUseToggleStates is null) && !resourceUseToggleStates.ContainsKey(ind)) || resourceUseToggleStates[ind]
                    };
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
                    foreach (var useProps in Props.useProperties)
                    {
                        if (!changedGraphics && !useProps.texPathToggledOn.NullOrEmpty())
                        {
                            ChangeGraphic(useProps.texPathToggledOn);
                            changedGraphics = true;
                        }

                        if (!changedGlower && useProps.glowerOptions != null)
                        {
                            if (!useProps.glowOnlyPowered || (parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false))
                            {
                                UpdateGlower(useProps.glowerOptions);
                                changedGlower = true;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var useProps in Props.useProperties)
                    {
                        if (UseIsEnabled(useProps))
                        {
                            int ind = Props.useProperties.IndexOf(useProps);
                            if (!changedGraphics && !useProps.texPathToggledOn.NullOrEmpty() && resourceUseToggleStates.ContainsKey(ind) && resourceUseToggleStates[ind])
                            {
                                ChangeGraphic(useProps.texPathToggledOn);
                                changedGraphics = true;
                            }

                            if (!changedGlower && useProps.glowerOptions != null)
                            {
                                if (useProps.glowOnlyPowered && (parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false))
                                {
                                    UpdateGlower(useProps.glowerOptions);
                                    changedGlower = true;
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
                    if (compGlower != null)
                    {
                        base.parent.Map.glowGrid.DeRegisterGlower(compGlower);
                        compGlower = null;
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
            var graphicData = new GraphicData
            {
                graphicClass = parent.def.graphicData.graphicClass,
                texPath = texPath,
                shaderType = parent.def.graphicData.shaderType,
                drawSize = parent.def.graphicData.drawSize,
                color = parent.def.graphicData.color,
                colorTwo = parent.def.graphicData.colorTwo
            };
            var newGraphic = graphicData.GraphicColoredFor(parent);
            Traverse.Create(parent).Field("graphicInt").SetValue(newGraphic);
            base.parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);
        }

        public CompGlower compGlower;
        public void RemoveGlower()
        {
            if (compGlower != null)
            {
                base.parent.Map.glowGrid.DeRegisterGlower(compGlower);
                compGlower = null;
            }
            var parentGlower = parent.TryGetComp<CompGlower>();
            if (parentGlower != null)
            {
                base.parent.Map.glowGrid.DeRegisterGlower(parentGlower);
            }
        }
        public void UpdateGlower(GlowerOptions glowerOptions)
        {
            RemoveGlower();
            compGlower = new CompGlower
            {
                parent = parent
            };
            compGlower.Initialize(new CompProperties_Glower()
            {
                glowColor = glowerOptions.glowColor,
                glowRadius = glowerOptions.glowRadius,
                overlightRadius = glowerOptions.overlightRadius
            });
            base.parent.Map.mapDrawer.MapMeshDirty(base.parent.Position, MapMeshFlag.Things);
            base.parent.Map.glowGrid.RegisterGlower(compGlower);
        }

        public void Drop()
        {

        }

        public void Notify_Removed()
        {

        }

        public bool TryGetQuality(out QualityCategory qc)
        {
            return parent.TryGetQuality(out qc);
        }

        public void Register()
        {
            ARTManager.Instance.RegisterAdjuster(this);
        }

        public void Deregister()
        {
            ARTManager.Instance.DeregisterAdjuster(this);
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

        public ThingDef GetStuff()
        {
            return parent.Stuff;
        }

        public bool IsStorageFor(ResourceProperties resourceProperties, out ResourceStorage resourceStorages)
        {
            resourceStorages = null;
            return false;
        }
    }
}