using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    [InitializeOnLoad]
    public static class ModernVillaSurfaceAudit
    {
        public const string Folder="Docs/ModernVillaSurfaceAudit";
        const string Request="Library/ModernVillaSurfaceAudit.request";
        static double next;
        static ModernVillaSurfaceAudit(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            next=EditorApplication.timeSinceStartup+1;
            if(!File.Exists(Request))return;
            string command=File.ReadAllText(Request).Trim();File.Delete(Request);
            try
            {
                if(command=="refresh"){AssetDatabase.Refresh();return;}
                if(command=="survey")Survey();
                if(command=="repair")ModernVillaSurfaceRepair.Apply();
                if(command=="traverse")ModernVillaTraversalAudit.Run();
                if(command=="connections")ModernVillaSurfaceConnections.Apply();
                if(command=="gaps")ModernVillaGapAudit.Run();
                if(command=="capture")ModernVillaSurfaceCapture.Run();
                if(command=="finish")ModernVillaSurfaceConnections.Finish();
            }
            catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/error.txt",e.ToString());Debug.LogException(e);}
        }
        public static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        static bool Surface(MeshFilter f)
        {
            string n=f.name.ToLowerInvariant();
            if(n.Contains("lamp"))return false;
            n+=" "+f.sharedMesh.name.ToLowerInvariant();
            return n.Contains("floor") || n.Contains("stair") || n.Contains("roof") || n.Contains("terrace") || n.Contains("paving") || n.Contains("deck") || n.Contains("step") || n.Contains("pool bottom") || n.Contains("pool straight top") || n.Contains("pool corner top");
        }
        [MenuItem("Rubber/Surface Audit/Survey Floors and Stairs")]
        public static void Survey()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ModernVillaSetup.ScenePath || Application.isPlaying || Lightmapping.isRunning)throw new InvalidOperationException("Open Modern Villa in Edit mode with no bake.");
            Directory.CreateDirectory(Folder);Physics.SyncTransforms();
            var meshes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh).ToArray();
            var colliders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).ToArray();
            File.WriteAllLines(Folder+"/all-meshes.tsv",meshes.Select(f=>$"{PathOf(f.transform)}\t{f.sharedMesh.name}\t{f.transform.position:F3}\t{f.GetComponent<Renderer>()?.bounds}"));
            var inventory=new StringBuilder("path\tactive\tmesh\tposition\trotation\tscale\tbounds center\tbounds size\tcolliders\n");
            var issues=new StringBuilder("surface\tpoint\treason\tactual surface\tdistance\n");
            var overlaps=new StringBuilder("surfaceA\tsurfaceB\tcenter\tsize\n");
            var surfaces=meshes.Where(Surface).ToArray();int count=0,missing=0,buried=0;
            foreach(var f in surfaces)
            {
                var r=f.GetComponent<Renderer>();var b=r?r.bounds:new Bounds(f.transform.position,Vector3.zero);
                inventory.AppendLine($"{PathOf(f.transform)}\t{f.gameObject.activeInHierarchy}\t{f.sharedMesh.name}\t{f.transform.position:F3}\t{f.transform.eulerAngles:F2}\t{f.transform.lossyScale:F3}\t{b.center:F3}\t{b.size:F3}\t{string.Join(";",f.GetComponents<Collider>().Select(c=>c.GetType().Name+" enabled="+c.enabled+" trigger="+c.isTrigger))}");
                if(!f.gameObject.activeInHierarchy || !r || !r.enabled)continue;
                var vertices=f.sharedMesh.vertices;var triangles=f.sharedMesh.triangles;var used=new HashSet<Vector3Int>();int sampled=0,failed=0;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 a=f.transform.TransformPoint(vertices[triangles[i]]),b1=f.transform.TransformPoint(vertices[triangles[i+1]]),c=f.transform.TransformPoint(vertices[triangles[i+2]]);
                    var normal=Vector3.Cross(b1-a,c-a).normalized;if(normal.y<.7f)continue;
                    int n=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(a,b1),Vector3.Distance(a,c),Vector3.Distance(b1,c))/.3f),1,80);
                    for(int u=0;u<n;u++)for(int v=0;v<n-u;v++)
                    {
                        var p=a+(b1-a)*((u+.33f)/n)+(c-a)*((v+.33f)/n);
                        var key=new Vector3Int(Mathf.RoundToInt(p.x/.12f),Mathf.RoundToInt(p.y/.04f),Mathf.RoundToInt(p.z/.12f));if(!used.Add(key))continue;
                        count++;sampled++;
                        var hits=Physics.RaycastAll(p+Vector3.up*.12f,Vector3.down,.30f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.65f).OrderBy(h=>Mathf.Abs(h.point.y-p.y)).ToArray();
                        if(hits.Length==0 || Mathf.Abs(hits[0].point.y-p.y)>.07f)
                        {
                            failed++;missing++;
                            if(failed<=30)issues.AppendLine($"{PathOf(f.transform)}\t{p:F3}\tNO_MATCH\t{(hits.Length==0?"none":PathOf(hits[0].collider.transform))}\t{(hits.Length==0?999:Mathf.Abs(hits[0].point.y-p.y)):F3}");
                        }
                    }
                }
                if(failed>0)issues.AppendLine($"SUMMARY {PathOf(f.transform)}\t{failed}/{sampled} missing");
            }
            var active=surfaces.Where(f=>f.gameObject.activeInHierarchy&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).ToArray();
            for(int i=0;i<active.Length;i++)for(int j=i+1;j<active.Length;j++)
            {
                var a=active[i].GetComponent<Renderer>().bounds;var b=active[j].GetComponent<Renderer>().bounds;
                float dx=Mathf.Min(a.max.x,b.max.x)-Mathf.Max(a.min.x,b.min.x),dz=Mathf.Min(a.max.z,b.max.z)-Mathf.Max(a.min.z,b.min.z);
                if(dx>.06f&&dz>.06f&&Mathf.Abs(a.max.y-b.max.y)<.025f){buried++;overlaps.AppendLine($"{PathOf(active[i].transform)}\t{PathOf(active[j].transform)}\t{(a.center+b.center)*.5f:F3}\t({dx:F3},{dz:F3})");}
            }
            var cs=new StringBuilder("path\ttype\tenabled\ttrigger\tcenter\tsize\n");
            foreach(var c in colliders)cs.AppendLine($"{PathOf(c.transform)}\t{c.GetType().Name}\t{c.enabled&&c.gameObject.activeInHierarchy}\t{c.isTrigger}\t{c.bounds.center:F3}\t{c.bounds.size:F3}");
            File.WriteAllText(Folder+"/surfaces.tsv",inventory.ToString());File.WriteAllText(Folder+"/missing-support.tsv",issues.ToString());File.WriteAllText(Folder+"/coplanar-candidates.tsv",overlaps.ToString());File.WriteAllText(Folder+"/colliders.tsv",cs.ToString());
            var summary=new StringBuilder($"Scene={scene.path}; dirty={scene.isDirty}; mesh filters={meshes.Length}; surface meshes={surfaces.Length}; colliders={colliders.Length}; samples={count}; missing samples={missing}; coplanar AABB candidates={buried}\n");
            foreach(var root in scene.GetRootGameObjects())summary.AppendLine("ROOT "+root.name);
            foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>())summary.AppendLine($"TERRAIN {PathOf(t.transform)} pos={t.transform.position} size={t.terrainData.size} holes={t.terrainData.holesResolution} collider={t.GetComponent<TerrainCollider>()?.enabled}");
            File.WriteAllText(Folder+"/summary.txt",summary.ToString());
        }
    }
}
