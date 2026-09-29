using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaConsoleMaintenance
    {
        const string Folder="Docs/UnityConsole";
        static VillaConsoleMaintenance(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Library/VillaConsole.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);Directory.CreateDirectory(Folder);
            try{if(command=="integration-check")VillaIntegrationChecks.Run();if(command=="integrate")VillaPlayerIntegration.Install();if(command=="handoff")VillaGameHandoff.Clean();if(command=="repair")Repair();if(command=="inspect"||command=="repair"||command=="handoff"||command=="integrate")Inspect();}catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());}
        }
        static void Repair()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying||scene.path!="Assets/Modern Villa/Scenes/Modern Villa.unity")throw new InvalidOperationException("Open Modern Villa in Edit mode first.");
            string backup=Folder+"/Backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path,backup+"/Modern Villa.unity.backup");
            var report=new List<string>();
            foreach(var root in scene.GetRootGameObjects().Where(g=>g.name=="StaticLightingSky"))
            {
                int count=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                if(count==0)continue;
                Undo.RegisterCompleteObjectUndo(root,"Remove unresolved sky component");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                report.Add("Removed unresolved StaticLightingSky components: "+count);
            }
            const string target="Assets/_Project/Prefabs/TerrainPlants";
            Directory.CreateDirectory(target);AssetDatabase.Refresh();
            foreach(var terrain in UnityEngine.Object.FindObjectsByType<Terrain>().Where(t=>t.gameObject.scene==scene))
            {
                var data=terrain.terrainData;string path=AssetDatabase.GetAssetPath(data);
                if(path!="Assets/Modern Villa/New Terrain.asset")continue;
                File.Copy(path,backup+"/New Terrain.asset.backup",true);
                var prototypes=data.treePrototypes;var before=data.treeInstances;
                Undo.RecordObject(data,"Use terrain-compatible decorative plants");
                for(int i=0;i<prototypes.Length;i++)
                {
                    var prefab=prototypes[i].prefab;
                    if(!prefab||prefab.GetComponentsInChildren<MeshCollider>(true).Length==0)continue;
                    string source=AssetDatabase.GetAssetPath(prefab);
                    string destination=target+"/"+prefab.name+"-Terrain.prefab";
                    var contents=PrefabUtility.LoadPrefabContents(source);
                    try
                    {
                        foreach(var collider in contents.GetComponentsInChildren<MeshCollider>(true))UnityEngine.Object.DestroyImmediate(collider);
                        prototypes[i].prefab=PrefabUtility.SaveAsPrefabAsset(contents,destination);
                        if(!prototypes[i].prefab)throw new InvalidOperationException("Failed to save "+destination);
                    }
                    finally{PrefabUtility.UnloadPrefabContents(contents);}
                    report.Add("Terrain prototype "+i+": "+source+" -> "+destination);
                }
                data.treePrototypes=prototypes;data.RefreshPrototypes();terrain.Flush();
                if(!before.SequenceEqual(data.treeInstances))throw new InvalidOperationException("Tree placement changed unexpectedly.");
                EditorUtility.SetDirty(data);report.Add("Preserved tree placements: "+before.Length);
            }
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            File.WriteAllLines(Folder+"/repair.txt",report);
        }
        static void Inspect()
        {
            var lines=new List<string>();var assembly=typeof(Editor).Assembly;var entries=assembly.GetType("UnityEditor.LogEntries");var entryType=assembly.GetType("UnityEditor.LogEntry");
            var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
            entries.GetMethod("StartGettingEntries",flags).Invoke(null,null);
            try
            {
                int count=(int)entries.GetMethod("GetCount",flags).Invoke(null,null);lines.Add("Console count="+count);
                var get=entries.GetMethod("GetEntryInternal",flags);
                for(int i=0;i<count;i++){object entry=Activator.CreateInstance(entryType);get.Invoke(null,new[]{(object)i,entry});var fs=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;var message=entryType.GetField("message",fs)?.GetValue(entry)?.ToString();lines.Add($"ENTRY {i}: "+message);}
            }
            finally{entries.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
            File.WriteAllLines(Folder+"/console.txt",lines);
            lines.Clear();var scene=SceneManager.GetActiveScene();lines.Add($"Scene={scene.path}, playing={Application.isPlaying}");
            foreach(var go in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject))
            {int n=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);if(n>0)lines.Add($"MISSING {ModernVillaSurfaceAudit.PathOf(go.transform)} count={n} prefab={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)}");}
            foreach(var terrain in UnityEngine.Object.FindObjectsByType<Terrain>())
            {
                var d=terrain.terrainData;lines.Add($"TERRAIN {ModernVillaSurfaceAudit.PathOf(terrain.transform)} data={AssetDatabase.GetAssetPath(d)} instances={d.treeInstanceCount}");
                for(int i=0;i<d.treePrototypes.Length;i++)
                {var prefab=d.treePrototypes[i].prefab;if(!prefab)continue;lines.Add($"TREE {i} {AssetDatabase.GetAssetPath(prefab)} instances={d.treeInstances.Count(t=>t.prototypeIndex==i)}");foreach(var c in prefab.GetComponentsInChildren<Collider>(true))lines.Add($" COLLIDER {ModernVillaSurfaceAudit.PathOf(c.transform)} {c.GetType().Name} enabled={c.enabled} bounds={c.bounds}");}
            }
            File.WriteAllLines(Folder+"/scene.txt",lines);
            var missing=new List<string>();
            foreach(var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)>0)missing.Add(ModernVillaSurfaceAudit.PathOf(go.transform)+" asset="+AssetDatabase.GetAssetPath(go)+" scene="+go.scene.path);
            File.WriteAllLines(Folder+"/loaded-missing.txt",missing);
        }
    }
}
