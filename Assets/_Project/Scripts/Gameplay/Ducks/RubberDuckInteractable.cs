using Rubber.Gameplay.Interaction;
using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RubberDuckInteractable : MonoBehaviour, IInteractable
    {
        private Rigidbody body;
        private Collider[] duckColliders;
        private bool[] colliderWasEnabled;
        private PlayerDuckCarrier carrier;

        public bool IsHeld => carrier;

        private void Awake() => body = GetComponent<Rigidbody>();

        public bool TryInteract(GameObject interactor)
        {
            if (!interactor || carrier) return false;
            PlayerDuckCarrier playerCarrier = interactor.GetComponent<PlayerDuckCarrier>();
            return playerCarrier && playerCarrier.TryPickUp(this);
        }

        internal bool BeginCarry(PlayerDuckCarrier newCarrier, Transform anchor)
        {
            if (carrier || !newCarrier || !anchor) return false;
            if (!body) body = GetComponent<Rigidbody>();

            duckColliders = GetComponentsInChildren<Collider>(true);
            colliderWasEnabled = new bool[duckColliders.Length];
            for (int i = 0; i < duckColliders.Length; i++)
            {
                Collider duckCollider = duckColliders[i];
                colliderWasEnabled[i] = duckCollider.enabled;
                duckCollider.enabled = false;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            transform.SetParent(anchor, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            carrier = newCarrier;
            return true;
        }

        internal void ReleaseAt(Vector3 position, Quaternion rotation)
        {
            if (!carrier) return;

            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            for (int i = 0; i < duckColliders.Length; i++)
                if (duckColliders[i]) duckColliders[i].enabled = colliderWasEnabled[i];

            carrier = null;
        }

        private void OnDestroy()
        {
            if (carrier) carrier.Forget(this);
        }
    }
}
