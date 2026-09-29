using System;
using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Rubber.EditorTools {
public static class ModernVillaSetup {
 public const string Folder="Assets/_Project/Lighting/ModernVilla", Report="Docs/ModernVillaDayNight", ScenePath="Assets/Modern Villa/Scenes/Modern Villa.unity";
 [MenuItem("Rubber/Modern Review/Configure Day Night %&n")]
 public static void Configure(){
 var scene=SceneManager.GetActiveScene();
 if(scene.path!=ScenePath || scene.isDirty || Application.isPlaying || Lightmapping.isRunning)throw new InvalidOperationException("Open saved Modern Villa outside Play/bake.");
 if(UnityEngine.Object.FindAnyObjectByType<ModernVillaWalkthrough>())throw new InvalidOperationException("Already configured.");
 Directory.CreateDirectory(Folder);Directory.CreateDirectory(Report);if(!File.Exists(Report+"/BeforeLighting.unity.backup"))File.Copy(ScenePath,Report+"/BeforeLighting.unity.backup",false);
 var log=new StringBuilder(); Physics.SyncTransforms();
 var hints=new[]{new Vector3(0,1.4f,0),new Vector3(-2,2,5.3f),new Vector3(-2,5,3.3f),new Vector3(-2.8f,5,6.4f),new Vector3(9.9f,1.7f,11.8f),new Vector3(9.8f,1.7f,17.8f),new Vector3(6,4.5f,15.3f),new Vector3(6.6f,4.5f,17.5f)};
 var labels=new[]{"F1 Pool terrace","F2 Dining","F3 Study","F4 Reading","F5 Living","F6 Breakfast","F7 Studio","F8 Retreat"};
 var points=new ModernVillaWalkthrough.Checkpoint[hints.Length];
 for(int i=0;i<hints.Length;i++){if(!Floor(hints[i],out var p,out var surface))throw new InvalidOperationException("No clear floor at "+labels[i]);points[i]=new ModernVillaWalkthrough.Checkpoint{label=labels[i],feet=p,yaw=i==0?0:-90};log.AppendLine(labels[i]+" feet="+p+" floor="+surface);}
 var camera=UnityEngine.Object.FindObjectsByType<Camera>().First(c=>c.isActiveAndEnabled);
 foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))if(c!=camera)c.enabled=false;
 foreach(var a in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AudioListener>(true)))a.enabled=a.gameObject==camera.gameObject;
 var root=new GameObject("Modern Villa - Lighting Walkthrough");var body=root.AddComponent<CharacterController>();body.height=1.8f;body.radius=.28f;body.center=Vector3.up*.9f;body.stepOffset=.3f;body.skinWidth=.03f;body.minMoveDistance=0;body.slopeLimit=45;
 camera.transform.SetParent(root.transform);camera.transform.localPosition=Vector3.up*1.65f;camera.transform.localRotation=Quaternion.identity;camera.nearClipPlane=.08f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.Skybox;camera.fieldOfView=65;
 var walk=root.AddComponent<ModernVillaWalkthrough>();walk.view=camera.transform;walk.checkpoints=points;walk.GoTo(0);
 var flash=root.AddComponent<BeachVillaTestFlashlight>();flash.view=camera.transform;
 var night=root.AddComponent<ModernVillaDayNight>();night.startAtNight=false;night.sceneLights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
 var settings=UnityEngine.Object.Instantiate(Lightmapping.lightingSettings);settings.name="Modern Villa Day Night";settings.bakedGI=true;settings.realtimeGI=false;settings.mixedBakeMode=MixedLightingMode.IndirectOnly;AssetDatabase.CreateAsset(settings,Folder+"/DayNight.lighting");Lightmapping.lightingSettings=settings;
 var sky=new Material(Shader.Find("Skybox/Procedural"));sky.name="Modern Villa Day Sky";sky.SetFloat("_Exposure",1.0f);sky.SetColor("_SkyTint",new Color(.45f,.55f,.65f));AssetDatabase.CreateAsset(sky,Folder+"/DaySky.mat");RenderSettings.skybox=sky;RenderSettings.ambientMode=AmbientMode.Skybox;RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.65f;
 foreach(var l in night.sceneLights){l.lightmapBakeType=l.type==LightType.Directional?LightmapBakeType.Mixed:LightmapBakeType.Baked;if(l.type==LightType.Directional)RenderSettings.sun=l;PrefabUtility.RecordPrefabInstancePropertyModifications(l);}
 var set=ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(set,null);set.minDistanceBetweenProbes=.75f;set.simplificationLevels=2;set.minRendererVolumeSize=.1f;AssetDatabase.CreateAsset(set,Folder+"/BakingSet.asset");if(!set.TryAddScene(AssetDatabase.AssetPathToGUID(ScenePath)))throw new InvalidOperationException("Existing scene baking set");
 Volume("APV - villa grounds",new Vector3(-2,3,12),new Vector3(40,12,46),1,2);
 Volume("APV - main house",new Vector3(-2,3.5f,8),new Vector3(14,8,23),0,1);
 Volume("APV - annex",new Vector3(7.5f,3,16),new Vector3(12,8,21),0,1);
 foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>()){if(r.GetComponentInParent<Rigidbody>())continue;bool transparent=r.sharedMaterials.Any(m=>m && m.renderQueue>=3000);if(transparent)continue;GameObjectUtility.SetStaticEditorFlags(r.gameObject,GameObjectUtility.GetStaticEditorFlags(r.gameObject)|StaticEditorFlags.ContributeGI);r.receiveGI=ReceiveGI.Lightmaps;PrefabUtility.RecordPrefabInstancePropertyModifications(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);}
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Report+"/setup.txt",log.ToString());
 }
 [MenuItem("Rubber/Modern Review/Dim Furniture Strips %&k")]
 public static void DimStrips(){
 if(Application.isPlaying || SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Modern Villa Edit mode");
 var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Modern Villa/Materials/mv_Emitting White Light.mat");
 string path=Folder+"/WarmFurnitureStrip.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
 if(!material){material=new Material(source){name="Modern Villa warm furniture strip"};material.SetColor("_EmissionColor",new Color(.08f,.045f,.022f));material.SetColor("_BaseColor",new Color(.15f,.12f,.09f));AssetDatabase.CreateAsset(material,path);}
 int count=0;
 foreach(var r in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))){bool furniture=false;for(var t=r.transform;t;t=t.parent)if(t.name.ToLowerInvariant().Contains("couch")||t.name.ToLowerInvariant().Contains("leather"))furniture=true;if(!furniture)continue;var materials=r.sharedMaterials;bool changed=false;for(int i=0;i<materials.Length;i++)if(materials[i]==source){materials[i]=material;changed=true;}if(changed){r.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(r);count++;}}
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());File.WriteAllText(Report+"/furniture-emission.txt","Original vendor emissive strips, not a lightmap defect. Scene-only warm material overrides="+count+". Shared vendor material preserved. Emission reduced from 0.972 white to linear (0.08,0.045,0.022).\n");
 }
 [MenuItem("Rubber/Modern Review/Fix Start %&j")]
 public static void FixStart(){if(Application.isPlaying)throw new InvalidOperationException("Exit Play");var w=UnityEngine.Object.FindAnyObjectByType<ModernVillaWalkthrough>();if(!Floor(new Vector3(0,1.4f,0),out var p,out var surface))throw new InvalidOperationException("No terrace floor");w.checkpoints[0]=new ModernVillaWalkthrough.Checkpoint{label="F1 Pool terrace",feet=p,yaw=0};w.GoTo(0);EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);File.AppendAllText(Report+"/setup.txt","Revised F1="+p+" floor="+surface+"\n");}
 static void Volume(string name,Vector3 center,Vector3 size,int low,int high){var v=new GameObject(name).AddComponent<ProbeVolume>();v.mode=ProbeVolume.Mode.Local;v.transform.position=center;v.size=size;v.overridesSubdivLevels=true;v.lowestSubdivLevelOverride=low;v.highestSubdivLevelOverride=high;v.fillEmptySpaces=true;}
 public static bool Floor(Vector3 hint,out Vector3 feet,out string surface){for(int ring=0;ring<=5;ring++)for(int x=-ring;x<=ring;x++)for(int z=-ring;z<=ring;z++){if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))!=ring)continue;var origin=hint+new Vector3(x*.4f,.5f,z*.4f);if(!Physics.Raycast(origin,Vector3.down,out var hit,2.8f,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.8f)continue;var n=hit.collider.name.ToLowerInvariant();if(!n.Contains("roof")&&!n.Contains("floor")&&!n.Contains("terrain")&&!n.Contains("carpet")&&!n.Contains("stair"))continue;var p=hit.point+Vector3.up*.05f;if(Physics.CheckCapsule(p+Vector3.up*.28f,p+Vector3.up*1.52f,.28f,~0,QueryTriggerInteraction.Ignore))continue;feet=p;surface=hit.collider.name;return true;}feet=default;surface=null;return false;}
}}




