using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [DisallowMultipleComponent]
    public sealed class PlayerDuckCarrier : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private Vector3 heldLocalPosition = new(0.35f, -0.3f, 0.9f);
        [SerializeField] private Vector3 heldLocalEulerAngles = new(0f, -20f, 0f);
        [SerializeField, Min(0.5f)] private float dropDistance = 1.2f;
        [SerializeField] private LayerMask dropSurfaceLayers = Physics.DefaultRaycastLayers;

        private Transform holdAnchor;
        private RubberDuckInteractable heldDuck;

        public RubberDuckInteractable HeldDuck => heldDuck;
        public bool IsHoldingDuck => heldDuck;

        public void Configure(Transform cameraTransform) => view = cameraTransform;

        private void Awake()
        {
            if (!view)
            {
                Camera playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera) view = playerCamera.transform;
            }
            if (!view)
            {
                Debug.LogError("PlayerDuckCarrier requires the player's camera transform.", this);
                enabled = false;
                return;
            }

            var anchorObject = new GameObject("Held Duck Anchor");
            holdAnchor = anchorObject.transform;
            holdAnchor.SetParent(view, false);
            holdAnchor.localPosition = heldLocalPosition;
            holdAnchor.localRotation = Quaternion.Euler(heldLocalEulerAngles);
        }

        public bool TryPickUp(RubberDuckInteractable duck)
        {
            if (!isActiveAndEnabled || !holdAnchor || !duck || heldDuck || !duck.BeginCarry(this, holdAnchor))
                return false;

            heldDuck = duck;
            return true;
        }

        public bool TryDrop()
        {
            if (!isActiveAndEnabled || !heldDuck) return false;

            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            forward.Normalize();

            float placementDistance = dropDistance;
            Vector3 castOrigin = view.position - Vector3.up * 0.35f;
            if (Physics.SphereCast(castOrigin, 0.2f, forward, out RaycastHit obstruction,
                    dropDistance, dropSurfaceLayers, QueryTriggerInteraction.Ignore))
            {
                placementDistance = obstruction.distance - 0.1f;
                if (placementDistance < 0.6f) return false;
            }

            Vector3 dropPosition = transform.position + forward * placementDistance;
            dropPosition.y = view.position.y - 0.35f;

            RubberDuckInteractable duck = heldDuck;
            heldDuck = null;
            duck.ReleaseAt(dropPosition, Quaternion.LookRotation(forward, Vector3.up));
            return true;
        }

        internal void Forget(RubberDuckInteractable duck)
        {
            if (heldDuck == duck) heldDuck = null;
        }
    }
}
