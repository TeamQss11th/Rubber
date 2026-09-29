using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static Rubber.EditorTools.VillaMapBuilder;

namespace Rubber.EditorTools
{
    public static class VillaEstateBuilder
    {
        public const string PathName = "Assets/_Project/Scenes/Main/VillaEstate.unity";
        const string ArtPath = "Assets/_Project/Art/Materials/VillaEstate";
        const string ReportPath = "Docs/VillaEstate";
        static readonly List<Light> lamps = new List<Light>();
        static Transform architecture, props, greenery, lightingRoot, markers;
        static Material stone, grass, water, brass;

        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(PathName)) throw new InvalidOperationException("Estate already exists; creation will not overwrite it.");
            Directory.CreateDirectory(ReportPath);
            Directory.CreateDirectory(ArtPath);
            // Preserve unsaved user work as a separate scene before switching scenes.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var open = SceneManager.GetSceneAt(i);
                if (!open.isDirty) continue;
                var backup = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Scenes/Test/" + open.name + "_BeforeExpansion.unity");
                if (!EditorSceneManager.SaveScene(open, backup, true)) throw new IOException("Could not preserve unsaved scene.");
                File.AppendAllText(ReportPath + "/preserved-scenes.txt", backup + "\n");
            }
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Modern Villa.unity", OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, PathName)) throw new IOException("Could not create estate copy.");
            var originalRoots = scene.GetRootGameObjects();
            var core = new GameObject("01 Original Villa - furnished main house").transform;
            foreach (var root in originalRoots)
            {
                if (root.GetComponent<Terrain>() != null || root.name.ToLowerInvariant().Contains("terrain")) { root.SetActive(false); root.transform.SetParent(core); continue; }
                foreach (var sourceCamera in root.GetComponentsInChildren<Camera>(true)) sourceCamera.enabled = false;
                foreach (var audio in root.GetComponentsInChildren<AudioListener>(true)) audio.enabled = false;
                foreach (var light in root.GetComponentsInChildren<Light>(true)) light.enabled = false;
                foreach (var probe in root.GetComponentsInChildren<ReflectionProbe>(true)) probe.enabled = false;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.lightmapIndex = -1;
                root.transform.SetParent(core, true);
            }
            core.position = new Vector3(2.84f, -.75f, 5.75f);
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();
            var settings = new LightingSettings { name = "Estate dynamic lighting", bakedGI = false, realtimeGI = false };
            AssetDatabase.CreateAsset(settings, ArtPath + "/EstateLighting.lighting");
            Lightmapping.lightingSettings = settings;
            architecture = new GameObject("02 Extension Architecture - west and east wings").transform;
            props = new GameObject("03 Furnished extension rooms").transform;
            greenery = new GameObject("04 Courtyard gardens and arrival promenade").transform;
            markers = new GameObject("05 Duck location markers - gameplay pending").transform;
            lightingRoot = new GameObject("06 Budgeted estate lighting").transform;
            lamps.Clear();
            foreach (var source in core.GetComponentsInChildren<Light>().Where(l => l.type != LightType.Directional).Take(24))
            {
                AddLamp(source.transform.position, source.type == LightType.Spot);
                lamps[lamps.Count - 1].transform.rotation = source.transform.rotation;
            }
            stone = NewMaterial("Warm limestone", new Color(.66f, .63f, .54f), .22f);
            grass = NewMaterial("Garden green", new Color(.22f, .30f, .16f), .05f);
            water = NewMaterial("Pool water prototype", new Color(.025f, .31f, .37f), .85f);
            brass = NewMaterial("Wayfinding brass", new Color(.82f, .58f, .20f), .55f);
            Box("120 x 110 m landscape", new Vector3(0, -.4f, 0), new Vector3(120, .4f, 110), grass, greenery);
            Box("72 x 80 m estate foundation", new Vector3(0, -.2f, 0), new Vector3(72, .4f, 80), stone, architecture);

            // Six genuinely separate furnished rooms in two connected wings.
            Room("West lounge", new Vector3(-25, 0, 12), 0);
            Room("West reading room", new Vector3(-25, 0, 0), 1);
            Room("West games lounge", new Vector3(-25, 0, -12), 2);
            Room("East dining room", new Vector3(25, 0, 12), 3);
            Room("East tea room", new Vector3(25, 0, 0), 4);
            Room("East garden lounge", new Vector3(25, 0, -12), 5);

            // Main swimming court and garden pockets occupy the enlarged southern half.
            var pool = Piece("buildings/Pool", new Vector3(0, -1.1f, -11), 0, greenery);
            pool.name = "Grand swimming court - vendor pool modules";
            Box("Still water - no reflection camera", new Vector3(0, .35f, -11), new Vector3(10.8f, .03f, 15.8f), water, greenery, false);
            foreach (var x in new[] { -9f, 9f })
            for (int i = 0; i < 5; i++)
            {
                float z = -17 + i * 3;
                Piece("furniture/Lounger", new Vector3(x, .03f, z), x < 0 ? 90 : -90, props);
                if (i % 2 == 0) Piece("Props/Sunshade", new Vector3(x * 1.24f, .02f, z), 0, props, false);
            }
            foreach (var x in new[] { -16f, 16f })
            foreach (var z in new[] { -26f, -12f, 2f, 20f, 33f })
            {
                Box("Garden island", new Vector3(x, .08f, z), new Vector3(4.5f, .16f, 6), grass, greenery);
                Piece("Plants/PalmA", new Vector3(x, .16f, z), z, greenery);
                Piece("Plants/Shrub", new Vector3(x + 1.1f, .16f, z + 1.5f), 0, greenery, false);
                Piece("Plants/Shrub", new Vector3(x - 1.1f, .16f, z - 1.5f), 40, greenery, false);
            }
            // Pavilions define destinations rather than leaving the enlarged grounds empty.
            foreach (var x in new[] { -26f, 26f })
            {
                Piece("Props/PavillonA", new Vector3(x, .02f, -29), 0, props);
                Piece("furniture/Couch Group B", new Vector3(x, .02f, -29), 0, props);
                AddLamp(new Vector3(x, 2.5f, -29), true);
            }
            Box("Entry spine", new Vector3(0, .015f, -30), new Vector3(7, .03f, 18), stone, architecture);
            Box("Return destination brass inlay", new Vector3(0, .035f, -23), new Vector3(3, .05f, 2), brass, markers, false);
            foreach (var x in new[] { -4.5f, 4.5f })
            foreach (var z in new[] { -36f, -29f, -22f })
            {
                Piece("Stoneboxes/StoneboxLong", new Vector3(x, .02f, z), 0, greenery);
                Piece("Plants/PlantA", new Vector3(x, .45f, z), 0, greenery, false);
            }
            foreach (var x in new[] { -13f, 13f })
            foreach (var z in new[] { -30f, -18f, -6f, 8f, 24f, 35f })
            {
                Piece("lighting/FloorLight on Variant", new Vector3(x, 0, z), 0, props, false);
                AddLamp(new Vector3(x, .85f, z), false);
            }
            // Low perimeter edges; front opening remains a clear architectural entrance.
            Box("West estate wall", new Vector3(-36, .45f, 0), new Vector3(.3f, .9f, 80), stone, architecture);
            Box("East estate wall", new Vector3(36, .45f, 0), new Vector3(.3f, .9f, 80), stone, architecture);
            Box("North estate wall", new Vector3(0, .45f, 40), new Vector3(72, .9f, .3f), stone, architecture);
            foreach (var x in new[] { -20f, 20f })
                Box("Arrival wall", new Vector3(x, .45f, -40), new Vector3(32, .9f, .3f), stone, architecture);

            var rig = lightingRoot.gameObject.AddComponent<VillaLightingPreview>();
            rig.sun = new GameObject("Estate sun and moon").AddComponent<Light>();
            rig.sun.transform.SetParent(lightingRoot);
            rig.sun.type = LightType.Directional;
            rig.sun.shadows = LightShadows.Soft;
            rig.sun.lightmapBakeType = LightmapBakeType.Realtime;
            rig.practicalLights = Array.Empty<Light>(); // Budget component owns the local lights.
            rig.skyMaterial = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(rig.skyMaterial, ArtPath + "/EstateSky.mat");
            RenderSettings.skybox = rig.skyMaterial;
            RenderSettings.sun = rig.sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 80;
            RenderSettings.fogEndDistance = 145;
            rig.Apply(0);
            var player = new GameObject("07 Estate first person walkthrough");
            player.transform.position = new Vector3(0, .08f, -34);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.center = Vector3.up * .9f; controller.radius = .28f; controller.stepOffset = .3f;
            var cameraObj = new GameObject("Estate Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObj.tag = "MainCamera";
            cameraObj.transform.SetParent(player.transform, false);
            cameraObj.transform.localPosition = Vector3.up * 1.65f;
            var camera = cameraObj.GetComponent<Camera>();
            camera.nearClipPlane = .08f; camera.farClipPlane = 150; camera.fieldOfView = 70;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            player.AddComponent<VillaWalkthrough>().view = camera.transform;
            var budget = lightingRoot.gameObject.AddComponent<VillaLightBudget>();
            budget.lighting = rig; budget.viewer = camera.transform; budget.localLights = lamps.ToArray();
            EditorSceneManager.SaveScene(scene, PathName);
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { ArtPath }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
            Capture();
            Audit();
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(48, -25, 0), 85);
            Selection.activeGameObject = player;
        }

        static Material NewMaterial(string name, Color color, float smoothness)
        {
            var result = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            result.name = name; result.SetColor("_BaseColor", color); result.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(result, ArtPath + "/" + name + ".mat");
            return result;
        }

        static void Room(string name, Vector3 center, int theme)
        {
            var shell = new GameObject(name).transform; shell.SetParent(architecture);
            // 12 x 10 m footprint and broad north/south openings form a room-to-room loop.
            Box(name + " floor", center + new Vector3(0, -.1f, 0), new Vector3(12, .2f, 10), stone, shell);
            for (int x = -5; x <= 5; x += 2)
            {
                if (Mathf.Abs(x) > 1)
                {
                    Piece("Windows/WindowStraight", center + new Vector3(x, 0, -5), 0, shell);
                    Piece("Walls/Wall2x2,5", center + new Vector3(x, 0, 5), 0, shell);
                }
            }
            for (int z = -4; z <= 4; z += 2)
            {
                if (z != 0)
                {
                    Piece("Windows/WindowStraight", center + new Vector3(-6, 0, z), 90, shell);
                    Piece("Windows/WindowStraight", center + new Vector3(6, 0, z), 90, shell);
                }
            }
            Box(name + " roof", center + Vector3.up * 2.65f, new Vector3(12.5f, .3f, 10.5f), stone, shell);
            // Columns frame the broad north/south openings.
            foreach (var x in new[] { -1.95f, 1.95f })
            foreach (var z in new[] { -5f, 5f })
                Box("Door jamb", center + new Vector3(x, 1.25f, z), new Vector3(.15f, 2.5f, .25f), stone, shell);
            if (theme == 3)
            {
                foreach (var x in new[] { -2.8f, 2.8f })
                {
                    var table = Piece("furniture/DiningTable", center + new Vector3(x, 0, 1), 0, props);
                    foreach (var dx in new[] { -.7f, .7f })
                    foreach (var z in new[] { .05f, 1.95f })
                        Piece("furniture/DiningChair", center + new Vector3(x + dx, 0, z), z < 1 ? 0 : 180, props);
                    Piece("Props/Bowl", new Vector3(center.x + x, BoundsOf(table).max.y, center.z + 1), 0, props, false);
                }
            }
            else
            {
                Piece("Props/CarpetA", center + new Vector3(-2.5f, .015f, 1), 0, props, false);
                Piece("furniture/Couch Group B", center + new Vector3(-2.5f, .025f, 1), theme * 90, props);
                Piece("furniture/LoungeChair", center + new Vector3(3.5f, .02f, 2.5f), -45, props);
                var table = Piece("furniture/CouchTableC", center + new Vector3(3.2f, .02f, .8f), 0, props);
                Piece(theme == 1 ? "Props/BookB" : "Props/CoffeePot", new Vector3(center.x + 3.2f, BoundsOf(table).max.y, center.z + .8f), 0, props, false);
            }
            foreach (var x in new[] { -4.8f, 4.8f })
                Piece("Plants/PottedPlantB", center + new Vector3(x, 0, 3.7f), 0, props);
            Piece("Props/PaintingA", center + new Vector3(-3.4f, 1.1f, 4.85f), 180, props, false);
            foreach (var x in new[] { -2.8f, 2.8f })
            {
                Piece("lighting/RoofLamp", center + new Vector3(x, 1.35f, .4f), 0, props, false);
                AddLamp(center + new Vector3(x, 2.2f, .4f), true);
            }
            var markerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/VillaStudy/Duck marker yellow.mat");
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Duck location " + (theme + 1) + " - " + name;
            marker.transform.SetParent(markers); marker.transform.position = center + new Vector3(4, .2f, -2.5f);
            marker.transform.localScale = new Vector3(.3f, .25f, .35f);
            marker.GetComponent<Renderer>().sharedMaterial = markerMat;
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        static void AddLamp(Vector3 position, bool spot)
        {
            var light = new GameObject(spot ? "Room local light" : "Path local light").AddComponent<Light>();
            light.transform.SetParent(lightingRoot); light.transform.position = position;
            light.transform.rotation = Quaternion.Euler(90, 0, 0);
            light.type = spot ? LightType.Spot : LightType.Point;
            light.spotAngle = 115; light.innerSpotAngle = 75; light.range = spot ? 7 : 5;
            light.color = new Color(1, .76f, .48f); light.lightmapBakeType = LightmapBakeType.Realtime;
            light.shadows = LightShadows.None; light.enabled = false; lamps.Add(light);
        }

        public static void Capture()
        {
            if (SceneManager.GetActiveScene().path != PathName) return;
            var rig = UnityEngine.Object.FindAnyObjectByType<VillaLightingPreview>();
            var budget = UnityEngine.Object.FindAnyObjectByType<VillaLightBudget>();
            var obj = new GameObject("Temporary estate review camera");
            var camera = obj.AddComponent<Camera>(); camera.farClipPlane = 250; camera.fieldOfView = 62;
            var target = new RenderTexture(1600, 1000, 24);
            var previous = RenderTexture.active;
            var blend = rig.nightBlend;
            var fog = RenderSettings.fog;
            try
            {
                foreach (var mode in new[] { ("day", 0f), ("night", 1f) })
                {
                    rig.Apply(mode.Item2);
                    foreach (var view in new[] { ("overview", new Vector3(48, 62, -74), new Vector3(0, 0, 0)), ("arrival", new Vector3(0, 1.7f, -34), new Vector3(0, 1, 8)), ("west-wing", new Vector3(-25, 1.65f, -3), new Vector3(-27, 1, 2)) })
                    {
                        // Aerial layout review omits distance fog; eye-level captures retain gameplay fog.
                        RenderSettings.fog = view.Item1 == "overview" ? false : fog;
                        camera.transform.position = view.Item2; camera.transform.LookAt(view.Item3); camera.targetTexture = target;
                        budget.Evaluate(camera.transform.position);
                        Physics.SyncTransforms(); camera.Render(); camera.Render(); RenderTexture.active = target;
                        var texture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                        texture.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); texture.Apply();
                        File.WriteAllBytes(ReportPath + "/" + view.Item1 + "-" + mode.Item1 + ".png", texture.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
            }
            finally
            {
                rig.Apply(blend); foreach (var lamp in budget.localLights) lamp.enabled = false;
                RenderSettings.fog = fog;
                RenderTexture.active = previous; camera.targetTexture = null; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(obj);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssetIfDirty(rig.skyMaterial);
        }

        public static void ValidateRoutes()
        {
            if (SceneManager.GetActiveScene().path != PathName) return;
            Physics.SyncTransforms();
            const int width = 137, depth = 153;
            var open = new bool[width, depth];
            var reached = new bool[width, depth];
            int tested = 0;
            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                var p = new Vector3(-34 + x * .5f, 0, -38 + z * .5f);
                bool floor = Physics.Raycast(p + Vector3.up * .2f, Vector3.down, .4f, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = Physics.OverlapCapsule(p + Vector3.up * .36f, p + Vector3.up * 1.55f, .28f, ~0, QueryTriggerInteraction.Ignore)
                    .Any(c => !(c is CharacterController) && c.bounds.max.y > .10f);
                open[x, z] = floor && !blocked;
                tested++;
            }
            var queue = new Queue<Vector2Int>();
            var start = new Vector2Int(68, 8);
            if (open[start.x, start.y]) { queue.Enqueue(start); reached[start.x, start.y] = true; }
            var offsets = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var delta in offsets)
                {
                    var next = cell + delta;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= depth || !open[next.x, next.y] || reached[next.x, next.y]) continue;
                    reached[next.x, next.y] = true; queue.Enqueue(next);
                }
            }
            var report = new System.Text.StringBuilder($"Ground-level extension route grid: {tested} samples, 0.5m spacing, 0.28m capsule radius.\nStart (0,-34). Excludes original villa stairs/upper floor; not a full playthrough.\n");
            foreach (var x in new[] { -25f, 25f })
            foreach (var z in new[] { -12f, 0f, 12f })
            {
                int ix = Mathf.RoundToInt((x + 1 + 34) * 2), iz = Mathf.RoundToInt((z - 3 + 38) * 2);
                report.AppendLine($"Room interior ({x + 1}, {z - 3}): {(reached[ix, iz] ? "PASS" : "BLOCKED")}");
            }
            var budget = UnityEngine.Object.FindAnyObjectByType<VillaLightBudget>();
            var rig = budget.lighting;
            var blend = rig.nightBlend;
            int maxLights = 0, maxShadows = 0, violations = 0;
            foreach (var mode in new[] { 0f, .5f, 1f })
            {
                rig.Apply(mode);
                for (int x = -32; x <= 32; x += 4)
                for (int z = -36; z <= 36; z += 4)
                {
                    budget.Evaluate(new Vector3(x, 1.65f, z));
                    int lit = budget.localLights.Count(l => l.enabled);
                    int shadows = budget.localLights.Count(l => l.enabled && l.shadows != LightShadows.None);
                    maxLights = Mathf.Max(maxLights, lit); maxShadows = Mathf.Max(maxShadows, shadows);
                    if (lit > 8 || shadows > 2 || (mode == 0 && lit != 0)) violations++;
                }
            }
            rig.Apply(blend); foreach (var lamp in budget.localLights) lamp.enabled = false;
            report.AppendLine($"969 light-budget samples: max active {maxLights}, max local shadows {maxShadows}, violations {violations}.");
            File.WriteAllText(ReportPath + "/route-and-light-validation.txt", report.ToString());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssetIfDirty(rig.skyMaterial);
        }

        public static void Audit()
        {
            if (SceneManager.GetActiveScene().path != PathName) return;
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>();
            var budget = UnityEngine.Object.FindAnyObjectByType<VillaLightBudget>();
            int missing = renderers.Sum(r => r.sharedMaterials.Count(m => m == null || m.shader == null || !m.shader.isSupported));
            File.WriteAllText(ReportPath + "/estate-audit.txt", $"Reference facility envelope: approximately 30.75 x 35.01m (1076.6 square metres bounding rectangle).\nEstate facility foundation: 72 x 80m (5760 square metres); 5.35x bounding footprint.\nReference terrain: 100 x 100m. Estate landscape: 120 x 110m.\nOriginal furnished main villa retained; 6 new furnished rooms plus 2 garden pavilions.\nActive renderers: {renderers.Length}\nMissing/unsupported material slots: {missing}\nLocal light candidates: {budget.localLights.Length}; runtime max active: {budget.maxActive}; runtime max local shadows: {budget.maxShadowed}\nPer-frame reflection cameras: 0\nGPU performance: NOT MEASURED for this expanded scene.\n");
        }
    }
}
