using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Rubber.Gameplay.Ducks;
using Rubber.Gameplay.Interaction;

namespace Rubber.Gameplay.Player.Editor
{
    // Runs against the test scene in Play mode; never saves simulated positions.
    [InitializeOnLoad]
    public static class PlayerMovementSmokeCheck
    {
        private const string Request = "Temp/RubberPlayerCheck.request";
        private const string Pending = "Rubber.PlayerSmokeCheck";
        private const string BatchRun = "Rubber.PlayerSmokeCheck.BatchRun";
        private const string BatchExitCode = "Rubber.PlayerSmokeCheck.BatchExitCode";
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

        public static void RunBatch()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("RunBatch is only intended for Unity batch mode.");

            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Test/PlayerTestScene.unity");
            SessionState.SetBool(BatchRun, true);
            Run();
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                EditorApplication.delayCall += Check;
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(BatchRun, false))
            {
                int exitCode = SessionState.GetInt(BatchExitCode, 1);
                SessionState.EraseBool(BatchRun);
                SessionState.EraseInt(BatchExitCode);
                EditorApplication.Exit(exitCode);
            }
        }
        private static void Check()
        {
            SessionState.SetBool(Pending, false);
            var report = new StringBuilder();
            SimulationMode oldMode = Physics.simulationMode;
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
                var detector = UnityEngine.Object.FindAnyObjectByType<PlayerInteractionDetector>();
                var target = UnityEngine.Object.FindAnyObjectByType<TestInteractable>();
                Assert(detector && target, "Interaction detector and test target exist");
                var camera = motor.GetComponentInChildren<Camera>();
                camera.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                detector.SendMessage("Update");
                camera.transform.localRotation = Quaternion.identity;
                detector.SendMessage("Update");
                Assert(detector.CurrentInteractable is TestInteractable &&
                    InteractionOutlineSelection.HasSelection &&
                    InteractionOutlineSelection.SelectedRenderers.Length == 1,
                    "Center ray selects interactable renderers for the outline mask");
                Assert(detector.TryInteract() && target.InteractionCount == 1 &&
                    target.LastInteractor == motor.gameObject,
                    "Interaction executes once on the centered target");
                camera.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                detector.SendMessage("Update");
                Assert(detector.CurrentInteractable == null &&
                    !InteractionOutlineSelection.HasSelection,
                    "Looking away clears the outline mask selection");
                Assert(!detector.TryInteract() && target.InteractionCount == 1,
                    "Interaction does not execute without a target");
                camera.transform.localRotation = Quaternion.identity;
                Place(new Vector3(0,0.05f,-7));
                Assert(!detector.TryInteract() && target.InteractionCount == 1,
                    "Interaction does not execute beyond its range");
                Place(new Vector3(0,0.05f,-5));

                var carrier = motor.GetComponent<PlayerDuckCarrier>();
                Assert(carrier, "Player has a duck carrier");
                var sceneDucks = UnityEngine.Object.FindObjectsByType<RubberDuckInteractable>(FindObjectsSortMode.None);
                Assert(sceneDucks.Length == 5, "Five independent cube duck placeholders exist in the test scene");
                foreach (RubberDuckInteractable sceneDuck in sceneDucks)
                    Assert(sceneDuck.GetComponent<MeshFilter>() && sceneDuck.GetComponent<BoxCollider>() &&
                        sceneDuck.GetComponent<Rigidbody>() && sceneDuck.transform.childCount == 0,
                        "Each duck placeholder is one interactable cube");
                GameObject CreateDuck(string name, float z)
                {
                    GameObject duckObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    duckObject.name = name;
                    duckObject.transform.position = new Vector3(0, 1.6f, z);
                    duckObject.transform.localScale = Vector3.one * 0.45f;
                    Rigidbody duckBody = duckObject.AddComponent<Rigidbody>();
                    duckBody.isKinematic = true;
                    duckBody.useGravity = false;
                    duckObject.AddComponent<RubberDuckInteractable>();
                    return duckObject;
                }
                GameObject firstDuckObject = CreateDuck("First Test Duck", -3f);
                GameObject secondDuckObject = CreateDuck("Second Test Duck", -2.5f);
                var firstDuck = firstDuckObject.GetComponent<RubberDuckInteractable>();
                var secondDuck = secondDuckObject.GetComponent<RubberDuckInteractable>();
                Physics.SyncTransforms();
                Assert(detector.TryInteract() && carrier.HeldDuck == firstDuck &&
                    firstDuck.transform.IsChildOf(camera.transform) &&
                    firstDuck.GetComponent<Renderer>().enabled &&
                    !firstDuck.GetComponent<Collider>().enabled,
                    "First duck is held visibly in front of the camera without collision");
                detector.SendMessage("Update");
                Assert(detector.CurrentInteractable == secondDuck &&
                    InteractionOutlineSelection.HasSelection,
                    "Second duck remains outlined while one is held");
                Assert(!detector.TryInteract(out bool blockedDuckTarget) && blockedDuckTarget &&
                    carrier.HeldDuck == firstDuck,
                    "A second duck cannot be picked up or trigger a drop");
                secondDuckObject.SetActive(false);
                Assert(detector.TryInteract() && target.InteractionCount == 2 &&
                    carrier.HeldDuck == firstDuck,
                    "Other interactables still work while holding a duck");
                camera.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                Assert(!detector.TryInteract(out bool emptyTarget) && !emptyTarget &&
                    carrier.TryDrop() && !carrier.IsHoldingDuck &&
                    !firstDuck.IsHeld && firstDuck.GetComponent<Collider>().enabled &&
                    firstDuckObject.transform.position.x > body.position.x,
                    "Empty-space interaction drops the duck in front of the player");
                float releaseHeight = firstDuckObject.transform.position.y;
                Tick(12);
                Assert(firstDuckObject.transform.position.y < releaseHeight - 0.1f,
                    "Released duck falls under gravity");
                firstDuckObject.SetActive(false);
                camera.transform.localRotation = Quaternion.identity;
                target.gameObject.SetActive(false);
                float start = body.position.z;
                motor.Move(Vector2.up); Tick(1);
                Assert(body.linearVelocity.z > 0f && body.linearVelocity.z < 4f,
                    "Movement accelerates instead of starting at full speed");
                Tick(49);
                Assert(Mathf.Abs(body.linearVelocity.z - 4f) < 0.01f && body.position.z - start > 3.5f,
                    "Forward speed reaches 4 m/s");
                motor.Move(Vector2.zero); Tick(1);
                Assert(Mathf.Abs(body.linearVelocity.z) < 0.01f,
                    "Releasing movement stops grounded horizontal drift");
                Tick(12);
                Assert(Mathf.Abs(body.linearVelocity.z) < 0.01f,
                    "Standing still does not resume horizontal movement");
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
                motor.Move(Vector2.down); Tick(95);
                Assert(body.position.y < 0.1f && motor.IsGrounded, "Walk down stairs");
                Place(new Vector3(7,0.05f,3.5f)); motor.Move(Vector2.up); Tick(75);
                Assert(body.position.y > 1.3f && body.position.z > 8.5f, "Walk up 0.28 m stairs");
                Place(new Vector3(0,0.05f,-5)); motor.Move(Vector2.one); Tick(30);
                Assert(new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude < 4.01f,
                    "Diagonal speed is normalized");
                motor.GetComponent<PlayerCamera>().Look(new Vector2(0,10000));
                float pitch = Mathf.DeltaAngle(0,camera.transform.localEulerAngles.x);
                Assert(Mathf.Abs(pitch + 85) < 0.1f, "Camera pitch clamps at 85 degrees");

                actions = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/_Project/Scripts/Gameplay/Player/PlayerControls.inputactions"));
                InputAction moveAction = actions.FindAction("Player/Move", true);
                InputAction jumpAction = actions.FindAction("Player/Jump", true);
                InputAction interactAction = actions.FindAction("Player/Interact", true);
                bool hasW = false;
                foreach (InputBinding binding in moveAction.bindings)
                    hasW |= binding.effectivePath == "<Keyboard>/w";
                bool hasSpace = false;
                foreach (InputBinding binding in jumpAction.bindings)
                    hasSpace |= binding.effectivePath == "<Keyboard>/space";
                bool hasE = false;
                foreach (InputBinding binding in interactAction.bindings)
                    hasE |= binding.effectivePath == "<Keyboard>/e";
                Assert(hasW && hasSpace && hasE,
                    "New Input System W / Space / E bindings");
            }
            catch (Exception exception) { report.AppendLine("FAIL " + exception); }
            finally
            {
                if (actions) { actions.Disable(); UnityEngine.Object.DestroyImmediate(actions); }
                Physics.simulationMode = oldMode;
                File.WriteAllText("Temp/RubberPlayerCheck.txt", report.ToString());
                Debug.Log(report.ToString());
                if (SessionState.GetBool(BatchRun, false))
                    SessionState.SetInt(BatchExitCode, report.ToString().Contains("FAIL") ? 1 : 0);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
