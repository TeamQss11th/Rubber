using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rubber.EditorTools
{
    public static class VillaAssetAudit
    {
        [MenuItem("Rubber/Map/Inspect Available Pieces")]
        public static void Inspect()
        {
            var report = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Modern Villa/Prefabs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var renderers = root.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) continue;
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    report.AppendLine($"{path}\tcenter={bounds.center:F2}\tsize={bounds.size:F2}\tlights={root.GetComponentsInChildren<Light>().Length}\tcolliders={root.GetComponentsInChildren<Collider>().Length}");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Directory.CreateDirectory("Docs/MapBuild");
            File.WriteAllText("Docs/MapBuild/asset-bounds.txt", report.ToString());
            var demo = EditorSceneManager.OpenPreviewScene("Assets/Modern Villa/Scenes/Modern Villa.unity");
            var sceneReport = new StringBuilder();
            try
            {
                foreach (var root in demo.GetRootGameObjects())
                {
                    var renderers = root.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var bounds = renderers[0].bounds;
                        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                        sceneReport.AppendLine($"{root.name}\tcenter={bounds.center:F2}\tsize={bounds.size:F2}\trenderers={renderers.Length}");
                    }
                    foreach (var terrain in root.GetComponentsInChildren<Terrain>())
                        sceneReport.AppendLine($"TERRAIN {terrain.name}\tposition={terrain.transform.position:F2}\tsize={terrain.terrainData.size:F2}");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(demo); }
            File.WriteAllText("Docs/MapBuild/modern-villa-reference-bounds.txt", sceneReport.ToString());
            Debug.Log("Rubber: asset bounds written to Docs/MapBuild/asset-bounds.txt");
        }
    }
}
