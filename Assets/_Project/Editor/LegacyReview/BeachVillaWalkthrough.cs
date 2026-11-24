using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubber.World
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class BeachVillaWalkthrough : MonoBehaviour
    {
        [Serializable] public struct Checkpoint { public string label; public Vector3 feet; public float yaw; }
        public Transform view;
        public Checkpoint[] checkpoints;
        CharacterController body;
        float pitch, verticalSpeed;
        int current;
        bool showHelp = true;
        const string Help = "BEACH VILLA / LIGHTING REVIEW\nClick: look | WASD: walk | Shift: faster | Esc: release mouse\nF1-F11: checkpoints | R: reset | N: day/night | F: flashlight | H: help";

        void Awake() { body = GetComponent<CharacterController>(); }
        void OnDisable() { ReleaseMouse(); }
        void OnApplicationFocus(bool focused) { if (!focused) ReleaseMouse(); }
        static void ReleaseMouse() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        public void GoTo(int index)
        {
            if (checkpoints == null || index < 0 || index >= checkpoints.Length) return;
            if (!body) body = GetComponent<CharacterController>();
            current = index;
            body.enabled = false;
            transform.SetPositionAndRotation(checkpoints[index].feet, Quaternion.Euler(0, checkpoints[index].yaw, 0));
            pitch = verticalSpeed = 0;
            view.localRotation = Quaternion.identity;
            body.enabled = true;
        }
        void Update()
        {
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (!view || !Application.isFocused) return;
            if (keys != null)
            {
                if (keys.escapeKey.wasPressedThisFrame) ReleaseMouse();
                if (keys.hKey.wasPressedThisFrame) showHelp = !showHelp;
                for (int i = 0; i < Mathf.Min(11, checkpoints.Length); i++)
                    if (keys[(Key)((int)Key.F1 + i)].wasPressedThisFrame) GoTo(i);
                if (keys.rKey.wasPressedThisFrame) GoTo(current);
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && (keys == null || !keys.escapeKey.wasPressedThisFrame))
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            Vector2 input = Vector2.zero;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (mouse != null)
                {
                    var look = mouse.delta.ReadValue() * .09f;
                    transform.Rotate(0, look.x, 0);
                    pitch = Mathf.Clamp(pitch - look.y, -80, 80);
                    view.localRotation = Quaternion.Euler(pitch, 0, 0);
                }
                if (keys != null) input = Vector2.ClampMagnitude(new Vector2((keys.dKey.isPressed?1:0)-(keys.aKey.isPressed?1:0), (keys.wKey.isPressed?1:0)-(keys.sKey.isPressed?1:0)), 1);
            }
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (body.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(verticalSpeed + Physics.gravity.y * dt, -30);
            float speed = keys != null && keys.leftShiftKey.isPressed ? 4 : 2.6f;
            body.Move(((transform.right * input.x + transform.forward * input.y) * speed + Vector3.up * verticalSpeed) * dt);
            if (transform.position.y < -10) GoTo(current);
        }
        void OnGUI()
        {
            if (!showHelp) return;
            GUI.Box(new Rect(16,16,530,78), Help);
            if (checkpoints != null && checkpoints.Length > 0) GUI.Box(new Rect(16,98,530,26), checkpoints[current].label);
        }
    }
}
