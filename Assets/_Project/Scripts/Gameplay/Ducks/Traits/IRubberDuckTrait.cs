using UnityEngine;

namespace Rubber.Gameplay.Ducks.Traits
{
    public interface IRubberDuckTrait
    {
        RubberDuckTraitExecution Init(RubberDuckTraitContext context);
        TraitInteractionResult OnInteract(GameObject interactor);
        void OnInterval();
    }
}
