using UnityEngine;

namespace Rubber.Gameplay.Player
{
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField, Min(0.01f)] private float sensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 85f;
        [SerializeField, Min(0.01f)] private float stepSmoothTime = 0.1f;
        private float yaw;
        private float pitch;
        private Vector3 cameraLocalPosition;
        private float stepOffset;
        private float stepOffsetVelocity;
        public void Configure(Transform pivot) => cameraPivot = pivot;

        private void Awake()
        {
            if (cameraPivot) cameraLocalPosition = cameraPivot.localPosition;
        }

        public void Look(Vector2 mouseDelta)
        {
            if (!cameraPivot) return;
            // Mouse delta already measures movement per frame; do not multiply by deltaTime.
            yaw += mouseDelta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - mouseDelta.y * sensitivity, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public void SmoothStep(float height)
        {
            if (!cameraPivot || height <= 0f) return;
            stepOffset -= height;
            cameraPivot.localPosition = cameraLocalPosition + Vector3.up * stepOffset;
        }

        public void ResetStepSmoothing()
        {
            stepOffset = 0f;
            stepOffsetVelocity = 0f;
            if (cameraPivot) cameraPivot.localPosition = cameraLocalPosition;
        }

        private void LateUpdate()
        {
            if (!cameraPivot || stepOffset == 0f) return;
            stepOffset = Mathf.SmoothDamp(stepOffset, 0f, ref stepOffsetVelocity,
                stepSmoothTime, Mathf.Infinity, Time.deltaTime);
            if (Mathf.Abs(stepOffset) < 0.001f)
            {
                stepOffset = 0f;
                stepOffsetVelocity = 0f;
            }
            cameraPivot.localPosition = cameraLocalPosition + Vector3.up * stepOffset;
        }

        private void OnDisable() => ResetStepSmoothing();
    }
}
