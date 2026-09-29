using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    public static class BeachVillaAPV
    {
        const string ScenePath = "Assets/Modern Villa/Scenes/Beach Villa.unity";
        const string Folder = "Assets/_Project/Lighting/BeachVilla";
        const string Reports = "Docs/BeachVillaAPV";
        static void CheckScene()
        {
            if (SceneManager.GetActiveScene().path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open Beach Villa outside Play mode.");
        }
        [MenuItem("Rubber/APV/1 Configure Beach Villa")]
        public static void Configure()
        {
            CheckScene();
            if (UnityEngine.Object.FindAnyObjectByType<ProbeVolume>() != null) throw new InvalidOperationException("Existing APV configuration: use bake or inspect; do not overwrite.");
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(Reports);
            var scene = SceneManager.GetActiveScene();
            var pipeline = QualitySettings.renderPipeline;
            File.WriteAllText(Reports + "/pipeline-before.json", EditorJsonUtility.ToJson(pipeline, true));
            var so = new SerializedObject(pipeline);
            so.FindProperty("m_LightProbeSystem").intValue = 1;
            so.FindProperty("m_ProbeVolumeMemoryBudget").intValue = 512;
            so.FindProperty("m_ProbeVolumeSHBands").intValue = 1;
            so.FindProperty("m_SupportProbeVolumeScenarios").boolValue = false;
            so.FindProperty("m_SupportProbeVolumeScenarioBlending").boolValue = false;
            so.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(pipeline);
            var settings = UnityEngine.Object.Instantiate(Lightmapping.lightingSettings);
            settings.name = "Beach Villa APV Day";
            settings.bakedGI = true; settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
            AssetDatabase.CreateAsset(settings, Folder + "/Day.lighting");
            Lightmapping.lightingSettings = settings;
            var log = new StringBuilder();
            foreach (var light in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)))
            {
                log.AppendLine($"{light.name}: {light.lightmapBakeType} -> Mixed; intensity={light.intensity}; shadows={light.shadows}");
                Undo.RecordObject(light, "APV indirect lighting");
                light.lightmapBakeType = LightmapBakeType.Mixed;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            }
            File.WriteAllText(Reports + "/light-changes.txt", log.ToString());
            var set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
            // Use this installed Unity version's own initialization, including virtual offset defaults.
            typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(set, null);
            set.minDistanceBetweenProbes = .75f; set.simplificationLevels = 2;
            set.minRendererVolumeSize = .1f;
            AssetDatabase.CreateAsset(set, Folder + "/BeachVillaBakingSet.asset");
            if (!set.TryAddScene(AssetDatabase.AssetPathToGUID(ScenePath))) throw new InvalidOperationException("Scene already belongs to a baking set.");
            var outer = new GameObject("APV - terrace and pool").AddComponent<ProbeVolume>();
            Undo.RegisterCreatedObjectUndo(outer.gameObject, "Create Beach Villa APV");
            outer.mode = ProbeVolume.Mode.Local;
            outer.transform.position = new Vector3(2, 3, 12); outer.size = new Vector3(44, 12, 60);
            outer.overridesSubdivLevels = true; outer.lowestSubdivLevelOverride = 1; outer.highestSubdivLevelOverride = 2;
            outer.fillEmptySpaces = true;
            var inner = new GameObject("APV - villa interior").AddComponent<ProbeVolume>();
            Undo.RegisterCreatedObjectUndo(inner.gameObject, "Create Beach Villa APV");
            inner.mode = ProbeVolume.Mode.Local;
            inner.transform.position = new Vector3(6.3f, 3.5f, 13); inner.size = new Vector3(18, 9, 27);
            inner.overridesSubdivLevels = true; inner.lowestSubdivLevelOverride = 0; inner.highestSubdivLevelOverride = 1;
            inner.fillEmptySpaces = true;
            AssetDatabase.SaveAssetIfDirty(set);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Reports + "/configuration.txt", "Same Beach Villa scene. Existing lightmaps preserved. APV-only bake.\nVolumes: exterior 44x12x60m at (2,3,12); interior 18x9x27m at (6.3,3.5,13).\nBase spacing 0.75m; exterior minimum 2.25m, interior minimum 0.75m; adaptive placement.\nL1, Low pool budget (enum 512; NOT 512MB), no scenario blending.\nMixed lights with Baked Indirect. Original intensities/shadows preserved.\n");
        }
        [MenuItem("Rubber/APV/2 Bake Day Probes")]
        public static void Bake()
        {
            CheckScene();
            Directory.CreateDirectory(Reports);
            bool started = AdaptiveProbeVolumes.BakeAsync();
            File.WriteAllText(Reports + "/bake-status.txt", "Started=" + started + " UTC=" + DateTime.UtcNow.ToString("O"));
            if (!started) throw new InvalidOperationException("APV bake did not start.");
            EditorApplication.update -= ObserveBake; EditorApplication.update += ObserveBake;
        }
        static void ObserveBake()
        {
            if (AdaptiveProbeVolumes.isRunning || Lightmapping.isRunning) return;
            EditorApplication.update -= ObserveBake;
            var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Folder + "/BeachVillaBakingSet.asset");
            AssetDatabase.SaveAssetIfDirty(set);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.AppendAllText(Reports + "/bake-status.txt", "\nFinished UTC=" + DateTime.UtcNow.ToString("O") + "; HasBakedData=" + set.HasBakedData());
        }
        [MenuItem("Rubber/APV/3 Repair Chair GI and Rebake")]
        public static void RepairChairs()
        {
            CheckScene();
            var names = new[] { "LoungeChair", "LoungeChair (1)", "mv_LoungeChair", "mv_LoungeChair (1)" };
            var log = new StringBuilder();
            foreach (var r in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => names.Contains(r.name)))
            {
                Undo.RecordObject(r, "Use APV for invalid lightmap UV chair");
                log.AppendLine($"{r.name}: ReceiveGI {r.receiveGI} -> LightProbes; old lightmap index={r.lightmapIndex}");
                r.receiveGI = ReceiveGI.LightProbes; r.lightProbeUsage = LightProbeUsage.BlendProbes; r.lightmapIndex = -1;
                // These four vendor meshes have invalid bake UVs. Receive APV, but do not
                // contribute to its bake until their UVs are repaired in the source asset.
                Undo.RecordObject(r.gameObject, "Exclude invalid UV chair from GI contributors");
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, GameObjectUtility.GetStaticEditorFlags(r.gameObject) & ~StaticEditorFlags.ContributeGI);
                log.AppendLine("ContributeGI disabled for this renderer only; realtime shadows and colliders retained; UV source unchanged.");
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
            }
            File.WriteAllText(Reports + "/chair-repair.txt", log.ToString());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Bake();
        }
        [MenuItem("Rubber/APV/4 Validate and Capture")]
        public static void Validate()
        {
            CheckScene();
            if (AdaptiveProbeVolumes.isRunning) throw new InvalidOperationException("Wait for bake.");
            var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Folder + "/BeachVillaBakingSet.asset");
            if (!set.HasBakedData()) throw new InvalidOperationException("No baked APV data.");
            var container = new GameObject("Temporary APV verification");
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor", new Color(.65f,.65f,.65f,1)); mat.SetFloat("_Smoothness", .1f);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.transform.SetParent(container.transform); sphere.transform.localScale = Vector3.one * .6f;
            sphere.GetComponent<Renderer>().sharedMaterial = mat;
            var camObj = new GameObject("APV review camera"); camObj.transform.SetParent(container.transform);
            var camera = camObj.AddComponent<Camera>(); camera.enabled = false; camera.fieldOfView = 50; camera.nearClipPlane = .08f; camera.farClipPlane = 120;
            var volume = container.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10000;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            var option = profile.Add<ProbeVolumesOptions>(true); option.animateSamplingNoise.Override(false);
            var rt = new RenderTexture(960, 640, 24); var old = RenderTexture.active;
            var report = new StringBuilder($"HasBakedData={set.HasBakedData()}; lightmaps retained={LightmapSettings.lightmaps.Length}; scenarios={string.Join(",",set.lightingScenarios)}\n");
            try
            {
                foreach (var location in new[] { ("interior", new Vector3(6,2.3f,13)), ("entrance", new Vector3(6,1.6f,1)), ("poolside",new Vector3(0,.7f,-7)) })
                {
                    sphere.transform.position = location.Item2;
                    camera.transform.position = location.Item2 + new Vector3(0,.3f,-2.2f); camera.transform.LookAt(location.Item2); camera.targetTexture = rt;
                    Color32[] baseline = null;
                    foreach (var strength in new[] { 0f, 1f })
                    {
                        option.intensityMultiplier.Override(strength);
                        for (int i=0;i<12;i++) { VolumeManager.instance.Update(camera.transform, ~0); camera.Render(); }
                        RenderTexture.active = rt;
                        var image = new Texture2D(960,640,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,960,640),0,0); image.Apply();
                        var pixels = image.GetPixels32();
                        File.WriteAllBytes(Reports+"/"+location.Item1+(strength==0?"-ambient":"-apv")+".png", image.EncodeToPNG());
                        if (strength==0) baseline=pixels;
                        else
                        {
                            double diff=0; int samples=0;
                            for(int y=280;y<360;y++) for(int x=440;x<520;x++) {int i=y*960+x;diff+=Math.Abs(pixels[i].r-baseline[i].r)+Math.Abs(pixels[i].g-baseline[i].g)+Math.Abs(pixels[i].b-baseline[i].b);samples+=3;}
                            report.AppendLine($"{location.Item1}: center 80x80 mean absolute RGB byte difference APV vs ambient={diff/samples:F3}");
                        }
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                }
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(container);UnityEngine.Object.DestroyImmediate(profile);UnityEngine.Object.DestroyImmediate(mat);
            }
            report.AppendLine("Temporary spheres/camera/volume removed. Not an FPS benchmark or exhaustive leak test.");
            File.WriteAllText(Reports+"/validation.txt", report.ToString());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
    }
}
