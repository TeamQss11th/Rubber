using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Rubber.EditorTools
{
    public static class VillaSceneRelocation
    {
        public static void Move()
        {
            const string source="Assets/Modern Villa/Scenes/Modern Villa.unity";
            const string destination="Assets/_Project/Scenes/Modern Villa.unity";
            if(Application.isPlaying || Lightmapping.isRunning)throw new InvalidOperationException("Edit mode outside bake required.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=source)throw new InvalidOperationException("Expected original Modern Villa scene.");
            EditorSceneManager.SaveScene(scene);
            string guid=AssetDatabase.AssetPathToGUID(source);
            var dependencies=AssetDatabase.GetDependencies(source,true).Where(p=>p!=source).OrderBy(p=>p).ToArray();
            string lighting=AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
            int maps=LightmapSettings.lightmaps.Length;
            var builds=EditorBuildSettings.scenes;
            string error=AssetDatabase.MoveAsset(source,destination);
            if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            foreach(var entry in builds)if(entry.path==source)entry.path=destination;
            EditorBuildSettings.scenes=builds;
            scene=EditorSceneManager.OpenScene(destination,OpenSceneMode.Single);
            var after=AssetDatabase.GetDependencies(destination,true).Where(p=>p!=destination).OrderBy(p=>p).ToArray();
            var report=new List<string>{"Scene="+scene.path,"GUID preserved="+(guid==AssetDatabase.AssetPathToGUID(destination))+" ("+guid+")","Dependencies unchanged="+dependencies.SequenceEqual(after),"Lighting data unchanged="+(lighting==AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset)),"Lightmaps before="+maps+" after="+LightmapSettings.lightmaps.Length};
            if(guid!=AssetDatabase.AssetPathToGUID(destination)||!dependencies.SequenceEqual(after))throw new InvalidOperationException("Scene reference verification failed.");
            Directory.CreateDirectory("Docs/SceneRelocation");File.WriteAllLines("Docs/SceneRelocation/checks.txt",report);
            AssetDatabase.SaveAssets();
        }
    }
}
