using Rubber.Gameplay.Ducks;
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
        [SerializeField] private PlayerDuckCarrier duckCarrier;
        private InputActionAsset runtimeActions;
        private InputActionMap playerMap;
        private InputAction move, look, jump, interact, drop, releaseCursor, captureCursor;
        private bool captured;
        public bool PauseHandledExternally { get; set; }

        public void Configure(InputActionAsset actions, PlayerMovement motor, PlayerCamera cameraController)
        { inputActions = actions; movement = motor; playerCamera = cameraController; }

        private void Awake()
        {
            if (!movement) movement = GetComponent<PlayerMovement>();
            if (!playerCamera) playerCamera = GetComponent<PlayerCamera>();
            if (!interactionDetector) interactionDetector = GetComponent<PlayerInteractionDetector>();
            if (!interactionDetector)
                Debug.LogWarning("Add PlayerInteractionDetector to enable the Interact action.", this);
            if (!duckCarrier) duckCarrier = GetComponent<PlayerDuckCarrier>();
            if (!duckCarrier)
                Debug.LogWarning("Add PlayerDuckCarrier to enable picking up and dropping ducks.", this);
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
            drop = playerMap.FindAction("Drop", true);
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
            // Unity/the OS can release the cursor without a focus callback.
            // Keep our state aligned so the next click can capture it again.
            if (captured && Cursor.lockState != CursorLockMode.Locked)
                SetCapture(false);
            if (!PauseHandledExternally && releaseCursor.WasPressedThisFrame()) SetCapture(false);
            else if (!captured && captureCursor.WasPressedThisFrame())
            {
                SetCapture(true);
                return;
            }
            if (!captured || Cursor.lockState != CursorLockMode.Locked)
            { movement.ClearInput(); return; }
            // Refresh held input when focus/cursor capture returns.
            movement.Move(move.ReadValue<Vector2>());
            playerCamera.Look(look.ReadValue<Vector2>());
            if (interact.WasPressedThisFrame())
                TryInteract();
            if (drop.WasPressedThisFrame() && duckCarrier)
                duckCarrier.TryDrop();
        }

        private void OnMove(InputAction.CallbackContext context)
        { if (captured && Cursor.lockState == CursorLockMode.Locked) movement.Move(context.ReadValue<Vector2>()); }
        private void OnJump(InputAction.CallbackContext context)
        { if (captured && Cursor.lockState == CursorLockMode.Locked) movement.Jump(); }
        private void TryInteract()
        {
            bool worldInteractionExecuted = interactionDetector && interactionDetector.TryInteract();
            if (!worldInteractionExecuted && duckCarrier)
                duckCarrier.TryUseHeldDuckTrait();
        }
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
