using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Rubber.Gameplay.Player.Editor
{
    // Runs against the test scene in Play mode; never saves simulated positions.
    [InitializeOnLoad]
    public static class PlayerMovementSmokeCheck
    {
        private const string Request = "Temp/RubberPlayerCheck.request";
        private const string Pending = "Rubber.PlayerSmokeCheck";
        static PlayerMovementSmokeCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(Request);
                Run();
            };
        }
        [MenuItem("Rubber/Check Player Test Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
                "Assets/_Project/Scenes/Test/PlayerTestScene.unity")
            { Debug.LogWarning("Open PlayerTestScene before running its checks."); return; }
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                EditorApplication.delayCall += Check;
        }
        private static void Check()
        {
            SessionState.SetBool(Pending, false);
            var report = new StringBuilder();
            SimulationMode oldMode = Physics.simulationMode;
            Keyboard keyboard = null;
            InputActionAsset actions = null;
            try
            {
                var motor = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
                if (!motor) throw new Exception("PlayerMovement missing.");
                var body = motor.GetComponent<Rigidbody>();
                var serializedMotor = new SerializedObject(motor);
                motor.GetComponent<PlayerInputReader>().enabled = false;
                motor.enabled = false;
                Physics.simulationMode = SimulationMode.Script;
                void Assert(bool condition, string label)
                { if (!condition) throw new Exception(label + " at " + body.position); report.AppendLine("PASS " + label); }
                void Tick(int count)
                {
                    for (int i = 0; i < count; i++)
                    { motor.SendMessage("FixedUpdate"); Physics.Simulate(Time.fixedDeltaTime); }
                }
                void Place(Vector3 position)
                {
                    motor.ClearInput(); body.position = position; body.linearVelocity = Vector3.zero;
                    Physics.SyncTransforms(); Tick(30);
                }
                Assert(motor.GetComponent<CapsuleCollider>() && motor.GetComponentsInChildren<Renderer>().Length == 0,
                    "Capsule player has no visual renderer");
                Assert(serializedMotor.FindProperty("stats").objectReferenceValue is PlayerStats,
                    "PlayerMovement references PlayerStats asset");
                Place(new Vector3(0,0.05f,-5));
                Assert(motor.IsGrounded && Mathf.Abs(body.position.y) < 0.05f, "Grounding");
                float start = body.position.z;
                motor.Move(Vector2.up); Tick(1);
                Assert(body.linearVelocity.z > 0f && body.linearVelocity.z < 4f,
                    "Movement accelerates instead of starting at full speed");
                Tick(49);
                Assert(Mathf.Abs(body.linearVelocity.z - 4f) < 0.01f && body.position.z - start > 3.5f,
                    "Forward speed reaches 4 m/s");
                motor.Move(Vector2.zero); Tick(1);
                Assert(body.linearVelocity.z > 0f && body.linearVelocity.z < 4f,
                    "Movement decelerates instead of stopping immediately");
                Tick(12);
                Assert(Mathf.Abs(body.linearVelocity.z) < 0.01f, "Deceleration stops horizontal movement");
                motor.Jump(); Tick(10);
                Assert(body.position.y > 0.7f && !motor.IsGrounded, "Fast jump rises off floor");
                float yVelocity = body.linearVelocity.y; motor.Jump(); Tick(1);
                Assert(body.linearVelocity.y < yVelocity, "No midair double jump");
                Tick(50); Assert(motor.IsGrounded, "Fast jump lands");
                Place(new Vector3(0,0.05f,-5));
                body.position += Vector3.up;
                Physics.SyncTransforms(); Tick(1);
                Assert(!motor.IsGrounded, "Player has just left the ground");
                motor.Jump(); Tick(1);
                Assert(body.linearVelocity.y > 0f, "Coyote time allows a late jump");
                Place(new Vector3(0,0.05f,-5));
                body.position += Vector3.up * 2f;
                Physics.SyncTransforms(); Tick(8);
                motor.Jump(); Tick(1);
                Assert(body.linearVelocity.y < 0f, "Coyote time expires after its window");
                Place(new Vector3(0,0.05f,10)); motor.Move(Vector2.up); Tick(60);
                Assert(body.position.z < 11.5f && body.position.z > 11.2f, "Wall blocks movement");
                Place(new Vector3(3,0.05f,-1.4f)); motor.Move(Vector2.up); Tick(70);
                Assert(body.position.y > 1.1f && body.position.z > 3.5f, "Walk up 0.20 m stairs");
                motor.Move(Vector2.down); Tick(75);
                Assert(body.position.y < 0.1f && motor.IsGrounded, "Walk down stairs");
                Place(new Vector3(7,0.05f,3.5f)); motor.Move(Vector2.up); Tick(75);
                Assert(body.position.y > 1.3f && body.position.z > 8.5f, "Walk up 0.28 m stairs");
                Place(new Vector3(0,0.05f,-5)); motor.Move(Vector2.one); Tick(30);
                Assert(new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude < 4.01f,
                    "Diagonal speed is normalized");
                motor.GetComponent<PlayerCamera>().Look(new Vector2(0,10000));
                var camera = motor.GetComponentInChildren<Camera>();
                float pitch = Mathf.DeltaAngle(0,camera.transform.localEulerAngles.x);
                Assert(Mathf.Abs(pitch + 85) < 0.1f, "Camera pitch clamps at 85 degrees");

                actions = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/_Project/Scripts/Gameplay/Player/PlayerControls.inputactions"));
                keyboard = InputSystem.AddDevice<Keyboard>();
                actions.devices = new InputDevice[] { keyboard };
                actions.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
                InputSystem.Update();
                Assert(actions.FindAction("Player/Move").ReadValue<Vector2>() == Vector2.up &&
                    actions.FindAction("Player/Jump").IsPressed(),
                    "New Input System W / Space bindings");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert(actions.FindAction("Player/Move").ReadValue<Vector2>() == Vector2.zero,
                    "Input release resets Move");
            }
            catch (Exception exception) { report.AppendLine("FAIL " + exception); }
            finally
            {
                if (actions) { actions.Disable(); UnityEngine.Object.DestroyImmediate(actions); }
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                Physics.simulationMode = oldMode;
                File.WriteAllText("Temp/RubberPlayerCheck.txt", report.ToString());
                Debug.Log(report.ToString());
                EditorApplication.isPlaying = false;
            }
        }
    }
}
