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
    public static class VillaSecondPoolSetup
    {
        [MenuItem("Rubber/Pool/Add Water To Front Pool")]
        public static void Install()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying||Lightmapping.isRunning||scene.path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Modern Villa Edit mode required");
            const string rootName="Modern Villa - Front Pool Water";
            if(GameObject.Find(rootName))throw new InvalidOperationException("Front pool water already exists; no duplicate created.");
            var pool=GameObject.Find("Pool Level/Pool");if(!pool)throw new InvalidOperationException("Front pool not found");
            var renderers=pool.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/PoolPrototype/PoolWater.mat");if(!material)throw new InvalidOperationException("Existing water material missing");
            float height=renderers.Where(r=>r.name.ToLowerInvariant().Contains("top")).Max(r=>r.bounds.max.y)-.15f;
            float cell=.1f;var origin=new Vector2(Mathf.Floor(bounds.min.x/cell)*cell,Mathf.Floor(bounds.min.z/cell)*cell);
            int columns=Mathf.CeilToInt((bounds.max.x-origin.x)/cell),rows=Mathf.CeilToInt((bounds.max.z-origin.y)/cell);var wet=new bool[columns*rows];
            var vertices=new List<Vector3>();var triangles=new List<int>();Physics.SyncTransforms();
            bool Inside(float x,float z)
            {
                var hits=Physics.RaycastAll(new Vector3(x,height+.03f,z),Vector3.down,height-bounds.min.y+.2f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
                foreach(var hit in hits)return hit.collider.transform.IsChildOf(pool.transform)&&hit.point.y<height-.015f&&hit.normal.y>.2f;
                return false;
            }
            for(int z=0;z<rows;z++)for(int x=0;x<columns;x++)
            {
                float px=origin.x+x*cell,pz=origin.y+z*cell;
                if(!Inside(px+.002f,pz+.002f)||!Inside(px+.098f,pz+.002f)||!Inside(px+.002f,pz+.098f)||!Inside(px+.098f,pz+.098f))continue;
                wet[z*columns+x]=true;int k=vertices.Count;vertices.Add(new Vector3(px,0,pz));vertices.Add(new Vector3(px,0,pz+cell));vertices.Add(new Vector3(px+cell,0,pz+cell));vertices.Add(new Vector3(px+cell,0,pz));triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            }
            if(vertices.Count==0)throw new InvalidOperationException("No valid pool footprint; scene was not changed");
            const string meshPath="Assets/_Project/Art/PoolPrototype/FrontPoolWaterSurface.asset";
            if(AssetDatabase.LoadAssetAtPath<Mesh>(meshPath))throw new InvalidOperationException("Mesh asset already exists; preserve it");
            Directory.CreateDirectory("Docs/VillaPool");EditorSceneManager.SaveScene(scene);
            const string backup="Docs/VillaPool/BeforeSecondWater.unity.backup";if(!File.Exists(backup))File.Copy(scene.path,backup);
            var mesh=new Mesh{name="Front pool water footprint",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
            var root=new GameObject(rootName);Undo.RegisterCreatedObjectUndo(root,"Add front pool water");root.transform.position=new Vector3(0,height,0);
            var water=root.AddComponent<VillaPoolWater>();water.origin=origin;water.cellSize=cell;water.columns=columns;water.rows=rows;water.wetCells=wet;water.bottom=bounds.min.y-.1f;
            var surface=new GameObject("Water surface");surface.transform.SetParent(root.transform,false);surface.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;water.surface=renderer;
            var trigger=root.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(bounds.center.x,(water.bottom+height)*.5f-height,bounds.center.z);trigger.size=new Vector3(bounds.size.x,height-water.bottom,bounds.size.z);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Docs/VillaPool/second-pool.txt",$"Scene={scene.path}\nPool=Pool Level/Pool\nWater height={height:F3}, bottom={water.bottom:F3}, cells={wet.Count(v=>v)}, triangles={triangles.Count/3}\nShared material={AssetDatabase.GetAssetPath(material)}\nSaved. No Play-mode test or new test ducks requested.\n");
        }
    }
}
