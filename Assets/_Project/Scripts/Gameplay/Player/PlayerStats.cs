using UnityEngine;

namespace Rubber.Gameplay.Player
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Rubber/Player/Player Stats")]
    public sealed class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
        [SerializeField, Min(0.1f)] private float acceleration = 20f;
        [SerializeField, Min(0.1f)] private float deceleration = 50f;

        [Header("Jump")]
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.2f;
        [SerializeField, Range(0f, 0.5f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(1f)] private float risingGravityMultiplier = 2f;
        [SerializeField, Min(1f)] private float fallingGravityMultiplier = 2.5f;

        [Header("Ground")]
        [SerializeField, Range(0f, 0.5f)] private float stepHeight = 0.3f;
        [SerializeField, Range(0f, 80f)] private float maxSlopeAngle = 50f;
        [SerializeField] private LayerMask groundMask = ~4;

        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float JumpHeight => jumpHeight;
        public float CoyoteTime => coyoteTime;
        public float RisingGravityMultiplier => risingGravityMultiplier;
        public float FallingGravityMultiplier => fallingGravityMultiplier;
        public float StepHeight => stepHeight;
        public float MaxSlopeAngle => maxSlopeAngle;
        public LayerMask GroundMask => groundMask;
    }
}
