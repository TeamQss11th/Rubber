using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    public static class BeachVillaExpansionSurvey
    {
        public const string Reports="Docs/BeachVillaExpansion";
        public static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
        public static Bounds BoundsOf(GameObject go)
        {
            var rr=go.GetComponentsInChildren<Renderer>(true);
            var b=rr.Length>0?rr[0].bounds:new Bounds(go.transform.position,Vector3.zero);
            foreach(var r in rr)b.Encapsulate(r.bounds);
            return b;
        }
        [MenuItem("Rubber/Expansion/1 Survey Assets and Scene")]
        public static void Survey()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode.");
            Directory.CreateDirectory(Reports);
            var sb=new StringBuilder("path\tsize\tcenter\trenderers\tcolliders\tLOD groups\tmissing materials\n");
            foreach(var path in AssetDatabase.FindAssets("t:GameObject",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p=>p))
            {
                var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!go)continue;
                var b=BoundsOf(go);var rr=go.GetComponentsInChildren<Renderer>(true);
                sb.AppendLine($"{path}\t{b.size:F3}\t{b.center:F3}\t{rr.Length}\t{go.GetComponentsInChildren<Collider>(true).Length}\t{go.GetComponentsInChildren<LODGroup>(true).Length}\t{rr.Sum(r=>r.sharedMaterials.Count(m=>!m))}");
            }
            File.WriteAllText(Reports+"/all-assets.tsv",sb.ToString());
            sb.Clear();var scene=SceneManager.GetActiveScene();
            foreach(var root in scene.GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=> t.parent==null || t.parent==root.transform || t.name.ToLowerInvariant().Contains("stair") || t.name.ToLowerInvariant().Contains("door")))
            {
                var b=BoundsOf(t.gameObject);
                sb.AppendLine($"{PathOf(t)}\tpos={t.position:F3}\trot={t.eulerAngles:F1}\tcenter={b.center:F3}\tsize={b.size:F3}");
            }
            File.WriteAllText(Reports+"/scene-structure.tsv",sb.ToString());
            sb.Clear();Physics.SyncTransforms();
            foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Collider>(true))
                sb.AppendLine($"{PathOf(c.transform)}\t{c.GetType().Name}\tenabled={c.enabled}\ttrigger={c.isTrigger}\tcenter={c.bounds.center:F3}\tsize={c.bounds.size:F3}");
            File.WriteAllText(Reports+"/colliders-before.tsv",sb.ToString());
            sb.Clear();var terrain=UnityEngine.Object.FindAnyObjectByType<Terrain>();
            sb.AppendLine($"Terrain pos={terrain.transform.position}; size={terrain.terrainData.size}; asset={AssetDatabase.GetAssetPath(terrain.terrainData)}; layers={string.Join(",",terrain.terrainData.terrainLayers.Select(l=>l.name))}");
            for(int z=-25;z<=55;z+=5)for(int x=-45;x<=45;x+=5)
                sb.AppendLine($"{x},{z}\tground={terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y:F3}");
            File.WriteAllText(Reports+"/terrain-before.tsv",sb.ToString());
            Debug.Log("Expansion survey completed: "+Reports);
        }
    }
}
