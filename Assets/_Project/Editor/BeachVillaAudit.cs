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
    public static class BeachVillaAudit
    {
        const string ScenePath = "Assets/Modern Villa/Scenes/Beach Villa.unity";
        [MenuItem("Rubber/Keep Beach Villa Main Camera")]
        public static void KeepMainCamera()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open Beach Villa outside Play mode first.");
            if (scene.isDirty) throw new InvalidOperationException("Unsaved scene changes exist; camera cleanup stopped to preserve them.");
            var cameras = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
            var main = cameras.Single(c => c.CompareTag("MainCamera"));
            if (!main.gameObject.activeInHierarchy) throw new InvalidOperationException("Main camera is inactive.");
            Directory.CreateDirectory("Docs/BeachVillaAudit");
            var log = new StringBuilder("Camera cleanup: " + DateTime.UtcNow.ToString("O") + "\n");
            foreach (var camera in cameras)
            {
                log.AppendLine($"{Hierarchy(camera)}: enabled {camera.enabled} -> {camera == main}; transform preserved");
                Undo.RecordObject(camera, "Keep Beach Villa Main Camera");
                camera.enabled = camera == main;
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            foreach (var listener in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)))
            {
                log.AppendLine($"AudioListener {Hierarchy(listener)}: enabled {listener.enabled} -> {listener.gameObject == main.gameObject}");
                Undo.RecordObject(listener, "Keep main camera audio listener");
                listener.enabled = listener.gameObject == main.gameObject;
                PrefabUtility.RecordPrefabInstancePropertyModifications(listener);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
            log.AppendLine($"Active enabled cameras after cleanup: {cameras.Count(c => c.isActiveAndEnabled)}");
            File.WriteAllText("Docs/BeachVillaAudit/camera-cleanup.txt", log.ToString());
            if (File.Exists("Docs/BeachVillaAudit/inventory.txt") && !File.Exists("Docs/BeachVillaAudit/inventory-before-camera-cleanup.txt"))
                File.Copy("Docs/BeachVillaAudit/inventory.txt", "Docs/BeachVillaAudit/inventory-before-camera-cleanup.txt");
            Run();
        }
        static string Hierarchy(Component c) { string p = c.name; for (var t = c.transform.parent; t != null; t = t.parent) p = t.name + "/" + p; return p; }
        [MenuItem("Rubber/Audit Beach Villa")]
        public static void Run()
        {
            var scene = SceneManager.GetActiveScene();
            bool preview = scene.path != ScenePath;
            if (preview) scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                var active = all.Where(t => t.gameObject.activeInHierarchy).ToArray();
                var renderers = active.SelectMany(t => t.GetComponents<Renderer>()).Where(r => r.enabled).ToArray();
                var meshes = renderers.Select(r => r.GetComponent<MeshFilter>()?.sharedMesh ?? (r as SkinnedMeshRenderer)?.sharedMesh).Where(m => m != null).ToArray();
                var mats = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
                var textures = mats.SelectMany(m => m.GetTexturePropertyNames().Select(n => m.GetTexture(n))).Where(t => t != null).Distinct().ToArray();
                var sb = new StringBuilder();
                sb.AppendLine($"Scene={scene.path}; preview={preview}; unsaved={scene.isDirty}; UTC={DateTime.UtcNow:O}");
                sb.AppendLine($"Objects total={all.Length}; active={active.Length}; active enabled renderers={renderers.Length}; unique meshes={meshes.Distinct().Count()}; mesh instances={meshes.Length}; materials={mats.Length}; textures={textures.Length}");
                long Triangles(Mesh m) { long n = 0; for (int i = 0; i < m.subMeshCount; i++) if (m.GetTopology(i) == MeshTopology.Triangles) n += (long)m.GetIndexCount(i) / 3; return n; }
                sb.AppendLine($"Mesh-instance triangle sum={meshes.Sum(Triangles)}; excludes terrain and visibility/LOD culling; NOT per-frame triangles.");
                sb.AppendLine($"Renderers shadow-casting={renderers.Count(r => r.shadowCastingMode != ShadowCastingMode.Off)}; receive-shadows={renderers.Count(r => r.receiveShadows)}; assigned lightmap index={renderers.Count(r => r.lightmapIndex >= 0 && r.lightmapIndex < 65534)}");
                foreach (StaticEditorFlags flag in new[] { StaticEditorFlags.BatchingStatic, StaticEditorFlags.ContributeGI, StaticEditorFlags.OccluderStatic, StaticEditorFlags.OccludeeStatic })
                    sb.AppendLine($"Renderers {flag}={renderers.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, flag))}");
                sb.AppendLine($"LODGroups={active.Sum(t => t.GetComponents<LODGroup>().Length)}; rigidbodies={active.Sum(t => t.GetComponents<Rigidbody>().Length)}; animators={active.Sum(t => t.GetComponents<Animator>().Length)}; particle systems={active.Sum(t => t.GetComponents<ParticleSystem>().Length)}; missing scripts={all.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))}");
                foreach (var group in active.SelectMany(t => t.GetComponents<Collider>()).GroupBy(c => c.GetType().Name))
                    sb.AppendLine($"Collider {group.Key}={group.Count()}; enabled={group.Count(c => c.enabled)}; triggers={group.Count(c => c.isTrigger)}");
                foreach (var root in roots)
                {
                    var rr = root.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                    var b = rr.Length > 0 ? rr[0].bounds : new Bounds(root.transform.position, Vector3.zero);
                    foreach (var r in rr) b.Encapsulate(r.bounds);
                    sb.AppendLine($"ROOT {root.name}: active={root.activeSelf}; objects={root.GetComponentsInChildren<Transform>(true).Length}; renderers={rr.Length}; center={b.center}; size={b.size}");
                }
                foreach (var light in all.SelectMany(t => t.GetComponents<Light>()))
                    sb.AppendLine($"LIGHT {Hierarchy(light)}: active={light.isActiveAndEnabled}; type={light.type}; bake={light.lightmapBakeType}; intensity={light.intensity}; range={light.range}; shadows={light.shadows}; resolution={light.shadowResolution}; color={light.color}; bakedOutput={light.bakingOutput.isBaked}; mixedOutput={light.bakingOutput.mixedLightingMode}");
                foreach (var probe in all.SelectMany(t => t.GetComponents<ReflectionProbe>()))
                    sb.AppendLine($"REFLECTION {Hierarchy(probe)}: active={probe.isActiveAndEnabled}; mode={probe.mode}; refresh={probe.refreshMode}; timeSlicing={probe.timeSlicingMode}; resolution={probe.resolution}; boxProjection={probe.boxProjection}; size={probe.size}; bakedTexture={AssetDatabase.GetAssetPath(probe.bakedTexture)}");
                foreach (var probe in all.SelectMany(t => t.GetComponents<LightProbeGroup>())) sb.AppendLine($"LIGHTPROBES {Hierarchy(probe)}: count={probe.probePositions.Length}");
                foreach (var camera in all.SelectMany(t => t.GetComponents<Camera>())) sb.AppendLine($"CAMERA {Hierarchy(camera)}: enabled={camera.isActiveAndEnabled}; clip={camera.nearClipPlane}..{camera.farClipPlane}; occlusion={camera.useOcclusionCulling}; HDR={camera.allowHDR}; MSAA={camera.allowMSAA}; display={camera.targetDisplay}; target={camera.targetTexture?.name ?? "screen"}; depth={camera.depth}; rect={camera.rect}; mask={camera.cullingMask}; clear={camera.clearFlags}; cameraType={camera.cameraType}; URPdata={camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() != null}");
                foreach (var g in active.SelectMany(t => t.GetComponents<LODGroup>()).GroupBy(g => $"levels={g.lodCount}; enabled={g.enabled}; fade={g.fadeMode}; thresholds={string.Join(",", g.GetLODs().Select(l => l.screenRelativeTransitionHeight.ToString("F3")))}")) sb.AppendLine($"LOD {g.Key}: groups={g.Count()}");
                var mc = active.SelectMany(t => t.GetComponents<MeshCollider>()).ToArray();
                sb.AppendLine($"MESHCOLLIDER convex={mc.Count(c => c.convex)}; nonconvex={mc.Count(c => !c.convex)}; triangles={mc.Where(c => c.sharedMesh != null).Sum(c => Triangles(c.sharedMesh))}");
                sb.AppendLine($"TEXTURE SUMMARY editor runtime bytes={textures.Sum(t => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t))}; >=4k={textures.Count(t => t.width >= 4096 || t.height >= 4096)}; 2k={textures.Count(t => Math.Max(t.width,t.height) == 2048)}; 1k={textures.Count(t => Math.Max(t.width,t.height) == 1024)}");
                foreach (var t in active.SelectMany(t => t.GetComponents<Terrain>())) sb.AppendLine($"TERRAIN {Hierarchy(t)}: size={t.terrainData.size}; heightmap={t.terrainData.heightmapResolution}; pixelError={t.heightmapPixelError}; instanced={t.drawInstanced}; detailDistance={t.detailObjectDistance}; treeDistance={t.treeDistance}; trees={t.terrainData.treeInstanceCount}; detailPrototypes={t.terrainData.detailPrototypes.Length}");
                foreach (var g in all.SelectMany(t => t.GetComponents<MonoBehaviour>()).Where(m => m != null).GroupBy(m => m.GetType().FullName)) sb.AppendLine($"SCRIPT {g.Key}: {g.Count()}");
                foreach (var m in mats) sb.AppendLine($"MATERIAL {AssetDatabase.GetAssetPath(m)}: shader={m.shader.name}; supported={m.shader.isSupported}; instancing={m.enableInstancing}; queue={m.renderQueue}; keywords={string.Join(",", m.enabledKeywords.Select(k => k.name))}");
                foreach (var m in meshes.GroupBy(m => m).OrderByDescending(g => Triangles(g.Key) * g.Count()).Take(25)) sb.AppendLine($"MESH {m.Key.name}: instances={m.Count()}; trianglesEach={Triangles(m.Key)}; total={Triangles(m.Key) * m.Count()}; path={AssetDatabase.GetAssetPath(m.Key)}");
                foreach (var t in textures.OrderByDescending(t => (long)t.width * t.height))
                {
                    var path = AssetDatabase.GetAssetPath(t); var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    sb.AppendLine($"TEXTURE {path}: {t.width}x{t.height}; runtimeBytes={UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)}; mipmaps={importer?.mipmapEnabled}; streaming={importer?.streamingMipmaps}; readable={importer?.isReadable}; compression={importer?.textureCompression}");
                }
                if (!preview)
                {
                    sb.AppendLine($"LOADED LIGHTMAPS={LightmapSettings.lightmaps.Length}; mode={LightmapSettings.lightmapsMode}; lightProbeCount={LightmapSettings.lightProbes?.count}; pipeline={QualitySettings.renderPipeline?.name}; quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}");
                    foreach (var lm in LightmapSettings.lightmaps) sb.AppendLine($"LIGHTMAP color={AssetDatabase.GetAssetPath(lm.lightmapColor)}; directional={AssetDatabase.GetAssetPath(lm.lightmapDir)}; shadowMask={AssetDatabase.GetAssetPath(lm.shadowMask)}; size={lm.lightmapColor?.width}x{lm.lightmapColor?.height}");
                }
                Directory.CreateDirectory("Docs/BeachVillaAudit");
                File.WriteAllText("Docs/BeachVillaAudit/inventory.txt", sb.ToString());
                Debug.Log("Beach Villa read-only audit complete: Docs/BeachVillaAudit/inventory.txt");
            }
            finally { if (preview) EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
