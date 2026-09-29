using UnityEngine;

namespace Rubber.Gameplay.Ducks.Traits
{
    public sealed class TestDuckTrait : IRubberDuckTrait
    {
        private const float TestInterval = 1f;

        private RubberDuckTraitTestProbe probe;

        public RubberDuckTraitExecution Init(RubberDuckTraitContext context)
        {
            probe = context.Duck.GetComponent<RubberDuckTraitTestProbe>();
            if (!probe)
                probe = context.Duck.gameObject.AddComponent<RubberDuckTraitTestProbe>();

            probe.RecordInitialization();
            return new RubberDuckTraitExecution(true, TestInterval);
        }

        public TraitInteractionResult OnInteract(GameObject interactor)
        {
            probe.RecordInteraction(interactor);
            return TraitInteractionResult.Consumed;
        }

        public void OnInterval()
        {
            probe.RecordInterval();
        }
    }
}
