namespace Rubber.Gameplay.Ducks.Traits
{
    public readonly struct RubberDuckTraitExecution
    {
        public bool UsesInteraction { get; }
        public float Interval { get; }

        public RubberDuckTraitExecution(bool usesInteraction, float interval)
        {
            UsesInteraction = usesInteraction;
            Interval = interval;
        }
    }
}
