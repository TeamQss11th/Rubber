using Rubber.Gameplay.Interaction;
using UnityEngine;
namespace Rubber.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(VillaSlidingDoor))]
    public sealed class VillaDoorInteractable : MonoBehaviour, IInteractable
    {
        public bool TryInteract(GameObject interactor)
        {
            var door=GetComponent<VillaSlidingDoor>();
            // The door disables its Update loop while idle; Open/Toggle re-enables it.
            if(!interactor || !door || !door.gameObject.activeInHierarchy)return false;
            door.Toggle();return true;
        }
    }
}
