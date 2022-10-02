namespace ART
{
    public class HediffResource_AbilityLink : HediffResource
    {
        public override bool ShouldRemove
        {
            get
            {
                if (ability.pawn is null)
                {
                    return true;
                }
                var compAbilities = ability.Comp;
                if (compAbilities is null || compAbilities.LearnedAbilities.Contains(ability) is false)
                {
                    return true;
                }
                return base.ShouldRemove;
            }
        }
    }
}
