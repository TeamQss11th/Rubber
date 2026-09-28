namespace Rubber.Gameplay.Ducks.Traits
{
    public static class RubberDuckTraitFactory
    {
        public static IRubberDuckTrait Create(RubberDuckTraitType type)
        {
            return type switch
            {
                RubberDuckTraitType.None => null,
                _ => null
            };
        }
    }
}
