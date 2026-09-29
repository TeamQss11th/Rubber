using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Rubber.World;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaPoolSetup
    {
        const string Folder="Assets/_Project/Art/PoolPrototype";
        static VillaPoolSetup(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/VillaPool.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;string command=File.ReadAllText(request).Trim();File.Delete(request);try{if(command=="setup")Setup();if(command=="second")VillaSecondPoolSetup.Install();if(command=="surface")VillaPoolSurfaceSettings.Apply();if(command=="test")VillaPoolChecks.Run();if(command=="stop")EditorApplication.isPlaying=false;}catch(Exception e){Directory.CreateDirectory("Docs/VillaPool");File.WriteAllText("Docs/VillaPool/error.txt",e.ToString());Debug.LogException(e);}}
        [MenuItem("Rubber/Pool/Install First Water Test")]
        public static void Setup()
        {
            var scene=SceneManager.GetActiveScene();if(Application.isPlaying||Lightmapping.isRunning||scene.path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Modern Villa Edit mode required");
            if(GameObject.Find("Modern Villa - Pool Water Test"))throw new InvalidOperationException("Pool test already exists; preserve current configuration.");
            Directory.CreateDirectory("Docs/VillaPool");Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(scene);if(!File.Exists("Docs/VillaPool/BeforeWater.unity.backup"))File.Copy(scene.path,"Docs/VillaPool/BeforeWater.unity.backup");
            var root=new GameObject("Modern Villa - Pool Water Test");Undo.RegisterCreatedObjectUndo(root,"Add pool water prototype");var water=root.AddComponent<VillaPoolWater>();root.transform.position=new Vector3(0,.1f,0);
            water.origin=new Vector2(-14.1f,22.1f);water.columns=157;water.rows=94;water.wetCells=new bool[water.columns*water.rows];
            Physics.SyncTransforms();var vertex=new List<Vector3>();var tris=new List<int>();
            bool Wet(float x,float z)
            {
                var hits=Physics.RaycastAll(new Vector3(x,.13f,z),Vector3.down,2.3f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
                foreach(var h in hits){string p=ModernVillaSurfaceAudit.PathOf(h.collider.transform);if(p.Contains("Pool Water Test"))continue;return p.StartsWith("Backyard/Pool back/Pool2/")&&h.point.y<.085f&&h.normal.y>.2f;}return false;
            }
            for(int z=0;z<water.rows;z++)for(int x=0;x<water.columns;x++)
            {
                float px=water.origin.x+x*.1f,pz=water.origin.y+z*.1f;
                if(!Wet(px+.002f,pz+.002f)||!Wet(px+.098f,pz+.002f)||!Wet(px+.002f,pz+.098f)||!Wet(px+.098f,pz+.098f))continue;
                water.wetCells[z*water.columns+x]=true;int k=vertex.Count;vertex.Add(new Vector3(px,0,pz));vertex.Add(new Vector3(px,0,pz+.1f));vertex.Add(new Vector3(px+.1f,0,pz+.1f));vertex.Add(new Vector3(px+.1f,0,pz));tris.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            }
            var mesh=new Mesh{name="Pool water footprint",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertex);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/PoolWaterSurface.asset");
            var surface=new GameObject("Water surface");surface.transform.SetParent(root.transform,false);surface.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=surface.AddComponent<MeshRenderer>();var material=new Material(Shader.Find("Rubber/Pool Water"));AssetDatabase.CreateAsset(material,Folder+"/PoolWater.mat");renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;water.surface=renderer;
            var volume=root.AddComponent<BoxCollider>();volume.isTrigger=true;volume.center=new Vector3(-6.25f,-1,26.8f);volume.size=new Vector3(15.7f,2,9.4f);
            var ducks=new List<VillaDuckBuoyancy>();var report=new List<string>();
            var files=Directory.GetFiles("Assets/_Project/Prefabs/Ducks","*.fbx").OrderBy(p=>p).ToArray();
            for(int i=0;i<files.Length;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(files[i].Replace('\\','/'));var go=new GameObject("Water test - "+source.name);go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(-3.6f-i*.8f,.8f,26.5f);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,go.transform);var rs=visual.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("Duck has no renderer");var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);report.Add(source.name+" original bounds="+b+" materials="+string.Join(",",rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct()));
                float scale=.32f/Mathf.Max(b.size.x,b.size.z);visual.transform.localScale*=scale;b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);visual.transform.position+=go.transform.position-new Vector3(b.center.x,b.min.y,b.center.z);
                var rb=go.AddComponent<Rigidbody>();rb.mass=.12f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearDamping=.05f;rb.angularDamping=.4f;rb.centerOfMass=new Vector3(0,.045f,0);
                var collider=go.AddComponent<BoxCollider>();collider.center=new Vector3(0,.07f,0);collider.size=new Vector3(.24f,.14f,.25f);
                var duck=go.AddComponent<VillaDuckBuoyancy>();duck.water=water;duck.floatPoints=new[]{new Vector3(-.09f,.035f,-.085f),new Vector3(.09f,.035f,-.085f),new Vector3(-.09f,.035f,.085f),new Vector3(.09f,.035f,.085f)};ducks.Add(duck);
            }
            var test=root.AddComponent<VillaPoolTestControls>();test.ducks=ducks.ToArray();test.water=water;test.walkthrough=UnityEngine.Object.FindAnyObjectByType<ModernVillaWalkthrough>();var checkpoints=test.walkthrough.checkpoints.ToList();test.poolCheckpoint=checkpoints.Count;checkpoints.Add(new ModernVillaWalkthrough.Checkpoint{label="Pool water and duck test",feet=new Vector3(1.1f,.3f,26.5f),yaw=270});test.walkthrough.checkpoints=checkpoints.ToArray();EditorUtility.SetDirty(test.walkthrough);
            report.Add("Water cells="+water.wetCells.Count(v=>v)+" triangles="+tris.Count/3+" height="+water.Height);File.WriteAllLines("Docs/VillaPool/setup.txt",report);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}



