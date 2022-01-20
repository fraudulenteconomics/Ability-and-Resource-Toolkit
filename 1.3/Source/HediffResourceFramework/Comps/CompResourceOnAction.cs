using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking.Types;
using Verse;

namespace HediffResourceFramework
{
    public class ResourceOnAction
    {
        public HediffDef hediffApplied;
        public float adjustSeverity;
        public float adjustResource;
        public bool onSelf;
        public float applyChance = 1f;
        public void TryApplyOn(Pawn pawn)
        {
            Log.Message("Applying on " + pawn);
            if (Rand.Chance(applyChance))
            {
                if (hediffApplied is HediffResourceDef def)
                {
                    HediffResourceUtils.AdjustResourceAmount(pawn, def, adjustResource, true, null);
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, hediffApplied, adjustSeverity);
                }
            }
        }
    }
    public class CompProperties_ResourceOnAction : CompProperties
    {
        public List<ResourceOnAction> resourcesOnAction;
        public CompProperties_ResourceOnAction()
        {
            this.compClass = typeof(CompResourceOnAction);
        }
    }
    public class CompResourceOnAction : ThingComp
    {
        public CompProperties_ResourceOnAction Props => base.props as CompProperties_ResourceOnAction;
    }
}
