using System;
using System.IO;
using System.Linq;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    // Normal Play-mode rendering only. No manual rendering or GPU readback.
    public static class BeachVillaNightPreview
    {
        [MenuItem("Rubber/Night Preview/1 Configure Dark Night")]
        public static void Configure()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/Modern Villa/Scenes/Beach Villa.unity" || EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty)
                throw new InvalidOperationException("Open the saved Beach Villa outside Play mode.");
            var rig=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            if(!rig || rig.gameObject.scene!=scene)throw new InvalidOperationException("Beach Villa walkthrough is required.");
            var night=rig.GetComponent<BeachVillaDarkNight>();
            if(!night)night=Undo.AddComponent<BeachVillaDarkNight>(rig.gameObject);
            Undo.RecordObject(night,"Configure dark night");
            night.sceneLights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
            night.startAtNight=true;
            EditorUtility.SetDirty(night);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed.");
        }
        [MenuItem("Rubber/Night Preview/2 Night in Play Mode")]
        public static void Night(){Get().SetNight(true);}
        [MenuItem("Rubber/Night Preview/3 Day in Play Mode")]
        public static void Day(){Get().SetNight(false);}
        [MenuItem("Rubber/Night Preview/4 Capture Current Game View")]
        public static void Capture(){Get().CaptureCurrentView();}
        static BeachVillaDarkNight Get()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play mode first.");
            var night=UnityEngine.Object.FindAnyObjectByType<BeachVillaDarkNight>();
            if(!night)throw new InvalidOperationException("Configure dark night first.");
            return night;
        }
    }
}
