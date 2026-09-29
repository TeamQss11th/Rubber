using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    public static class VillaMapBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/VillaLightingStudy.unity";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/VillaStudy";
        private static Transform architecture, furniture, garden, markers;
        private static readonly List<string> usedAssets = new List<string>();

        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save your open scene before creating the study. No scene was changed.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("Study already exists. Open it for editing; creation never overwrites it.");
            usedAssets.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            architecture = Group("01 Architecture");
            furniture = Group("02 Furniture and Props");
            garden = Group("03 Garden and Pool");
            markers = Group("04 Duck Location Markers - not collectibles");
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            var sand = Material("Warm stone", new Color(.68f, .63f, .51f), .18f);
            var yellow = Material("Duck marker yellow", new Color(1, .73f, .015f), .4f);
            var orange = Material("Duck marker beak", new Color(1, .27f, .015f), .25f);
            var teal = Material("Return marker teal", new Color(.035f, .47f, .45f), .35f);
            var water = Material("Still water study", new Color(.045f, .38f, .43f), .82f);
            water.SetFloat("_Metallic", .25f);

            // A 10 x 6 m interior; floors use the vendor's original 2 m module.
            for (var x = -4; x <= 4; x += 2)
            for (var z = -2; z <= 2; z += 2)
            {
                Piece("floors/Floor2x2", new Vector3(x, -1, z), 0, architecture);
                Piece("roof/Roof2x2", new Vector3(x, 2.5f, z), 0, architecture);
            }
            for (var x = -4; x <= 4; x += 2)
                Piece("Walls/Wall2x2,5", new Vector3(x, 0, 3), 0, architecture);
            for (var z = -2; z <= 2; z += 2)
            {
                Piece("Walls/Wall2x2,5", new Vector3(-5, 0, z), 90, architecture);
                Piece("Windows/WindowStraight", new Vector3(5, 0, z), 90, architecture);
            }
            foreach (var x in new[] { -4, -2, 2, 4 })
                Piece("Windows/WindowStraight", new Vector3(x, 0, -3), 0, architecture);

            // 2 m opening in the front facade; no door mechanic required.
            Box("Entrance lintel", new Vector3(0, 2.55f, -3), new Vector3(2, .2f, .3f), sand, architecture);
            Box("Terrace collision slab", new Vector3(0, -.16f, -5), new Vector3(14, .3f, 4), sand, architecture);
            for (var x = -6; x <= 6; x += 2)
            for (var z = -6; z <= -4; z += 2)
                Piece("floors/TerraceFloor200x200", new Vector3(x, -.1f, z), 0, architecture);

            // Pool deck consists of four slabs, leaving a real opening for the basin.
            Box("West pool deck", new Vector3(-4.75f, -.16f, -11), new Vector3(4.5f, .3f, 8), sand, architecture);
            Box("East pool deck", new Vector3(4.75f, -.16f, -11), new Vector3(4.5f, .3f, 8), sand, architecture);
            Box("South pool deck", new Vector3(0, -.16f, -14.5f), new Vector3(5, .3f, 1), sand, architecture);
            Box("North pool deck", new Vector3(0, -.16f, -7.5f), new Vector3(5, .3f, 1), sand, architecture);
            var pool = Piece("buildings/PoolA", new Vector3(0, -1.12f, -11), 0, garden);
            Box("Still water - visual prototype", new Vector3(0, -.18f, -11), new Vector3(4.05f, .03f, 5.05f), water, garden, false);
            // No swimming in this study: visible pool rim and invisible collision edge.
            Box("Pool safety west", new Vector3(-2.5f, .5f, -11), new Vector3(.12f, 1, 6), null, garden);
            Box("Pool safety east", new Vector3(2.5f, .5f, -11), new Vector3(.12f, 1, 6), null, garden);
            Box("Pool safety north", new Vector3(0, .5f, -8), new Vector3(5, 1, .12f), null, garden);
            Box("Pool safety south", new Vector3(0, .5f, -14), new Vector3(5, 1, .12f), null, garden);
            Box("Landscape foundation", new Vector3(0, -1.6f, -5), new Vector3(38, .2f, 38), sand, garden);

            Piece("Props/CarpetA", new Vector3(-2.4f, .015f, .1f), 0, furniture);
            Piece("furniture/CouchC", new Vector3(-2.5f, .03f, 1.5f), 180, furniture);
            Piece("furniture/CouchD", new Vector3(-3.8f, .03f, -.3f), 90, furniture);
            var coffee = Piece("furniture/CouchTable", new Vector3(-2.3f, .03f, -.2f), 0, furniture);
            Piece("Props/CoffeePot", new Vector3(-2.35f, BoundsOf(coffee).max.y, -.2f), 0, furniture, false);
            Piece("Props/BookA", new Vector3(-2, .04f, .8f), 25, furniture, false);
            var table = Piece("furniture/DiningTable", new Vector3(2.5f, .015f, .8f), 90, furniture);
            foreach (var z in new[] { .15f, 1.45f })
            {
                Piece("furniture/DiningChair", new Vector3(1.6f, .015f, z), 90, furniture);
                Piece("furniture/DiningChair", new Vector3(3.4f, .015f, z), -90, furniture);
            }
            Piece("Props/Bowl", new Vector3(2.5f, BoundsOf(table).max.y, .8f), 0, furniture, false);
            Piece("Plants/PottedPlantA", new Vector3(-4.15f, .015f, 2.1f), 0, furniture);
            Piece("Plants/PottedPlantB", new Vector3(4.1f, .015f, -2.1f), 0, furniture);
            Piece("Props/PaintingA", new Vector3(-2.5f, 1.15f, 2.83f), 180, furniture, false);
            foreach (var x in new[] { -4.25f, 4.25f })
            {
                Piece("furniture/Lounger", new Vector3(x, 0, -10.6f), 0, furniture);
                Piece("Plants/PottedPlantA", new Vector3(x, 0, -6.1f), 0, garden);
            }
            var side = Piece("furniture/SideTable Variant", new Vector3(5.3f, 0, -10.7f), 0, furniture);
            Piece("Props/TowelA", new Vector3(5.3f, BoundsOf(side).max.y, -10.7f), 0, furniture, false);
            foreach (var x in new[] { -8.5f, 8.5f })
            foreach (var z in new[] { -12, -3, 4 })
                Piece("Plants/PalmA", new Vector3(x, -1.5f, z), x * 15 + z, garden);
            // Low planted borders keep the test area legible and contain the walking route.
            foreach (var x in new[] { -6.8f, 6.8f })
            foreach (var z in new[] { -13, -10, -5 })
                Piece("Stoneboxes/StoneboxLow", new Vector3(x, 0, z), 0, garden);
            Box("Boundary west", new Vector3(-7.1f, .45f, -8), new Vector3(.18f, .9f, 14), sand, garden);
            Box("Boundary east", new Vector3(7.1f, .45f, -8), new Vector3(.18f, .9f, 14), sand, garden);
            Box("Boundary south", new Vector3(0, .45f, -15.1f), new Vector3(14.4f, .9f, .18f), sand, garden);
            Box("Terrace north west", new Vector3(-6, .45f, -3), new Vector3(2, .9f, .18f), sand, garden);
            Box("Terrace north east", new Vector3(6, .45f, -3), new Vector3(2, .9f, .18f), sand, garden);

            Marker("D01 Easy - entry table", new Vector3(-2.3f, .43f, -.45f), yellow, orange);
            Marker("D02 Medium - sofa side", new Vector3(-3.25f, .06f, 1.15f), yellow, orange);
            Marker("D03 Easy - dining chair", new Vector3(1.2f, .06f, 1.8f), yellow, orange);
            Marker("D04 Medium - planter side", new Vector3(4.8f, .04f, -6.1f), yellow, orange);
            Marker("D05 Easy - towel table", new Vector3(5.1f, BoundsOf(side).max.y + .02f, -10.65f), yellow, orange);
            Marker("D06 Hard - lounger far side", new Vector3(-4.7f, .03f, -11.45f), yellow, orange);
            Box("Return point visual marker - gameplay pending", new Vector3(0, .015f, -7.35f), new Vector3(1.1f, .03f, .65f), teal, markers, false);

            var lightingRoot = Group("05 Lighting - one main light and four practicals");
            var rig = lightingRoot.gameObject.AddComponent<VillaLightingPreview>();
            rig.sun = Light("Sun and moon", LightType.Directional, Vector3.zero, lightingRoot, true);
            rig.sun.shadowBias = .04f;
            rig.sun.shadowNormalBias = .3f;
            RenderSettings.sun = rig.sun;
            var practicals = new List<Light>();
            foreach (var x in new[] { -2.5f, 2.5f })
            {
                Piece("lighting/RoofLamp", new Vector3(x, 1.35f, .5f), 0, furniture, false);
                var light = Light("Interior pendant", LightType.Spot, new Vector3(x, 2.2f, .5f), lightingRoot, true);
                light.transform.rotation = Quaternion.Euler(90, 0, 0);
                light.range = 5;
                light.spotAngle = 110;
                light.innerSpotAngle = 65;
                practicals.Add(light);
            }
            foreach (var x in new[] { -4.25f, 4.25f })
            {
                Piece("lighting/FloorLight on Variant", new Vector3(x, 0, -7.3f), 0, furniture, false);
                var light = Light("Terrace pool guide", LightType.Point, new Vector3(x, .75f, -7.3f), lightingRoot, false);
                light.range = 4;
                practicals.Add(light);
            }
            rig.practicalLights = practicals.ToArray();
            rig.skyMaterial = new Material(Shader.Find("Skybox/Procedural"));
            rig.skyMaterial.name = "Villa study sky";
            AssetDatabase.CreateAsset(rig.skyMaterial, MaterialFolder + "/VillaSky.mat");
            RenderSettings.skybox = rig.skyMaterial;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 28;
            RenderSettings.fogEndDistance = 65;
            rig.Apply(0);

            var player = new GameObject("06 First person walkthrough - test only");
            player.transform.position = new Vector3(0, .05f, -5.8f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0, .9f, 0);
            controller.radius = .28f;
            controller.stepOffset = .2f;
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 70;
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 80;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            player.AddComponent<VillaWalkthrough>().view = cameraObject.transform;
            EditorSceneManager.SaveScene(scene, ScenePath);
            SaveStudyMaterials();
            WriteReport(scene);
            File.WriteAllLines("Docs/MapBuild/used-assets.txt", usedAssets.Distinct().OrderBy(x => x));
            Capture();
            Selection.activeGameObject = player;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 0, -5), Quaternion.Euler(32, -35, 0), 22);
            Debug.Log("Rubber: Villa Lighting Study created and saved. F1/F2/F3 lighting; click + WASD to walk.");
        }

        private static Transform Group(string name) => new GameObject(name).transform;
        private static Material Material(string name, Color color, float smoothness)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, MaterialFolder + "/" + name + ".mat");
            return material;
        }

        internal static GameObject Piece(string relative, Vector3 bottomCenter, float yaw, Transform parent, bool solid = true)
        {
            var path = "Assets/Modern Villa/Prefabs/" + relative + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new FileNotFoundException(path);
            usedAssets.Add(path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, yaw, 0));
            var b = BoundsOf(instance);
            instance.transform.position += bottomCenter - new Vector3(b.center.x, b.min.y, b.center.z);
            foreach (var light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var probe in instance.GetComponentsInChildren<ReflectionProbe>(true)) probe.enabled = false;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = solid;
            return instance;
        }

        internal static Bounds BoundsOf(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException(obj.name + " has no renderer");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        internal static GameObject Box(string name, Vector3 center, Vector3 size, Material material, Transform parent, bool solid = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = center;
            obj.transform.localScale = size;
            obj.GetComponent<Collider>().enabled = solid;
            if (material != null) obj.GetComponent<Renderer>().sharedMaterial = material;
            else obj.GetComponent<Renderer>().enabled = false;
            GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return obj;
        }

        private static void Marker(string name, Vector3 position, Material yellow, Material orange)
        {
            var root = new GameObject(name).transform;
            root.SetParent(markers);
            foreach (var part in new[] { ("Body", new Vector3(0, .11f, 0), new Vector3(.25f, .19f, .3f)), ("Head", new Vector3(0, .24f, .085f), new Vector3(.15f, .15f, .15f)) })
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = part.Item1;
                obj.transform.SetParent(root, false);
                obj.transform.localPosition = part.Item2;
                obj.transform.localScale = part.Item3;
                obj.GetComponent<Renderer>().sharedMaterial = yellow;
                UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            }
            Box("Beak", new Vector3(0, .24f, .17f), new Vector3(.1f, .035f, .1f), orange, root, false);
            root.position = position;
        }

        private static Light Light(string name, LightType type, Vector3 position, Transform parent, bool shadow)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            var light = obj.AddComponent<Light>();
            light.type = type;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = new Color(1, .72f, .43f);
            light.shadows = shadow ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        public static void Capture()
        {
            var scene = SceneManager.GetActiveScene();
            var rig = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<VillaLightingPreview>()).FirstOrDefault();
            if (rig == null) throw new InvalidOperationException("Open the study scene first.");
            Directory.CreateDirectory("Docs/MapBuild");
            var obj = new GameObject("Temporary review camera");
            var camera = obj.AddComponent<Camera>();
            camera.fieldOfView = 60;
            camera.farClipPlane = 80;
            var target = new RenderTexture(1600, 900, 24);
            var original = RenderTexture.active;
            var blend = rig.nightBlend;
            try
            {
                foreach (var mode in new[] { ("day", 0f), ("dusk", .5f), ("night", 1f) })
                {
                    rig.Apply(mode.Item2);
                    foreach (var view in new[] { ("overview", new Vector3(12, 9, -21), new Vector3(0, 0, -5)), ("interior", new Vector3(.2f, 1.65f, -2.1f), new Vector3(-1, 1, 1.1f)) })
                    {
                        camera.transform.position = view.Item2;
                        camera.transform.LookAt(view.Item3);
                        camera.targetTexture = target;
                        Physics.SyncTransforms();
                        camera.Render();
                        camera.Render();
                        RenderTexture.active = target;
                        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                        texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                        texture.Apply();
                        File.WriteAllBytes($"Docs/MapBuild/{view.Item1}-{mode.Item1}.png", texture.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
            }
            finally
            {
                rig.Apply(blend);
                RenderTexture.active = original;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(obj);
            }
            EditorSceneManager.SaveScene(scene);
            SaveStudyMaterials();
        }

        private static void SaveStudyMaterials()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        public static void Refine()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the study first.");
            var props = scene.GetRootGameObjects().First(x => x.name.StartsWith("02 ")).transform;
            var book = props.Find("BookA");
            if (book != null)
            {
                var best = Quaternion.identity;
                float height = float.MaxValue;
                foreach (var rotation in new[] { Quaternion.identity, Quaternion.Euler(90, 0, 0), Quaternion.Euler(0, 0, 90) })
                {
                    book.rotation = rotation;
                    float candidate = BoundsOf(book.gameObject).size.y;
                    if (candidate < height) { best = rotation; height = candidate; }
                }
                book.rotation = Quaternion.Euler(0, 25, 0) * best;
                var b = BoundsOf(book.gameObject);
                book.position += new Vector3(-2, .04f, .8f) - new Vector3(b.center.x, b.min.y, b.center.z);
            }
            var pot = props.Find("CoffeePot");
            if (pot != null) pot.position += new Vector3(-2.3f, pot.position.y, -.04f) - new Vector3(pot.position.x, pot.position.y, pot.position.z);
            Capture();
            ValidateRoute();
        }

        public static void ValidateRoute()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open the study first.");
            Physics.SyncTransforms();
            var route = new[] { new Vector3(0, 0, -5.8f), new Vector3(0, 0, 1.8f), new Vector3(0, 0, -5.8f), new Vector3(3.1f, 0, -7.4f), new Vector3(3.1f, 0, -14.6f), new Vector3(-3.1f, 0, -14.6f), new Vector3(-3.1f, 0, -7.4f), new Vector3(0, 0, -5.8f) };
            var issues = new HashSet<string>();
            int tested = 0;
            for (var i = 1; i < route.Length; i++)
            {
                int steps = Mathf.CeilToInt(Vector3.Distance(route[i - 1], route[i]) / .2f);
                for (var s = 0; s <= steps; s++)
                {
                    var p = Vector3.Lerp(route[i - 1], route[i], (float)s / steps);
                    tested++;
                    if (!Physics.Raycast(p + Vector3.up * .5f, Vector3.down, out var hit, .8f)) issues.Add($"No floor at {p}");
                    foreach (var overlap in Physics.OverlapCapsule(p + Vector3.up * .38f, p + Vector3.up * 1.5f, .28f))
                        if (!(overlap is CharacterController)) issues.Add($"Segment {i}: blocked by {overlap.name}");
                }
            }
            File.WriteAllText("Docs/MapBuild/route-validation.txt", $"Sampled {tested} positions along entry/interior/pool loop using radius 0.28m and 0.2m steps.\n" + (issues.Count == 0 ? "PASS: floor and capsule clearance. Does not verify collection or visual clues.\n" : string.Join("\n", issues)));
        }

        public static void BuildPlayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) throw new FileNotFoundException(ScenePath);
            Directory.CreateDirectory("Builds/VillaStudy");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/VillaStudy/RubberVilla.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            File.WriteAllText("Docs/MapBuild/build-result.txt", $"{report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\n");
        }

        private static void WriteReport(Scene scene)
        {
            var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
            var lights = all.SelectMany(x => x.GetComponents<Light>()).ToArray();
            var renderers = all.SelectMany(x => x.GetComponents<Renderer>()).ToArray();
            var missing = renderers.Sum(r => r.sharedMaterials.Count(m => m == null || m.shader == null || !m.shader.isSupported));
            Directory.CreateDirectory("Docs/MapBuild");
            File.WriteAllText("Docs/MapBuild/scene-audit.txt", $"Scene: {scene.path}\nObjects: {all.Length}\nRenderers: {renderers.Length}\nLights: {lights.Length}\nShadow-capable lights: {lights.Count(l => l.shadows != LightShadows.None)}\nMissing/unsupported material slots: {missing}\nDuck markers: {markers.childCount - 1}\nPipeline: unchanged PC_RPAsset baseline\nGPU/frame timings: NOT MEASURED\n");
        }
    }
}
