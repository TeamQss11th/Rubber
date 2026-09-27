using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RubberDuckReturnZone : MonoBehaviour
    {
        [SerializeField] private RubberDuckReturnRegistry registry;

        public void Configure(RubberDuckReturnRegistry returnRegistry) => registry = returnRegistry;

        private void Reset()
        {
            Collider zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        private void Awake()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (!zoneCollider.isTrigger)
                Debug.LogError("RubberDuckReturnZone requires a trigger collider.", this);
            if (!registry)
                Debug.LogError("RubberDuckReturnZone requires RubberDuckReturnRegistry.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!registry || !other) return;

            RubberDuckInteractable duck = other.GetComponentInParent<RubberDuckInteractable>();
            if (!duck || duck.IsHeld) return;

            registry.TryRegister(duck);
        }
    }
}
