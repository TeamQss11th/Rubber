using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    public static class ModernVillaSurfaceConnections
    {
        public static void Finish()
        {
            if(Application.isPlaying||Lightmapping.isRunning||SceneManager.GetActiveScene().path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Modern Villa edit mode required");
            var root=Find("Backyard/Pool back/Pool2");var material=root.Find("Bottom").GetComponentInChildren<Renderer>().sharedMaterial;
            var reference=root.Find("Bottom").GetComponentInChildren<MeshFilter>().sharedMesh;var uv=reference.uv;var vertices=reference.vertices;var triangles=reference.triangles;var region=new System.Collections.Generic.List<Vector2>();
            for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized.y>.9f){region.Add(uv[a]);region.Add(uv[b]);region.Add(uv[c]);}}
            var lo=new Vector2(region.Min(v=>v.x),region.Min(v=>v.y));var hi=new Vector2(region.Max(v=>v.x),region.Max(v=>v.y));
            const string meshPath="Assets/_Project/Lighting/ModernVilla/PoolStepTileUV.asset";var adapted=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(!adapted){var source=root.Find("Pool stair lower connection").GetComponentInChildren<MeshFilter>().sharedMesh;adapted=UnityEngine.Object.Instantiate(source);adapted.name="StepBlock - existing pool tile UV";var sourceUV=source.uv;var mn=new Vector2(sourceUV.Min(v=>v.x),sourceUV.Min(v=>v.y));var mx=new Vector2(sourceUV.Max(v=>v.x),sourceUV.Max(v=>v.y));for(int i=0;i<sourceUV.Length;i++)sourceUV[i]=new Vector2(Mathf.Lerp(lo.x+.001f,hi.x-.001f,Mathf.InverseLerp(mn.x,mx.x,sourceUV[i].x)),Mathf.Lerp(lo.y+.001f,hi.y-.001f,Mathf.InverseLerp(mn.y,mx.y,sourceUV[i].y)));adapted.uv=sourceUV;AssetDatabase.CreateAsset(adapted,meshPath);}
            foreach(var r in root.Find("Pool stair lower connection").GetComponentsInChildren<Renderer>()){Undo.RecordObject(r,"Match pool tile material");r.sharedMaterial=material;var filter=r.GetComponent<MeshFilter>();Undo.RecordObject(filter,"Use pool tile atlas region");filter.sharedMesh=adapted;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.AppendAllText(ModernVillaSurfaceAudit.Folder+"/repairs.txt","MATCH seven lower steps to existing pool basin material: "+material.name+"\n");
        }
        static Transform Find(string p)=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>ModernVillaSurfaceAudit.PathOf(t)==p);
        static void Fit(Transform t,float xmin,float xmax,float zmin,float zmax)
        {
            Undo.RecordObject(t,"Align surface connection");var r=t.GetComponent<Renderer>();var b=r.bounds;var s=t.localScale;
            bool xRight=Mathf.Abs(t.right.x)>.99f;s[xRight?0:2]*=(xmax-xmin)/b.size.x;s[xRight?2:0]*=(zmax-zmin)/b.size.z;t.localScale=s;
            b=r.bounds;t.position+=new Vector3((xmin+xmax)*.5f-b.center.x,0,(zmin+zmax)*.5f-b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();if(Application.isPlaying||Lightmapping.isRunning||scene.path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Modern Villa edit mode required");
            var log=new StringBuilder();
            // Fill only verified missing collision on the existing pool surface mesh.
            var lines=File.ReadAllLines(ModernVillaSurfaceAudit.Folder+"/missing-support.tsv").Where(l=>l.StartsWith("SUMMARY "));
            foreach(var line in lines)
            {
                string p=line.Substring(8).Split('\t')[0];if(!p.ToLowerInvariant().Contains("pool"))continue;
                var t=Find(p);var mesh=t.GetComponent<MeshFilter>();if(!mesh)continue;
                if(!t.GetComponents<MeshCollider>().Any(c=>c.enabled&&!c.isTrigger&&c.sharedMesh==mesh.sharedMesh))
                {var c=Undo.AddComponent<MeshCollider>(t.gameObject);c.sharedMesh=mesh.sharedMesh;log.AppendLine("ADD mesh-matched pool surface collision: "+p);}
            }
            var floor=Find("Backyard/Pool back/Pool2/Floor/Floor 2x2 (8)");Fit(floor,-12.5f,-11,20.5f,22.5f);
            var corner=Find("Backyard/Floor 2/Floor 2x2 (20)");
            if(!corner.parent.Find("Floor seam infill"))
            {
                var copy=UnityEngine.Object.Instantiate(corner.gameObject,corner.parent);Undo.RegisterCreatedObjectUndo(copy,"Complete corner floor seam");copy.name="Floor seam infill";
                Fit(copy.transform,2,3,20.5f,22.25f);Fit(corner,1,2,20.5f,22.5f);log.AppendLine("SPLIT corner overlap into two existing floor tiles without changing outer coverage");
            }
            var root=Find("Backyard/Pool back/Pool2");
            for(int i=2;i<=5;i++){var tile=Find("Backyard/Floor 2/Floor 1x1 ("+i+")");var b=tile.GetComponent<Renderer>().bounds;Fit(tile,1.5f,b.max.x,b.min.z,b.max.z);}
            // Existing pool light meshes share the basin's exact top plane. Lift only the fitting by 1 cm.
            foreach(var light in root.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.StartsWith("mv_pool_light")))
            {var t=light.transform;if(Mathf.Abs(t.position.y+1.832f)<.001f){Undo.RecordObject(t,"Separate flush pool light surface");t.position+=Vector3.up*.01f;PrefabUtility.RecordPrefabInstancePropertyModifications(t);log.AppendLine("LIFT pool fitting 0.01m: "+ModernVillaSurfaceAudit.PathOf(t));}}
            if(!root.Find("Pool stair lower connection"))
            {
                var group=new GameObject("Pool stair lower connection");Undo.RegisterCreatedObjectUndo(group,"Connect pool steps to basin");group.transform.SetParent(root,false);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/floors/StepBlock.prefab");
                var mat=Find("Backyard/Pool back/Pool2/Swimming Pool Steps/Swimming Pool Steps Floor").GetComponent<Renderer>().sharedMaterial;
                for(int i=0;i<7;i++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group.transform);go.name="Pool lower step "+(i+1);float top=-.50f-i*.185f;
                    go.transform.rotation=Quaternion.Euler(0,90,0);go.transform.localScale=new Vector3(3.4f,(top+1.80f)/.2f,1);go.transform.position=new Vector3(-1.65f-i*.3f,-1.80f,26.5f);
                    go.GetComponent<Renderer>().sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<Renderer>());
                }
                log.AppendLine("ADD seven existing StepBlock instances, rise 0.185m, tread 0.3m, width 3.4m; support down to pool basin");
            }
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.AppendAllText(ModernVillaSurfaceAudit.Folder+"/repairs.txt",log.ToString());ModernVillaSurfaceAudit.Survey();
        }
    }
}

