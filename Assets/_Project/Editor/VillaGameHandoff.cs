using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubber.World;

namespace Rubber.EditorTools
{
    public static class VillaGameHandoff
    {
        public static void Clean()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!=ModernVillaSetup.ScenePath || Lightmapping.isRunning)
                throw new InvalidOperationException("Modern Villa Edit mode required.");
            Directory.CreateDirectory("Docs/GameHandoff");
            EditorSceneManager.SaveScene(scene);
            if(!File.Exists("Docs/GameHandoff/BeforeCleanup.unity.backup"))File.Copy(scene.path,"Docs/GameHandoff/BeforeCleanup.unity.backup");
            var report=new List<string>();
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            const string folder="Assets/_Project/Prefabs/Ducks/Buoyant";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(var duck in all.Select(t=>t.GetComponent<VillaDuckBuoyancy>()).Where(d=>d).ToArray())
            {
                if(!duck.name.StartsWith("Water test - "))continue;
                string name=duck.name.Substring("Water test - ".Length);
                duck.water=null;
                duck.name=name;
                var prefab=PrefabUtility.SaveAsPrefabAsset(duck.gameObject,folder+"/"+name+".prefab");
                if(!prefab)throw new InvalidOperationException("Failed to preserve duck prefab");
                report.Add("Saved reusable buoyant prefab and removed test instance: "+name);
                UnityEngine.Object.DestroyImmediate(duck.gameObject);
            }
            foreach(var walk in all.Where(t=>t).Select(t=>t.GetComponent<ModernVillaWalkthrough>()).Where(w=>w).ToArray())
            {
                var root=walk.gameObject;
                var marker=new GameObject("Player Spawn - Villa");
                marker.transform.SetPositionAndRotation(root.transform.position,root.transform.rotation);
                foreach(var camera in root.GetComponentsInChildren<Camera>(true))
                {report.Add("Removed review camera: "+camera.name);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
                foreach(var component in root.GetComponents<Component>().Where(c=>c && !(c is Transform) && !(c is VillaTimeOfDay)).ToArray())
                {
                    // Remove dependents before their required CharacterController.
                    if(component is CharacterController)continue;
                    report.Add("Removed review component: "+component.GetType().Name);UnityEngine.Object.DestroyImmediate(component);
                }
                var body=root.GetComponent<CharacterController>();if(body)UnityEngine.Object.DestroyImmediate(body);
                root.name="Modern Villa - Environment Lighting";
            }
            foreach(var test in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VillaPoolTestControls>(true)).ToArray())UnityEngine.Object.DestroyImmediate(test);
            foreach(var test in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VillaDoorTestControls>(true)).ToArray())UnityEngine.Object.DestroyImmediate(test);
            var pool=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Modern Villa - Pool Water Test");if(pool)pool.name="Modern Villa - Back Pool Water";
            foreach(var camera in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).ToArray())
            {
                var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if(extra)UnityEngine.Object.DestroyImmediate(extra);
                report.Add("Removed sample camera: "+camera.name);UnityEngine.Object.DestroyImmediate(camera);
            }
            foreach(var listener in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)).ToArray())UnityEngine.Object.DestroyImmediate(listener);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)).ToArray();
            foreach(var group in components.Where(c=>c && (c is MonoBehaviour || c is Camera || c is AudioListener)).GroupBy(c=>c.GetType().Name))report.Add("Remaining "+group.Key+": "+group.Count());
            File.WriteAllLines("Docs/GameHandoff/cleanup.txt",report);
        }
    }
}
