using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Rubber.Gameplay.Ducks;
using Rubber.Gameplay.Interaction;

namespace Rubber.Gameplay.Player.Editor
{
    [InitializeOnLoad]
    public static class PlayerTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/PlayerTestScene.unity";
        private const string DefaultDuckDataPath =
            "Assets/_Project/ScriptableObjects/Ducks/DefaultRubberDuckData.asset";
        private static readonly string[] TestDuckDataPaths =
        {
            DefaultDuckDataPath,
            "Assets/_Project/ScriptableObjects/Ducks/ReturnTestRubberDuckData1.asset",
            "Assets/_Project/ScriptableObjects/Ducks/ReturnTestRubberDuckData2.asset",
            "Assets/_Project/ScriptableObjects/Ducks/ReturnTestRubberDuckData3.asset",
            "Assets/_Project/ScriptableObjects/Ducks/ReturnTestRubberDuckData4.asset"
        };
        private const string Request = "Temp/RubberPlayerSetup.request";
        static PlayerTestSceneSetup() => EditorApplication.delayCall += ProcessRequest;

        private static void ProcessRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            Setup();
        }

        [MenuItem("Rubber/Setup Player Test Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Player Test Course")
                { Debug.Log("Player test course already exists; use Add Duck Test Objects for pickup testing."); return; }

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/_Project/Scripts/Gameplay/Player/PlayerControls.inputactions");
            if (!actions) throw new System.InvalidOperationException("PlayerControls input asset is not imported.");
            var stats = AssetDatabase.LoadAssetAtPath<PlayerStats>(
                "Assets/_Project/ScriptableObjects/Player/PlayerStats.asset");
            if (!stats) throw new System.InvalidOperationException("PlayerStats asset is not imported.");
            RubberDuckData[] duckData = LoadTestDuckData();
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var course = new GameObject("Player Test Course");
            Material floor = MaterialAsset("TestFloor", new Color(0.48f, 0.62f, 0.61f));
            Material obstacle = MaterialAsset("TestObstacle", new Color(0.87f, 0.55f, 0.30f));
            Material stairs = MaterialAsset("TestStairs", new Color(0.42f, 0.57f, 0.78f));
            Box("Floor", new Vector3(0,-0.25f,4), new Vector3(24,0.5f,26), floor, course.transform);
            Box("Low Jump Block", new Vector3(-3,0.3f,1), new Vector3(2,0.6f,2), obstacle, course.transform);
            Box("Medium Jump Block", new Vector3(-6,0.5f,4), new Vector3(2,1,2), obstacle, course.transform);
            Box("Tall Obstacle", new Vector3(-3,1.25f,6), new Vector3(2,2.5f,2), obstacle, course.transform);
            Box("Wall", new Vector3(0,1.5f,12), new Vector3(8,3,0.5f), obstacle, course.transform);
            BuildStairs("Shallow Stairs", new Vector3(3,0,0), 6, 0.2f, 0.65f, stairs, course.transform);
            BuildStairs("Higher Stairs", new Vector3(7,0,5), 5, 0.28f, 0.75f, stairs, course.transform);
            Box("Rear Boundary", new Vector3(0,1,-9), new Vector3(24,2,0.4f), floor, course.transform);
            Box("Left Boundary", new Vector3(-12,1,4), new Vector3(0.4f,2,26), floor, course.transform);
            Box("Right Boundary", new Vector3(12,1,4), new Vector3(0.4f,2,26), floor, course.transform);
            Box("Front Boundary", new Vector3(0,1,17), new Vector3(24,2,0.4f), floor, course.transform);
            GameObject interactionTarget = Box("Interaction Test Object", new Vector3(0,1f,-1),
                new Vector3(2f,2f,2f), obstacle, course.transform);
            interactionTarget.AddComponent<TestInteractable>();

            var player = new GameObject("Player");
            player.layer = 2;
            player.transform.SetParent(course.transform);
            player.transform.position = new Vector3(0,0.05f,-5);
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0,0.9f,0);
            const string physicsPath = "Assets/_Project/Art/Materials/PlayerFrictionless.physicMaterial";
            var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physicsPath);
            if (!friction)
            {
                friction = new PhysicsMaterial("PlayerFrictionless") { staticFriction = 0, dynamicFriction = 0,
                    bounciness = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
                AssetDatabase.CreateAsset(friction, physicsPath);
            }
            capsule.sharedMaterial = friction;
            var body = player.AddComponent<Rigidbody>();
            body.mass = 70;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Camera camera = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                camera = root.GetComponentInChildren<Camera>();
                if (camera) break;
            }
            if (!camera)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera = cameraObject.GetComponent<Camera>();
            }
            camera.tag = "MainCamera";
            camera.transform.SetParent(player.transform);
            camera.transform.localPosition = new Vector3(0,1.6f,0);
            camera.transform.localRotation = Quaternion.identity;
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 70;
            var movement = player.AddComponent<PlayerMovement>();
            movement.Configure(camera.transform, stats);
            var look = player.AddComponent<PlayerCamera>();
            look.Configure(camera.transform);
            player.AddComponent<PlayerDuckCarrier>().Configure(camera.transform);
            player.AddComponent<PlayerInputReader>().Configure(actions, movement, look);
            player.AddComponent<PlayerInteractionDetector>().Configure(camera, stats);

            AddDuckTestObjects(course.transform, obstacle, duckData);
            AddDuckReturnTestArea(course.transform, stairs, duckData);

            var guide = new GameObject("Interaction Guide Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(InteractionReticle));
            guide.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            Debug.Log("Rubber player test scene ready: CapsuleCollider, Rigidbody, new Input Actions, 2 staircases, 4 obstacles.");
        }

        [MenuItem("Rubber/Add Duck Test Objects")]
        public static void AddDuckTestObjectsToCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Open PlayerTestScene in Edit mode before adding duck test objects.");
                return;
            }

            Transform course = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Player Test Course") course = root.transform;
            if (!course) return;

            Material obstacle = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Art/Materials/TestObstacle.mat");
            if (AddDuckTestObjects(course, obstacle, LoadTestDuckData()))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        [MenuItem("Rubber/Add Duck Return Test Area")]
        public static void AddDuckReturnTestAreaToCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Open PlayerTestScene in Edit mode before adding the return test area.");
                return;
            }

            Transform course = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Player Test Course") course = root.transform;
            if (!course) return;

            Material poolMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Art/Materials/TestStairs.mat");
            if (AddDuckReturnTestArea(course, poolMaterial, LoadTestDuckData()))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static bool AddDuckTestObjects(Transform course, Material material, RubberDuckData[] duckData)
        {
            Vector3[] positions =
            {
                new(-0.45f, 0.175f, -3f),
                new(0f, 0.175f, -3f),
                new(0.45f, 0.175f, -3f),
                new(-0.225f, 0.175f, -2.55f),
                new(0.225f, 0.175f, -2.55f)
            };
            if (duckData == null || duckData.Length != positions.Length)
                throw new System.InvalidOperationException("Five test duck data assets are required.");

            bool added = false;
            for (int i = 0; i < positions.Length; i++)
            {
                string name = $"Duck Pickup Test {i + 1}";
                Transform existing = course.Find(name);
                if (existing)
                {
                    RubberDuckInteractable interactable = existing.GetComponent<RubberDuckInteractable>();
                    if (interactable && interactable.Data != duckData[i])
                    {
                        Undo.RecordObject(interactable, "Assign Default Rubber Duck Data");
                        interactable.Configure(duckData[i]);
                        EditorUtility.SetDirty(interactable);
                        added = true;
                    }
                    if (EnsureDuckMarkerCube(existing, material))
                        added = true;
                    continue;
                }

                GameObject duck = GameObject.CreatePrimitive(PrimitiveType.Cube);
                duck.name = name;
                duck.transform.SetParent(course);
                duck.transform.position = positions[i];
                duck.transform.localScale = Vector3.one * 0.35f;
                duck.GetComponent<Renderer>().sharedMaterial = material;
                Rigidbody duckBody = duck.AddComponent<Rigidbody>();
                duckBody.isKinematic = true;
                duckBody.useGravity = false;
                duck.AddComponent<RubberDuckInteractable>().Configure(duckData[i]);
                EnsureDuckMarkerCube(duck.transform, material);
                added = true;
            }
            return added;
        }

        private static bool EnsureDuckMarkerCube(Transform duck, Material material)
        {
            if (duck.Find("cube")) return false;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "cube";
            marker.transform.SetParent(duck, false);
            marker.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            marker.transform.localScale = Vector3.one * 0.5f;
            marker.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            return true;
        }

        private static bool AddDuckReturnTestArea(
            Transform course, Material material, RubberDuckData[] duckData)
        {
            if (course.Find("Duck Return Pool")) return false;

            var pool = new GameObject("Duck Return Pool");
            pool.transform.SetParent(course);
            pool.transform.position = new Vector3(5f, 0f, -4.5f);
            RubberDuckReturnRegistry registry = pool.AddComponent<RubberDuckReturnRegistry>();
            registry.Configure(duckData);

            Box("Pool Bottom", pool.transform.position + new Vector3(0f, 0.05f, 0f),
                new Vector3(3.6f, 0.1f, 3.6f), material, pool.transform);
            Box("Pool Wall Left", pool.transform.position + new Vector3(-1.75f, 0.45f, 0f),
                new Vector3(0.3f, 0.9f, 3.6f), material, pool.transform);
            Box("Pool Wall Right", pool.transform.position + new Vector3(1.75f, 0.45f, 0f),
                new Vector3(0.3f, 0.9f, 3.6f), material, pool.transform);
            Box("Pool Wall Back", pool.transform.position + new Vector3(0f, 0.45f, 1.75f),
                new Vector3(3.2f, 0.9f, 0.3f), material, pool.transform);
            Box("Pool Wall Front", pool.transform.position + new Vector3(0f, 0.45f, -1.75f),
                new Vector3(3.2f, 0.9f, 0.3f), material, pool.transform);

            var triggerObject = new GameObject("Return Trigger");
            triggerObject.transform.SetParent(pool.transform, false);
            triggerObject.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.1f, 0.8f, 3.1f);
            triggerObject.AddComponent<RubberDuckReturnZone>().Configure(registry);

            var hud = new GameObject("Duck Return HUD");
            hud.transform.SetParent(course);
            hud.AddComponent<RubberDuckReturnDebugHud>().Configure(registry);
            return true;
        }

        private static RubberDuckData[] LoadTestDuckData()
        {
            var data = new RubberDuckData[TestDuckDataPaths.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = AssetDatabase.LoadAssetAtPath<RubberDuckData>(TestDuckDataPaths[i]);
                if (!data[i])
                    throw new System.InvalidOperationException($"Test duck data is not imported: {TestDuckDataPaths[i]}");
            }
            return data;
        }

        private static Material MaterialAsset(string name, Color color)
        {
            string path = "Assets/_Project/Art/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }
        private static void BuildStairs(string name, Vector3 origin, int count, float rise, float depth,
            Material material, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            for (int i = 0; i < count; i++)
            {
                float height = (i + 1) * rise;
                Box("Step " + (i + 1), origin + new Vector3(0,height * 0.5f,i * depth),
                    new Vector3(2.5f,height,depth), material, root.transform);
            }
            Box("Landing", origin + new Vector3(0,count * rise * 0.5f,count * depth + 0.5f),
                new Vector3(2.5f,count * rise,1 + depth), material, root.transform);
        }
    }
}
