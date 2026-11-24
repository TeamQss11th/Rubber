using System;
using System.Collections;
using System.IO;
using System.Linq;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    [InitializeOnLoad]
    public static class BeachVillaNightBake
    {
        const string ScenePath = "Assets/Modern Villa/Scenes/Beach Villa.unity";
        const string SetPath = "Assets/_Project/Lighting/BeachVilla/BeachVillaBakingSet.asset";
        const string Pending = "Rubber.NightBake.Pending";
        const string Report = "Docs/BeachVillaNightBake";
        static Material temporarySky;
        static BeachVillaNightBake()
        {
            if (SessionState.GetBool(Pending, false)) EditorApplication.update += Observe;
        }
        [MenuItem("Rubber/Night Bake/1 Bake Night APV")]
        public static void Bake()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning || AdaptiveProbeVolumes.isRunning)
                throw new InvalidOperationException("Open Beach Villa outside Play mode and wait for any bake.");
            var controller = UnityEngine.Object.FindAnyObjectByType<BeachVillaDarkNight>();
            var walk = UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            if (!controller || !walk) throw new InvalidOperationException("Night and walkthrough components required.");
            var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(SetPath);
            if (!set || !set.HasBakedData()) throw new InvalidOperationException("Existing day APV required.");
            if (set.lightingScenarios.Contains("Night")) throw new InvalidOperationException("Night already configured. Inspect its result before rebaking.");
            var flash = walk.GetComponent<BeachVillaTestFlashlight>();
            if (!flash) flash = Undo.AddComponent<BeachVillaTestFlashlight>(walk.gameObject);
            flash.view = walk.view;
            controller.nightProbesReady = false;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot preserve day scene.");
            Directory.CreateDirectory(Report);
            string backup = "Library/BeachVillaBeforeNightBake-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(ScenePath, backup + "/Beach Villa.unity");
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(SetPath))) File.Copy(file, backup + "/" + Path.GetFileName(file));
            var pipeline = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
            var rp = new SerializedObject(pipeline);
            rp.FindProperty("m_SupportProbeVolumeScenarios").boolValue = true;
            rp.FindProperty("m_SupportProbeVolumeScenarioBlending").boolValue = false;
            rp.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(pipeline);
            set.TryAddScenario("Night");
            var serializedSet = new SerializedObject(set);
            serializedSet.FindProperty("freezePlacement").boolValue = true;
            serializedSet.FindProperty("lightingScenario").stringValue = "Night";
            serializedSet.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(set);
            // Keep the saved day scene on disk; bake the temporary night illumination in memory.
            foreach (var light in controller.sceneLights)
            {
                if (!light) continue;
                if (light.type == LightType.Directional) light.enabled = false;
                else light.color = new Color(1, .67f, .37f);
            }
            if (RenderSettings.skybox)
            {
                temporarySky = new Material(RenderSettings.skybox) { hideFlags = HideFlags.DontSave };
                if (temporarySky.HasProperty("_Exposure")) temporarySky.SetFloat("_Exposure", .001f);
                if (temporarySky.HasProperty("_Tint")) temporarySky.SetColor("_Tint", new Color(.15f, .2f, .3f));
                RenderSettings.skybox = temporarySky;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.0001f, .00015f, .00025f);
            RenderSettings.ambientIntensity = 0;
            RenderSettings.reflectionIntensity = 0;
            File.WriteAllText(Report + "/bake-status.txt", "Started UTC=" + DateTime.UtcNow.ToString("O") + "\nDay backup=" + backup + "\nFrozen placement; L1; no blending; APV-only; existing day lightmaps preserved.\n");
            SessionState.SetBool(Pending, true);
            try
            {
                if (!AdaptiveProbeVolumes.BakeAsync()) throw new InvalidOperationException("Night APV bake did not start.");
                EditorApplication.update -= Observe; EditorApplication.update += Observe;
            }
            catch { Restore(false); throw; }
        }
        static void Observe()
        {
            if (AdaptiveProbeVolumes.isRunning || Lightmapping.isRunning || EditorApplication.isCompiling) return;
            var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(SetPath);
            // A scenario entry with a real stream asset is required; HasBakedData alone may refer to day.
            var so = new SerializedObject(set);
            var scenarios = so.FindProperty("scenarios");
            bool baked = false;
            if (scenarios != null)
            {
                var keys = scenarios.FindPropertyRelative("m_Keys");
                var values = scenarios.FindPropertyRelative("m_Values");
                for (int i = 0; keys != null && i < keys.arraySize; i++)
                    if (keys.GetArrayElementAtIndex(i).stringValue == "Night")
                    {
                        var stream = values.GetArrayElementAtIndex(i).FindPropertyRelative("cellDataAsset");
                        string guid = stream?.FindPropertyRelative("m_AssetGUID")?.stringValue;
                        string path = string.IsNullOrEmpty(guid) ? "" : AssetDatabase.GUIDToAssetPath(guid);
                        baked = File.Exists(path) && new FileInfo(path).Length > 0;
                    }
            }
            // Inspect serialized scenario data in the report as well as the generated files.
            File.WriteAllText(Report + "/baking-set.json", EditorJsonUtility.ToJson(set, true));
            Restore(baked);
        }
        static void Restore(bool baked)
        {
            EditorApplication.update -= Observe; SessionState.SetBool(Pending, false);
            var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(SetPath);
            var so = new SerializedObject(set); so.FindProperty("lightingScenario").stringValue = "Default";
            so.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(set);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (temporarySky) UnityEngine.Object.DestroyImmediate(temporarySky);
            var controller = UnityEngine.Object.FindAnyObjectByType<BeachVillaDarkNight>();
            controller.nightProbesReady = baked;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.AppendAllText(Report + "/bake-status.txt", "Completed UTC=" + DateTime.UtcNow.ToString("O") + "; NightData=" + baked + "; day lightmaps=" + LightmapSettings.lightmaps.Length + "\n");
        }
        [MenuItem("Rubber/Night Bake/2 Flashlight On")]
        public static void FlashOn() { UnityEngine.Object.FindAnyObjectByType<BeachVillaTestFlashlight>()?.SetOn(true); }
        [MenuItem("Rubber/Night Bake/3 Flashlight Off")]
        public static void FlashOff() { UnityEngine.Object.FindAnyObjectByType<BeachVillaTestFlashlight>()?.SetOn(false); }
        [MenuItem("Rubber/Night Bake/4 Review In Play Mode")]
        public static void Review()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var night = UnityEngine.Object.FindAnyObjectByType<BeachVillaDarkNight>();
            night.StartCoroutine(CaptureReview(night));
        }
        static IEnumerator CaptureReview(BeachVillaDarkNight night)
        {
            var walk = night.GetComponent<BeachVillaWalkthrough>();
            var flash = night.GetComponent<BeachVillaTestFlashlight>();
            var position = walk.transform.position; var rotation = walk.transform.rotation;
            var look = walk.view.localRotation;
            bool wasNight = night.IsNight, wasOn = flash.Beam && flash.Beam.enabled, wasWalking = walk.enabled;
            night.SetNight(true);
            var volume = night.GetComponentsInChildren<Volume>().First(v => v.sharedProfile && v.sharedProfile.Has<ProbeVolumesOptions>());
            volume.sharedProfile.TryGet<ProbeVolumesOptions>(out var options);
            walk.enabled = false;
            Directory.CreateDirectory(Report);
            try
            {
                File.WriteAllText(Report + "/review.txt", "Normal Game View validation; same pose per comparison.\n");
                foreach (var location in new[] { ("entrance", 1), ("interior", 2) })
                {
                    walk.GoTo(location.Item2);
                    // Slightly downward view includes floor bounce and flashlight footprint.
                    walk.view.localRotation = Quaternion.Euler(12, 0, 0);
                    foreach (var mode in new[] { "direct", "apv", "flashlight" })
                    {
                        options.intensityMultiplier.Override(mode == "direct" ? 0 : 1);
                        flash.SetOn(mode == "flashlight");
                        yield return new WaitForSecondsRealtime(2);
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report + "/" + location.Item1 + "-" + mode + ".png"));
                        File.AppendAllText(Report + "/review.txt", $"{location.Item1}-{mode}: scenario={ProbeReferenceVolume.instance.lightingScenario}, APV={options.intensityMultiplier.value}, flashlight={flash.Beam.enabled}, range={flash.Beam.range}, shadowResolution={flash.Beam.shadowCustomResolution}, tier={flash.Beam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>().additionalLightsShadowResolutionTier}, lightmaps={LightmapSettings.lightmaps.Length}, resolution={Screen.width}x{Screen.height}\n");
                        yield return new WaitForSecondsRealtime(1);
                    }
                }
            }
            finally
            {
                options.intensityMultiplier.Override(1);
                flash.SetOn(wasOn); night.SetNight(wasNight);
                var body = walk.GetComponent<CharacterController>(); body.enabled = false;
                walk.transform.SetPositionAndRotation(position, rotation); walk.view.localRotation = look;
                body.enabled = true; walk.enabled = wasWalking;
            }
            File.AppendAllText(Report + "/review.txt", "Review completed; temporary overrides and camera pose restored. Not a performance benchmark.\n");
        }
    }
}
