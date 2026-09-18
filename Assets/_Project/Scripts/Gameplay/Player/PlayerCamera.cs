using UnityEngine;

namespace Rubber.Gameplay.Player
{
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField, Min(0.01f)] private float sensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 85f;
        private float yaw;
        private float pitch;
        public void Configure(Transform pivot) => cameraPivot = pivot;

        public void Look(Vector2 mouseDelta)
        {
            if (!cameraPivot) return;
            // Mouse delta already measures movement per frame; do not multiply by deltaTime.
            yaw += mouseDelta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - mouseDelta.y * sensitivity, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
