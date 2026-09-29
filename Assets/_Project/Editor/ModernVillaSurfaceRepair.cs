using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Rubber.EditorTools
{
    public static class ModernVillaSurfaceRepair
    {
        static readonly string Folder=ModernVillaSurfaceAudit.Folder;
        static bool Rectangular(MeshFilter f)
        {
            if(!f.name.ToLowerInvariant().Contains("floor") || f.transform.lossyScale.x<=0 || f.transform.lossyScale.z<=0)return false;
            var vertices=f.sharedMesh.vertices;var triangles=f.sharedMesh.triangles;float area=0;var bounds=f.GetComponent<Renderer>().bounds;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=f.transform.TransformPoint(vertices[triangles[i]]);var b=f.transform.TransformPoint(vertices[triangles[i+1]]);var c=f.transform.TransformPoint(vertices[triangles[i+2]]);
                var cross=Vector3.Cross(b-a,c-a);
                if(cross.normalized.y>.999f&&Mathf.Abs(a.y-bounds.max.y)<.005f)area+=cross.magnitude*.5f;
            }
            return Mathf.Abs(area-bounds.size.x*bounds.size.z)<.015f && Mathf.Abs(Mathf.DeltaAngle(f.transform.eulerAngles.y,Mathf.Round(f.transform.eulerAngles.y/90)*90))<.01f;
        }
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();if(Application.isPlaying||Lightmapping.isRunning||scene.path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Modern Villa Edit mode required.");
            Directory.CreateDirectory(Folder);
            if(!File.Exists(Folder+"/BeforeRepair.unity.backup")){EditorSceneManager.SaveScene(scene);File.Copy(scene.path,Folder+"/BeforeRepair.unity.backup");foreach(var f in new[]{"surfaces.tsv","missing-support.tsv","coplanar-candidates.tsv","summary.txt","colliders.tsv"})if(File.Exists(Folder+"/"+f))File.Copy(Folder+"/"+f,Folder+"/before-"+f,true);}
            var report=new StringBuilder();
            var meshes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()).ToArray();
            var pool=meshes.Single(f=>ModernVillaSurfaceAudit.PathOf(f.transform)=="Backyard/Pool back/Pool2/Swimming Pool Steps/Swimming Pool Steps Floor");
            if(!pool.GetComponent<Collider>())
            {var c=Undo.AddComponent<MeshCollider>(pool.gameObject);c.sharedMesh=pool.sharedMesh;report.AppendLine("ADD exact static MeshCollider: "+ModernVillaSurfaceAudit.PathOf(pool.transform));}
            var floors=meshes.Where(Rectangular).ToArray();
            for(int iteration=0;iteration<3;iteration++)
            for(int i=0;i<floors.Length;i++)for(int j=i+1;j<floors.Length;j++)
            {
                var a=floors[i];var b=floors[j];if(!a.gameObject.activeInHierarchy||!b.gameObject.activeInHierarchy)continue;
                var ba=a.GetComponent<Renderer>().bounds;var bb=b.GetComponent<Renderer>().bounds;
                float dx=Mathf.Min(ba.max.x,bb.max.x)-Mathf.Max(ba.min.x,bb.min.x),dz=Mathf.Min(ba.max.z,bb.max.z)-Mathf.Max(ba.min.z,bb.min.z);
                if(dx<.01f||dz<.01f||Mathf.Abs(ba.max.y-bb.max.y)>.005f)continue;
                if((ba.center-bb.center).sqrMagnitude<1e-8f&&(ba.size-bb.size).sqrMagnitude<1e-8f&&a.sharedMesh==b.sharedMesh && a.transform.childCount==0&&b.transform.childCount==0)
                {Undo.RecordObject(b.gameObject,"Disable duplicate floor");b.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(b.gameObject);report.AppendLine("DISABLE exact duplicate: "+ModernVillaSurfaceAudit.PathOf(b.transform));continue;}
                if(TryTrim(a,ba,bb,report)||TryTrim(b,bb,ba,report))Physics.SyncTransforms();
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.AppendAllText(Folder+"/repairs.txt",report.ToString());ModernVillaSurfaceAudit.Survey();
        }
        static bool TryTrim(MeshFilter f,Bounds a,Bounds b,StringBuilder report)
        {
            for(int axis=0;axis<2;axis++)
            {
                int k=axis==0?0:2,other=axis==0?2:0;
                if(b.min[other]>a.min[other]+.005f||b.max[other]<a.max[other]-.005f)continue;
                float min=a.min[k],max=a.max[k];
                if(b.min[k]>min+.01f&&b.min[k]<max-.01f&&b.max[k]>=max-.005f)max=b.min[k];
                else if(b.max[k]<max-.01f&&b.max[k]>min+.01f&&b.min[k]<=min+.005f)min=b.max[k];
                else continue;
                float newSize=max-min;if(newSize<.1f)continue;
                var t=f.transform;Undo.RecordObject(t,"Align floor seam");
                int localAxis=Mathf.Abs(Vector3.Dot(t.right,axis==0?Vector3.right:Vector3.forward))>.99f?0:2;
                var scale=t.localScale;scale[localAxis]*=newSize/a.size[k];t.localScale=scale;
                var newBounds=f.GetComponent<Renderer>().bounds;var delta=Vector3.zero;delta[k]=(min+max)*.5f-newBounds.center[k];t.position+=delta;
                PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                report.AppendLine($"TRIM {ModernVillaSurfaceAudit.PathOf(t)} world axis={k} from={a.size[k]:F3} to={newSize:F3}; keep outer edge and meet neighbour; floor top={a.max.y:F3}");return true;
            }
            return false;
        }
    }
}
