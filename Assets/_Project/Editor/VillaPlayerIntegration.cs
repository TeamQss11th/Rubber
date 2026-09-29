using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Rubber.Gameplay.Player;
using Rubber.Gameplay.Ducks;
using Rubber.Gameplay.Ducks.Traits;
using Rubber.Gameplay.Interaction;
using Rubber.World;
namespace Rubber.EditorTools
{
    public static class VillaPlayerIntegration
    {
        public static void Install()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Open Modern Villa in Edit mode.");
            if(UnityEngine.Object.FindAnyObjectByType<PlayerMovement>())throw new InvalidOperationException("Player already installed.");
            Directory.CreateDirectory("Docs/PlayerIntegration");
            EditorSceneManager.SaveScene(scene);File.Copy(scene.path,"Docs/PlayerIntegration/BeforeIntegration.unity.backup",true);
            var spawn=GameObject.Find("Player Spawn - Villa").transform;
            var player=new GameObject("Player");player.layer=2;
            player.transform.SetPositionAndRotation(spawn.position+Vector3.up*.05f,spawn.rotation);
            var capsule=player.AddComponent<CapsuleCollider>();capsule.height=1.8f;capsule.radius=.3f;capsule.center=Vector3.up*.9f;
            capsule.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Art/Materials/PlayerFrictionless.physicMaterial");
            var body=player.AddComponent<Rigidbody>();body.mass=70;body.constraints=RigidbodyConstraints.FreezeRotation;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";cameraObject.transform.SetParent(player.transform,false);cameraObject.transform.localPosition=Vector3.up*1.6f;
            var camera=cameraObject.GetComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=150;camera.fieldOfView=70;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var stats=AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/_Project/ScriptableObjects/Player/PlayerStats.asset");
            var movement=player.AddComponent<PlayerMovement>();movement.Configure(camera.transform,stats);
            var look=player.AddComponent<PlayerCamera>();look.Configure(camera.transform);
            player.AddComponent<PlayerDuckCarrier>().Configure(camera.transform);
            player.AddComponent<PlayerInteractionDetector>().Configure(camera,stats);
            player.AddComponent<PlayerInputReader>().Configure(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Scripts/Gameplay/Player/PlayerControls.inputactions"),movement,look);
            new GameObject("Interaction Reticle",typeof(RectTransform),typeof(Canvas),typeof(InteractionReticle));
            foreach(var door in UnityEngine.Object.FindObjectsByType<VillaSlidingDoor>())if(!door.GetComponent<VillaDoorInteractable>())door.gameObject.AddComponent<VillaDoorInteractable>();
            var registry=new GameObject("Duck Collection").AddComponent<RubberDuckReturnRegistry>();
            const string folder="Assets/_Project/ScriptableObjects/Ducks/Villa";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var collection=new List<RubberDuckData>();var report=new List<string>();
            var hints=new[]{new Vector3(0,1.4f,0),new Vector3(-2,2,5.3f),new Vector3(-2,5,3.3f),new Vector3(9.9f,1.7f,11.8f),new Vector3(6,4.5f,15.3f)};
            var files=Directory.GetFiles("Assets/_Project/Prefabs/Ducks","*.fbx").OrderBy(p=>p).ToArray();
            if(files.Length!=5)throw new InvalidOperationException("Expected five existing duck models.");
            Physics.SyncTransforms();
            for(int i=0;i<files.Length;i++)
            {
                if(!ModernVillaSetup.Floor(hints[i],out var feet,out var surface))throw new InvalidOperationException("No safe duck location "+i);
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(files[i].Replace('\\','/'));
                var data=ScriptableObject.CreateInstance<RubberDuckData>();var serialized=new SerializedObject(data);
                serialized.FindProperty("id").intValue=1001+i;serialized.FindProperty("displayName").stringValue=source.name;serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(data,folder+"/"+source.name+".asset");collection.Add(data);
                var duck=new GameObject(source.name);duck.transform.position=feet+Vector3.up*.12f;
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,duck.transform);var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                visual.transform.localScale*=.32f/Mathf.Max(bounds.size.x,bounds.size.z);bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                visual.transform.position+=duck.transform.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                var rb=duck.AddComponent<Rigidbody>();rb.mass=.12f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearDamping=.05f;rb.angularDamping=.4f;rb.centerOfMass=new Vector3(0,.045f,0);
                var collider=duck.AddComponent<BoxCollider>();collider.center=new Vector3(0,.07f,0);collider.size=new Vector3(.24f,.14f,.25f);
                duck.AddComponent<RubberDuckInteractable>().Configure(data);duck.AddComponent<RubberDuckTraitController>();
                var buoyancy=duck.AddComponent<VillaDuckBuoyancy>();buoyancy.floatPoints=new[]{new Vector3(-.09f,.035f,-.085f),new Vector3(.09f,.035f,-.085f),new Vector3(-.09f,.035f,.085f),new Vector3(.09f,.035f,.085f)};
                duck.AddComponent<VillaDuckReturn>().registry=registry;
                report.Add(source.name+" id="+data.Id+" position="+duck.transform.position+" floor="+surface);
            }
            registry.Configure(collection);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene.path,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=scene.path).Select(s=>new EditorBuildSettingsScene(s.path,false))).ToArray();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            report.Add("Player=1; Camera=1; Ducks=5; Doors=8; Pools=2");File.WriteAllLines("Docs/PlayerIntegration/setup.txt",report);
        }
    }
}
