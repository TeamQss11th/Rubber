using Rubber.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubber.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerCamera))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private PlayerInteractionDetector interactionDetector;
        private InputActionAsset runtimeActions;
        private InputActionMap playerMap;
        private InputAction move, look, jump, interact, releaseCursor, captureCursor;
        private bool captured;

        public void Configure(InputActionAsset actions, PlayerMovement motor, PlayerCamera cameraController)
        { inputActions = actions; movement = motor; playerCamera = cameraController; }

        private void Awake()
        {
            if (!movement) movement = GetComponent<PlayerMovement>();
            if (!playerCamera) playerCamera = GetComponent<PlayerCamera>();
            if (!interactionDetector) interactionDetector = GetComponent<PlayerInteractionDetector>();
            if (!interactionDetector)
                Debug.LogWarning("Add PlayerInteractionDetector to enable the Interact action.", this);
            if (!inputActions)
            {
                Debug.LogError("Assign PlayerControls.inputactions to PlayerInputReader.", this);
                enabled = false;
                return;
            }
            runtimeActions = Instantiate(inputActions);
            playerMap = runtimeActions.FindActionMap("Player", true);
            move = playerMap.FindAction("Move", true);
            look = playerMap.FindAction("Look", true);
            jump = playerMap.FindAction("Jump", true);
            interact = playerMap.FindAction("Interact", true);
            releaseCursor = playerMap.FindAction("ReleaseCursor", true);
            captureCursor = playerMap.FindAction("CaptureCursor", true);
        }

        private void OnEnable()
        {
            if (playerMap == null) return;
            move.performed += OnMove;
            move.canceled += OnMove;
            jump.performed += OnJump;
            playerMap.Enable();
            SetCapture(true);
        }

        private void Update()
        {
            if (releaseCursor.WasPressedThisFrame()) SetCapture(false);
            else if (!captured && captureCursor.WasPressedThisFrame()) SetCapture(true);
            if (!captured || Cursor.lockState != CursorLockMode.Locked)
            { movement.ClearInput(); return; }
            // Refresh held input when focus/cursor capture returns.
            movement.Move(move.ReadValue<Vector2>());
            playerCamera.Look(look.ReadValue<Vector2>());
            if (interact.WasPressedThisFrame() && interactionDetector)
                interactionDetector.TryInteract();
        }

        private void OnMove(InputAction.CallbackContext context)
        { if (captured) movement.Move(context.ReadValue<Vector2>()); }
        private void OnJump(InputAction.CallbackContext context)
        { if (captured && Cursor.lockState == CursorLockMode.Locked) movement.Jump(); }
        private void SetCapture(bool value)
        {
            captured = value;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
            if (!value) movement.ClearInput();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) SetCapture(false); }
        private void OnDisable()
        {
            if (playerMap != null)
            {
                move.performed -= OnMove;
                move.canceled -= OnMove;
                jump.performed -= OnJump;
                playerMap.Disable();
            }
            SetCapture(false);
        }
        private void OnDestroy() { if (runtimeActions) Destroy(runtimeActions); }
    }
}
