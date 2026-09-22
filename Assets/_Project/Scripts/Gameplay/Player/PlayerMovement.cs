using UnityEngine;

namespace Rubber.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private PlayerStats stats;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private PlayerCamera playerCamera;
        private Vector2 moveInput;
        private bool jumpRequested;
        private float coyoteTimeRemaining;
        private Vector3 spawnPosition;
        public bool IsGrounded { get; private set; }

        public void Configure(Transform cameraTransform, PlayerStats playerStats)
        {
            view = cameraTransform;
            stats = playerStats;
        }
        public void Move(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }
        public void Jump() => jumpRequested = true;
        public void ClearInput()
        {
            moveInput = Vector2.zero;
            jumpRequested = false;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            playerCamera = GetComponent<PlayerCamera>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            spawnPosition = body.position;
            if (!stats)
            {
                Debug.LogError("Assign a PlayerStats asset to PlayerMovement.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            Vector3 bottom = body.position + capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius);
            IsGrounded = body.linearVelocity.y <= 0.1f &&
                Physics.SphereCast(bottom + Vector3.up * 0.05f, capsule.radius * 0.9f,
                    Vector3.down, out RaycastHit ground, 0.13f, stats.GroundMask, QueryTriggerInteraction.Ignore) &&
                Vector3.Angle(ground.normal, Vector3.up) <= stats.MaxSlopeAngle;
            if (IsGrounded)
                coyoteTimeRemaining = stats.CoyoteTime;
            else
                coyoteTimeRemaining = Mathf.Max(0f, coyoteTimeRemaining - Time.fixedDeltaTime);

            Vector3 forward = Vector3.ProjectOnPlane(view ? view.forward : transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = forward * moveInput.y + right * moveInput.x;
            Vector3 currentHorizontal = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 targetHorizontal = direction * stats.MoveSpeed;
            float speedChange = direction.sqrMagnitude > 0.001f ? stats.Acceleration : stats.Deceleration;
            // Stop on a ledge as soon as input is released instead of coasting off it.
            Vector3 horizontalVelocity = direction.sqrMagnitude <= 0.001f &&
                (IsGrounded || coyoteTimeRemaining > 0f)
                ? Vector3.zero
                : Vector3.MoveTowards(currentHorizontal, targetHorizontal,
                    speedChange * Time.fixedDeltaTime);
            Vector3 velocity = horizontalVelocity;
            velocity.y = body.linearVelocity.y;
            bool jumping = jumpRequested && (IsGrounded || coyoteTimeRemaining > 0f);
            if (jumping)
            {
                // Stronger upward gravity shortens the ascent. Increase the launch speed by the
                // same factor so Jump Height still represents the approximate peak height.
                velocity.y = Mathf.Sqrt(2f * Physics.gravity.magnitude *
                    stats.RisingGravityMultiplier * stats.JumpHeight);
                IsGrounded = false;
                coyoteTimeRemaining = 0f;
            }
            else if (!IsGrounded)
            {
                float gravityMultiplier = velocity.y > 0f
                    ? stats.RisingGravityMultiplier
                    : stats.FallingGravityMultiplier;
                // Rigidbody already applies normal gravity, so only add the remaining multiplier.
                velocity.y += Physics.gravity.y * (gravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
            jumpRequested = false;
            body.linearVelocity = velocity;
            if (IsGrounded && !jumping && direction.sqrMagnitude > 0.001f)
                TryStep(direction.normalized);

            if (body.position.y < -15f)
            {
                body.position = spawnPosition;
                body.linearVelocity = Vector3.zero;
                ClearInput();
                if (playerCamera) playerCamera.ResetStepSmoothing();
            }
        }

        private void TryStep(Vector3 direction)
        {
            Vector3 foot = body.position + capsule.center - Vector3.up * capsule.height * 0.5f;
            float reach = capsule.radius + stats.MoveSpeed * Time.fixedDeltaTime + 0.08f;
            if (!Physics.Raycast(foot + Vector3.up * 0.05f, direction, reach,
                    stats.GroundMask, QueryTriggerInteraction.Ignore))
                return;
            Vector3 probe = foot + direction * reach + Vector3.up * (stats.StepHeight + 0.05f);
            if (!Physics.Raycast(probe, Vector3.down, out RaycastHit top, stats.StepHeight,
                    stats.GroundMask, QueryTriggerInteraction.Ignore) ||
                Vector3.Angle(top.normal, Vector3.up) > stats.MaxSlopeAngle)
                return;
            float rise = top.point.y - foot.y + 0.015f;
            if (rise <= 0.02f || rise > stats.StepHeight + 0.02f) return;
            Vector3 offset = Vector3.up * rise + direction * stats.MoveSpeed * Time.fixedDeltaTime;
            Vector3 center = body.position + capsule.center;
            Vector3 lower = center - Vector3.up * (capsule.height * 0.5f - capsule.radius);
            Vector3 upper = center + Vector3.up * (capsule.height * 0.5f - capsule.radius);
            if (Physics.CapsuleCast(lower, upper, capsule.radius * 0.95f, Vector3.up, rise,
                    stats.GroundMask, QueryTriggerInteraction.Ignore)) return;
            if (Physics.CheckCapsule(lower + offset, upper + offset, capsule.radius * 0.95f,
                    stats.GroundMask, QueryTriggerInteraction.Ignore)) return;
            body.position += offset;
            if (playerCamera) playerCamera.SmoothStep(rise);
        }

        private void OnDisable() => ClearInput();
    }
}
