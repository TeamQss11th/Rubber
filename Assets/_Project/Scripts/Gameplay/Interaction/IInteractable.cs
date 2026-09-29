using UnityEngine;

namespace Rubber.Gameplay.Interaction
{
    /// <summary>Performs an interaction requested by a player.</summary>
    public interface IInteractable
    {
        bool TryInteract(GameObject interactor);
    }
}
