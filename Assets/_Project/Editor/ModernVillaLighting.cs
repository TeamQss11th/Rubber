using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Rubber.EditorTools {
public static class ModernVillaLighting {
 const string ScenePath="Assets/Modern Villa/Scenes/Modern Villa.unity", Report="Docs/ModernVillaLighting", Marker="Modern Villa - Interior Lighting";
 static Bounds B(GameObject g)=>BeachVillaExpansionSurvey.BoundsOf(g);
 static string P(Transform t)=>t.parent?P(t.parent)+"/"+t.name:t.name;
 static void Guard(){if(Application.isPlaying || Lightmapping.isRunning || SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Modern Villa in Edit mode with baking stopped.");Directory.CreateDirectory(Report);}
 [MenuItem("Rubber/Modern Lighting/1 Inspect")]
 public static void Inspect(){Guard();var log=new StringBuilder();
 foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))log.AppendLine($"LIGHT {P(l.transform)} pos={l.transform.position:F3} type={l.type} power={l.intensity} range={l.range} bake={l.lightmapBakeType} shadows={l.shadows}");
 foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("/lighting/"))){log.AppendLine($"FIXTURE {P(t)} position={t.position:F4} bounds={B(t.gameObject).min:F4} .. {B(t.gameObject).max:F4}");foreach(var mf in t.GetComponentsInChildren<MeshFilter>())log.AppendLine($"  MESH {P(mf.transform)} bounds={B(mf.gameObject).min:F4} .. {B(mf.gameObject).max:F4}");}
 foreach(var p in new[]{new Vector3(-4.7f,2,8.1f),new Vector3(-4.8f,4.6f,2.5f),new Vector3(-4.9f,4.6f,7.5f),new Vector3(7.4f,1.5f,14.45f),new Vector3(6.5f,1.5f,19.15f),new Vector3(7.65f,4.1f,13.35f),new Vector3(5.45f,4.1f,18.7f)})log.AppendLine($"CEILING {p:F3}: "+string.Join(" | ",Physics.RaycastAll(p,Vector3.up,3).Select(h=>$"{h.point.y:F4} {P(h.transform)}")));
 File.WriteAllText(Report+"/survey.txt",log.ToString());Previews();Views("before");Debug.Log("Modern lighting survey ready");}
 static void Save(RenderTexture rt,string name){RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);try{tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(Report+"/"+name+".png",tex.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(tex);}}
 static void Previews(){var preview=EditorSceneManager.NewPreviewScene();var rt=RenderTexture.GetTemporary(900,700,24);var old=RenderTexture.active;try{var g=new GameObject("PreviewCamera");SceneManager.MoveGameObjectToScene(g,preview);var c=g.AddComponent<Camera>();c.scene=preview;c.enabled=false;c.useOcclusionCulling=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.3f,.32f,.35f);c.nearClipPlane=.01f;c.aspect=900f/700;c.targetTexture=rt;var lg=new GameObject("PreviewLight");SceneManager.MoveGameObjectToScene(lg,preview);var l=lg.AddComponent<Light>();l.type=LightType.Directional;l.intensity=2;lg.transform.rotation=Quaternion.Euler(35,-40,0);foreach(var name in new[]{"RoofLamp","WallLight","CouchTableLamp"}){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/lighting/"+name+".prefab");var item=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);foreach(var lod in item.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);var b=B(item);c.transform.position=b.center+new Vector3(1,.5f,1.5f).normalized*b.size.magnitude*1.6f;c.transform.LookAt(b.center);c.Render();Save(rt,"asset-"+name);UnityEngine.Object.DestroyImmediate(item);}}finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);EditorSceneManager.ClosePreviewScene(preview);}}
 static void Capture(string name,Vector3 eye,Vector3 target){var g=new GameObject("Temporary lighting review camera"){hideFlags=HideFlags.HideAndDontSave};var cam=g.AddComponent<Camera>();cam.enabled=false;cam.useOcclusionCulling=false;cam.nearClipPlane=.05f;cam.farClipPlane=100;cam.fieldOfView=65;cam.aspect=1.5f;g.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));var rt=RenderTexture.GetTemporary(1440,960,24);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();Save(rt,name);}finally{RenderTexture.active=old;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(g);}}
 static void Views(string prefix){
 Capture(prefix+"-living",new Vector3(9.9f,1.88f,11.8f),new Vector3(7.4f,1.4f,15));
 Capture(prefix+"-dining",new Vector3(-2,2.38f,5.3f),new Vector3(-5,1.9f,8.3f));
 Capture(prefix+"-study",new Vector3(-2,5.18f,3.3f),new Vector3(-5,4.7f,4.8f));
 Capture(prefix+"-breakfast",new Vector3(9.8f,1.88f,17.8f),new Vector3(6.5f,1.5f,19.4f));
 Capture(prefix+"-reading",new Vector3(-2.8f,5.18f,6.4f),new Vector3(-5,4.65f,8));
 Capture(prefix+"-studio",new Vector3(6,4.68f,15.3f),new Vector3(7.65f,4.2f,13.4f));
 Capture(prefix+"-retreat",new Vector3(6.6f,4.68f,17.5f),new Vector3(4.5f,4.1f,19));
 }
 static Transform Group(string name,Transform parent){var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Light Modern Villa");g.transform.SetParent(parent,false);return g.transform;}
 static GameObject Fixture(string prefab,string name,Transform parent){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/lighting/"+prefab+".prefab"),SceneManager.GetActiveScene());Undo.RegisterCreatedObjectUndo(g,"Light Modern Villa");g.name=name;g.transform.SetParent(parent,false);foreach(var r in g.GetComponentsInChildren<MeshRenderer>()){r.lightmapIndex=-1;r.receiveGI=ReceiveGI.LightProbes;GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}return g;}
 static void SetShadowTier(UnityEngine.Rendering.Universal.UniversalAdditionalLightData data){var serialized=new SerializedObject(data);serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=0;serialized.ApplyModifiedProperties();}
 static Light Source(string name,Transform parent,Vector3 pos,Vector3 direction,float power,float range,float angle){var g=Group(name,parent);g.position=pos;g.rotation=Quaternion.LookRotation(direction);var l=Undo.AddComponent<Light>(g.gameObject);l.type=LightType.Spot;l.spotAngle=angle;l.innerSpotAngle=angle*.55f;l.intensity=power;l.range=range;l.color=Color.white;l.useColorTemperature=true;l.colorTemperature=3600;l.lightmapBakeType=LightmapBakeType.Mixed;l.shadows=LightShadows.Soft;l.shadowBias=.015f;l.shadowNormalBias=.15f;l.shadowNearPlane=.02f;l.bounceIntensity=1;l.renderMode=LightRenderMode.ForcePixel;var data=g.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();SetShadowTier(data);return l;}
 static void Pendant(string name,Transform parent,float x,float z,float ceiling){
 var g=Fixture("RoofLamp",name,parent);var b=B(g);g.transform.position=new Vector3(x-b.center.x,ceiling-b.max.y,z-b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);b=B(g);Source("Warm downlight",g.transform,new Vector3(x,b.min.y-.025f,z),Vector3.down,1.8f,4.5f,140);
 }
 static void Wall(string name,Transform parent,Vector3 back,float yaw){
 var g=Fixture("WallLight",name,parent);g.transform.SetPositionAndRotation(back,Quaternion.Euler(0,yaw,0));PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);var b=B(g);var outward=g.transform.forward;
 Source("Warm wall downlight",g.transform,b.center+Vector3.down*(b.extents.y+.02f)+outward*.025f,Vector3.down+outward*.5f,1.1f,3.8f,130);
 Source("Soft wall uplight",g.transform,b.center+Vector3.up*(b.extents.y+.02f)+outward*.025f,Vector3.up+outward*.5f,.3f,1.8f,110);
 }
 [MenuItem("Rubber/Modern Lighting/2 Apply")]
 public static void Apply(){Guard();if(GameObject.Find(Marker)){Debug.Log("Lighting already added; preserving manual edits.");return;}if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Resolve unsaved edits first");Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();try{
 var root=Group(Marker,null);
 Pendant("Main dining pendant",root,-4.7f,8.1f,3.255f);
 Pendant("Breakfast pendant",root,6.5f,19.15f,2.75f);
 Wall("Main study wall lamp",root,new Vector3(-7.245f,5.25f,3.3f),90);
 Wall("Main reading wall lamp",root,new Vector3(-7.245f,5.25f,7.15f),90);
 Wall("Living wall lamp",root,new Vector3(5.55f,2.1f,16.995f),180);
 Wall("Retreat wall lamp",root,new Vector3(3.255f,4.8f,17.55f),90);
 Wall("Dining wall lamp",root,new Vector3(-6.2f,2.6f,10.495f),180);
 Wall("Reading back wall lamp",root,new Vector3(-3.45f,5.3f,9.495f),180);
 var fixtures=GameObject.Find("Modern Villa - Interior Furnishing").GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).EndsWith("/CouchTableLamp.prefab")).ToArray();
 var existing=Group("Sources aligned to existing lamps",root);
 foreach(var t in fixtures){var b=B(t.gameObject);Source(t.name+" - warm task light",existing,new Vector3(b.center.x,b.min.y+.325f,b.center.z),Vector3.down,.18f,2.4f,145);}
 foreach(var path in new[]{"Main Building/Ground Level/Objects/mv_roof_lamp","annex building/Level 1/Objects/mv_roof_lamp"}){var g=GameObject.Find(path);if(!g)throw new InvalidOperationException(path);var b=B(g);Source(g.transform.parent.parent.name+" - existing pendant light",existing,new Vector3(b.center.x,b.min.y-.025f,b.center.z),Vector3.down,1.8f,4.5f,140);}
 Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Views("after");Debug.Log("Modern Villa lights added without baking.");
 }catch{Undo.RevertAllDownToGroup(undo);throw;}}
 [MenuItem("Rubber/Modern Lighting/3 Review")]
 public static void Review(){Guard();Views("after");var root=GameObject.Find(Marker);var log=new StringBuilder();foreach(var l in root.GetComponentsInChildren<Light>())log.AppendLine($"{P(l.transform)} pos={l.transform.position:F3}, intensity={l.intensity}, range={l.range}, kelvin={l.colorTemperature}, bake={l.lightmapBakeType}, shadows={l.shadows}");foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)))log.AppendLine($"FIXTURE {P(t)} bounds={B(t.gameObject).min:F4}..{B(t.gameObject).max:F4} scale={t.localScale:F3}");File.WriteAllText(Report+"/validation.txt",log.ToString());}
 [MenuItem("Rubber/Modern Lighting/4 Open Saved")]
 public static void OpenSaved(){ModernVillaInterior.OpenSaved();if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.sceneLighting=true;SceneView.lastActiveSceneView.Repaint();}}
 [MenuItem("Rubber/Modern Lighting/5 Refine")]
 public static void Refine(){Guard();var root=GameObject.Find(Marker).transform;
 if(!root.Find("Dining wall lamp"))Wall("Dining wall lamp",root,new Vector3(-6.2f,2.6f,10.495f),180);
 if(!root.Find("Reading back wall lamp"))Wall("Reading back wall lamp",root,new Vector3(-3.45f,5.3f,9.495f),180);
 foreach(var l in root.GetComponentsInChildren<Light>()){Undo.RecordObject(l,"Balance interior lights");l.colorTemperature=3600;l.intensity=l.name.Contains("task")?.18f:l.name.Contains("uplight")?.3f:l.name.Contains("wall down")?1.1f:1.8f;var d=l.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();if(!d)d=Undo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(l.gameObject);Undo.RecordObject(d,"Limit local shadow resolution");SetShadowTier(d); if(l.name.Contains("wall")){Undo.RecordObject(l.transform,"Balance wall light direction");l.transform.rotation=Quaternion.LookRotation((l.name.Contains("uplight")?Vector3.up:Vector3.down)+l.transform.parent.forward*.5f);}}
 EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Review();}
}
}
