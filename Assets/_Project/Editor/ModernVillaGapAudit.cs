using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Rubber.EditorTools
{
    public static class ModernVillaGapAudit
    {
        static List<float> Heights(MeshFilter m,float x,float z)
        {
            var result=new List<float>();var vs=m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v)).ToArray();var ts=m.sharedMesh.triangles;
            for(int i=0;i<ts.Length;i+=3){var a=vs[ts[i]];var b=vs[ts[i+1]];var c=vs[ts[i+2]];if(Vector3.Cross(b-a,c-a).normalized.y<.7f)continue;
                float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<1e-8f)continue;
                float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/den,v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/den;
                if(u>=0&&v>=0&&u+v<=1)result.Add(u*a.y+v*b.y+(1-u-v)*c.y);
            }return result;
        }
        public static void Run()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");var report=new StringBuilder();var meshes=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).GroupBy(m=>ModernVillaSurfaceAudit.PathOf(m.transform)).ToDictionary(g=>g.Key,g=>g.First());
            foreach(var line in File.ReadAllLines(ModernVillaSurfaceAudit.Folder+"/coplanar-candidates.tsv").Skip(1))
            {
                var p=line.Split('\t');var a=meshes[p[0]];var b=meshes[p[1]];var ab=a.GetComponent<Renderer>().bounds;var bb=b.GetComponent<Renderer>().bounds;int n=0,total=0;
                for(float x=Mathf.Max(ab.min.x,bb.min.x)+.037f;x<Mathf.Min(ab.max.x,bb.max.x);x+=.1f)for(float z=Mathf.Max(ab.min.z,bb.min.z)+.043f;z<Mathf.Min(ab.max.z,bb.max.z);z+=.1f){var ah=Heights(a,x,z);var bh=Heights(b,x,z);total++;if(ah.Any(y=>bh.Any(h=>Mathf.Abs(h-y)<.003f)))n++;}
                report.AppendLine($"{p[0]} | {p[1]}: same-height top samples={n}/{total}");
            }
            File.WriteAllText(ModernVillaSurfaceAudit.Folder+"/overlap-detail.txt",report.ToString());
            var grid=new Dictionary<Vector2Int,List<float>>();
            for(int xi=0;xi<124;xi++)for(int zi=0;zi<152;zi++)
            {float x=-17.875f+xi*.25f,z=-4.875f+zi*.25f;grid[new Vector2Int(xi,zi)]=Physics.RaycastAll(new Vector3(x,5,z),Vector3.down,8,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.7f&&ModernVillaTraversalAudit.Ground(h.collider)&&!(h.collider is TerrainCollider)).Select(h=>h.point.y).ToList();}
            var gaps=new StringBuilder();int count=0;
            foreach(var kv in grid)foreach(var dir in new[]{Vector2Int.right,Vector2Int.up})
            {
                if(!grid.TryGetValue(kv.Key-dir*2,out var left)||!grid.TryGetValue(kv.Key+dir*2,out var right))continue;
                foreach(float y in left.Where(y=>y>-.3f&&y<4))
                {if(!right.Any(h=>Mathf.Abs(h-y)<.03f)||kv.Value.Any(h=>Mathf.Abs(h-y)<.1f))continue;
                    var pos=new Vector3(-17.875f+kv.Key.x*.25f,y,-4.875f+kv.Key.y*.25f);
                    // A gap only matters where a player can fit above it; walls/furniture are excluded.
                    if(Physics.CheckCapsule(pos+Vector3.up*.34f,pos+Vector3.up*1.57f,.27f,~0,QueryTriggerInteraction.Ignore))continue;
                    count++;gaps.AppendLine($"{pos:F3} dir={dir} lowerHits="+string.Join(",",kv.Value.Select(h=>h.ToString("F3"))));
                }
            }
            File.WriteAllText(ModernVillaSurfaceAudit.Folder+"/gap-candidates.txt",$"0.25m grid, opposing supports 1m apart, clear player capsule; candidates={count}\n"+gaps);
        }
    }
}


