using Rubber.Gameplay.Player;
using UnityEngine;

namespace Rubber.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionDetector : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private Component currentTarget;

        public IInteractable CurrentInteractable => currentTarget as IInteractable;

        public void Configure(Camera camera, PlayerStats playerStats)
        {
            playerCamera = camera;
            stats = playerStats;
        }

        private void Awake()
        {
            if (!playerCamera) playerCamera = Camera.main;
            if (!playerCamera || !stats)
            {
                Debug.LogError("PlayerInteractionDetector requires Camera and PlayerStats.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Component detectedTarget = null;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (Physics.Raycast(ray, out RaycastHit hit, stats.InteractionDistance,
                    interactionLayers, QueryTriggerInteraction.Ignore))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable is Component component)
                    detectedTarget = component;
            }

            if (detectedTarget != currentTarget)
                SetTarget(detectedTarget);
        }

        private void SetTarget(Component target)
        {
            currentTarget = target;
            InteractionOutlineSelection.SetTarget(currentTarget);
        }

        private void ClearOutline()
        {
            currentTarget = null;
            InteractionOutlineSelection.Clear();
        }

        private void OnDisable() => ClearOutline();

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!playerCamera || !stats) return;
            Gizmos.color = Color.yellow;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * stats.InteractionDistance);
        }
#endif
    }
}
