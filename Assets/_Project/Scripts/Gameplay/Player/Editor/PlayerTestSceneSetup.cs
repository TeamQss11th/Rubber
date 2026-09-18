using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Rubber.Gameplay.Player.Editor
{
    [InitializeOnLoad]
    public static class PlayerTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/PlayerTestScene.unity";
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
                { Debug.Log("Player test course already exists; existing scene kept."); return; }

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/_Project/Scripts/Gameplay/Player/PlayerControls.inputactions");
            if (!actions) throw new System.InvalidOperationException("PlayerControls input asset is not imported.");
            var stats = AssetDatabase.LoadAssetAtPath<PlayerStats>(
                "Assets/_Project/ScriptableObjects/Player/PlayerStats.asset");
            if (!stats) throw new System.InvalidOperationException("PlayerStats asset is not imported.");
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
            player.AddComponent<PlayerInputReader>().Configure(actions, movement, look);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            Debug.Log("Rubber player test scene ready: CapsuleCollider, Rigidbody, new Input Actions, 2 staircases, 4 obstacles.");
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
        private static void Box(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
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
