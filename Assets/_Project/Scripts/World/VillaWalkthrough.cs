using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubber.World
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class VillaWalkthrough : MonoBehaviour
    {
        public Transform view;
        private CharacterController body;
        private float pitch;
        private float verticalSpeed;

        private void Awake() { body = GetComponent<CharacterController>(); }
        private void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void Update()
        {
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys == null || mouse == null) return;
            if (keys.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (mouse.leftButton.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (Cursor.lockState != CursorLockMode.Locked) return;
            var look = mouse.delta.ReadValue() * .09f;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -80, 80);
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
            var input = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);
            if (body.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            var move = (transform.right * input.x + transform.forward * input.y) * 2.6f;
            body.Move((move + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
        private void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 490, 54), "RUBBER / VILLA LIGHTING STUDY\nClick: look | WASD: walk | Esc: cursor | F1/2/3: day/dusk/night | F4: cycle");
        }
    }
}
