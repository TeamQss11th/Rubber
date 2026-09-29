using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools {
public static class ModernVillaHideSpaces {
 const string ScenePath="Assets/_Project/Scenes/Modern Villa.unity";
 const string Report="Docs/ModernVillaHideSpaces", Marker="Modern Villa - Search Details";
 const string Assets="Assets/Modern Villa/Prefabs/";
 static Bounds B(GameObject g) => BeachVillaExpansionSurvey.BoundsOf(g);
 [MenuItem("Rubber/Modern Search/5 Open Saved Scene")]
 public static void OpenSaved()=>ModernVillaInterior.OpenSaved();
 static void Guard(){if(Application.isPlaying || Lightmapping.isRunning || SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Modern Villa in Edit mode, with baking stopped.");}
 [MenuItem("Rubber/Modern Search/1 Inspect Assets")]
 public static void Inspect(){
 Guard();Directory.CreateDirectory(Report);var preview=EditorSceneManager.NewPreviewScene();var rt=RenderTexture.GetTemporary(900,600,24);var old=RenderTexture.active;var log=new StringBuilder();
 try {
 var cg=new GameObject("Review camera");SceneManager.MoveGameObjectToScene(cg,preview);var cam=cg.AddComponent<Camera>();cam.scene=preview;cam.enabled=false;cam.useOcclusionCulling=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.3f,.32f,.35f);cam.nearClipPlane=.01f;cam.targetTexture=rt;cam.aspect=1.5f;
 var lg=new GameObject("Review light");SceneManager.MoveGameObjectToScene(lg,preview);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;lg.transform.rotation=Quaternion.Euler(40,-35,0);
 foreach(var a in new[]{"Props/TowelA","Props/TabletA","Props/BookC","Props/BookE","Props/PillowG Variant","furniture/PillowB","Plants/PottedPlantC","Props/CoffeePot","Props/DecorativeBowl","furniture/BenchStraight"}){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Assets+a+".prefab");if(!prefab)throw new InvalidOperationException(a);
 var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);foreach(var lod in g.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);var b=B(g);log.AppendLine($"{a}: min={b.min:F4}, max={b.max:F4}, size={b.size:F4}, colliders={g.GetComponentsInChildren<Collider>().Length}");cam.transform.position=b.center+new Vector3(1,.9f,1.5f).normalized*b.size.magnitude*1.5f;cam.transform.LookAt(b.center);cam.Render();Save(rt,Report+"/asset-"+g.name+".png");UnityEngine.Object.DestroyImmediate(g);
 }
 }finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);EditorSceneManager.ClosePreviewScene(preview);}
 File.WriteAllText(Report+"/assets.txt",log.ToString());Views("before");Debug.Log("Modern Search asset inspection ready");
 }
 static void Save(RenderTexture rt,string path){RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);try{tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(tex);}}
 static void Capture(string name,Vector3 eye,Vector3 target){
 var g=new GameObject("Temporary search review"){hideFlags=HideFlags.HideAndDontSave};var cam=g.AddComponent<Camera>();cam.enabled=false;cam.useOcclusionCulling=false;cam.nearClipPlane=.04f;cam.farClipPlane=100;cam.fieldOfView=65;cam.aspect=1.5f;g.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));var light=g.AddComponent<Light>();light.type=LightType.Point;light.intensity=2.5f;light.range=12;light.shadows=LightShadows.None;var rt=RenderTexture.GetTemporary(1440,960,24);var old=RenderTexture.active;
 try{cam.targetTexture=rt;cam.Render();Save(rt,Report+"/"+name+".png");}finally{RenderTexture.active=old;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(g);}
 }
 static void Views(string prefix){
 Capture(prefix+"-living",new Vector3(9.9f,1.88f,11.8f),new Vector3(7.4f,1,15));
 Capture(prefix+"-study",new Vector3(-2,5.18f,3.3f),new Vector3(-5,4.3f,4.8f));
 Capture(prefix+"-breakfast",new Vector3(9.8f,1.88f,17.8f),new Vector3(6.5f,1,19.4f));
 Capture(prefix+"-reading",new Vector3(-2.8f,5.18f,6.4f),new Vector3(-5,4.2f,8));
 Capture(prefix+"-studio",new Vector3(6,4.68f,15.3f),new Vector3(7.65f,3.9f,13.4f));
 Capture(prefix+"-retreat",new Vector3(6.6f,4.68f,17.5f),new Vector3(7.8f,3.7f,19.4f));
 }
 static GameObject Existing(string name){var root=GameObject.Find("Modern Villa - Interior Furnishing");var matches=root.GetComponentsInChildren<Transform>().Where(t=>t.name==name).ToArray();if(matches.Length!=1)throw new InvalidOperationException("Expected unique furniture: "+name);return matches[0].gameObject;}
 static Transform Group(string name,Transform parent){var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Add Modern Villa search details");g.transform.SetParent(parent,false);return g.transform;}
 static readonly StringBuilder additions=new StringBuilder();
 // Mesh raycast locates the real seat or tabletop, rather than the top of its enclosing box.
 static float Surface(GameObject support,float x,float z){
 float top=float.NegativeInfinity;var ray=new Ray(new Vector3(x,B(support).max.y+1,z),Vector3.down);
 foreach(var mf in support.GetComponentsInChildren<MeshFilter>()){
 if(!mf.sharedMesh)continue;var temp=new GameObject("Temporary support probe"){hideFlags=HideFlags.HideAndDontSave};
 try{temp.transform.SetPositionAndRotation(mf.transform.position,mf.transform.rotation);temp.transform.localScale=mf.transform.lossyScale;var c=temp.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;Physics.SyncTransforms();if(c.Raycast(ray,out var hit,10))top=Mathf.Max(top,hit.point.y);}finally{UnityEngine.Object.DestroyImmediate(temp);}
 }
 if(float.IsNegativeInfinity(top))throw new InvalidOperationException("No mesh support on "+support.name+" at "+x+", "+z);return top;
 }
 static GameObject Piece(string asset,string name,Vector3 bottom,Quaternion rotation,Transform parent){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Assets+asset+".prefab");if(!prefab)throw new InvalidOperationException(asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,SceneManager.GetActiveScene());Undo.RegisterCreatedObjectUndo(g,"Add Modern Villa search details");g.name=name;g.transform.SetParent(parent,false);g.transform.rotation=rotation;var b=B(g);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
 foreach(var r in g.GetComponentsInChildren<MeshRenderer>(true)){r.lightmapIndex=-1;r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 // Small surface details do not add movement snags. Existing furniture keeps its colliders.
 if(!asset.StartsWith("Plants/"))foreach(var c in g.GetComponentsInChildren<Collider>()){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);additions.AppendLine($"{name} | {asset} | bottom={bottom:F4} | rotation={rotation.eulerAngles:F2} | bounds={B(g).min:F4} .. {B(g).max:F4}");return g;
 }
 static GameObject On(string asset,string name,GameObject support,float x,float z,Quaternion rotation,Transform parent){var y=Surface(support,x,z);additions.AppendLine($"SUPPORT {name}: {support.name}, mesh y={y:F4}");return Piece(asset,name,new Vector3(x,y+.001f,z),rotation,parent);}
 static GameObject Floor(string asset,string name,float x,float y,float z,float yaw,Transform parent){Physics.SyncTransforms();if(!Physics.Raycast(new Vector3(x,y+.15f,z),Vector3.down,out var h,.3f))throw new InvalidOperationException("No floor for "+name);additions.AppendLine($"SUPPORT {name}: {h.transform.name}, y={h.point.y:F4}");return Piece(asset,name,new Vector3(x,h.point.y,z),Quaternion.Euler(0,yaw,0),parent);}
 static void Books(GameObject support,float x,float z,Transform parent,string label){
 var b=On("Props/BookC",label+" - large base",support,x,z,Quaternion.Euler(0,0,90),parent);
 for(int i=0;i<3;i++)b=On("Props/BookE",label+" - volume "+(i+1),b,x+(i==1?.008f:0),z,Quaternion.Euler(0,i==1?5:-3,90),parent);
 }
 [MenuItem("Rubber/Modern Search/2 Apply Details")]
 public static void Apply(){
 Guard();if(GameObject.Find(Marker)){Debug.Log("Search details already exist; preserving scene edits without duplicates.");return;}if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Resolve unsaved scene changes first.");
 Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();additions.Clear();
 try{
 var root=Group(Marker,null);
 var study=Group("01 Writing desk - look behind reference books",root);var desk=Existing("Writing desk");Books(desk,-5.63f,4.67f,study,"Desk reference stack");
 var living=Group("02 Sofa - look around cushions and end table",root);
 var seat1=Existing("Sofa seat 1");var seat3=Existing("Sofa seat 3");
 On("Props/PillowG Variant","Sofa left back cushion",seat1,6.85f,16.13f,Quaternion.Euler(58,180,0),living);
 On("Props/PillowG Variant","Sofa right back cushion",seat3,8.08f,16.13f,Quaternion.Euler(58,180,0),living);
 Floor("Plants/PottedPlantC","Low palm by sofa end table",9.4f,.25f,15.25f,25,living);
 var breakfast=Group("03 Breakfast - look behind coffee service",root);var table=Existing("Breakfast table");
 var tray=On("Props/TabletA","Breakfast coffee tray",table,5.58f,19.26f,Quaternion.identity,breakfast);
 On("Props/CoffeePot","Coffee pot on serving tray",tray,5.58f,19.26f,Quaternion.identity,breakfast);
 var reading=Group("04 Reading corner - inspect low planting",root);
 Floor("Plants/PottedPlantC","Reading corner low palm",-2.98f,3.555f,8.78f,12,reading);
 var retreat=Group("05 Terrace room - look beside planting",root);
 Floor("Plants/PottedPlantC","Retreat low palm",7.65f,3.05f,19.25f,-25,retreat);
 File.WriteAllText(Report+"/additions.txt",additions.ToString());Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Views("after");Debug.Log("Modern Villa search details saved.");
 }catch{Undo.RevertAllDownToGroup(undo);throw;}
 }
 [MenuItem("Rubber/Modern Search/3 Capture Details")]
 public static void Review(){Guard();Views("after");
 Capture("detail-desk",new Vector3(-6.5f,5.18f,3.75f),new Vector3(-5.6f,4.5f,4.85f));
 Capture("detail-sofa",new Vector3(7.4f,1.88f,14.8f),new Vector3(7.4f,.9f,16.15f));
 Capture("detail-coffee",new Vector3(4.8f,1.88f,18.5f),new Vector3(5.6f,1.1f,19.35f));
 Capture("detail-reading-palm",new Vector3(-4.2f,5.18f,8.7f),new Vector3(-2.9f,3.9f,9));
 Capture("detail-living-palm",new Vector3(9.9f,1.88f,14.6f),new Vector3(9.4f,.65f,15.6f));
 Capture("detail-retreat-palm",new Vector3(6.65f,4.68f,19.5f),new Vector3(7.7f,3.4f,19.6f));
 }
 [MenuItem("Rubber/Modern Search/4 Check Details")]
 public static void Check(){
 Guard();var root=GameObject.Find(Marker);if(!root)throw new InvalidOperationException("Apply details first");
 Physics.SyncTransforms();
 var log=new StringBuilder("Design probes only: no actual duck prefab, collection test, or gameplay player test. Probe center is 8cm above support.\n");
 var routes=new[]{("Main east",new Vector3(-2.85f,.75f,1.5f),new Vector3(-2.85f,.75f,8.2f)),("Dining",new Vector3(-6.65f,.75f,7.5f),new Vector3(-6.65f,.75f,9.7f)),("Upper main",new Vector3(-2.5f,3.555f,1),new Vector3(-2.5f,3.555f,5.3f)),("Reading entry",new Vector3(-2.75f,3.555f,6.3f),new Vector3(-4.8f,3.555f,6.3f)),("Stair aisle",new Vector3(4.95f,.25f,11.7f),new Vector3(4.95f,.25f,16.4f)),("Living east",new Vector3(10.25f,.25f,11.7f),new Vector3(10.25f,.25f,14.6f)),("Breakfast",new Vector3(9.8f,.25f,17.65f),new Vector3(9.8f,.25f,19)),("Landing",new Vector3(4.5f,3.05f,16.2f),new Vector3(6.1f,3.05f,16.2f)),("Studio",new Vector3(6,3.05f,16.2f),new Vector3(6,3.05f,13.4f)),("Retreat",new Vector3(6.6f,3.05f,17.4f),new Vector3(6.6f,3.05f,20.1f))};
 foreach(var route in routes){string obstacle=null;for(int i=0;i<=40;i++){var p=Vector3.Lerp(route.Item2,route.Item3,i/40f);var hit=Physics.OverlapCapsule(p+Vector3.up*.32f,p+Vector3.up*1.5f,.28f,~0,QueryTriggerInteraction.Ignore);if(hit.Length>0){obstacle=hit[0].name;break;}}log.AppendLine($"ROUTE {route.Item1}: {(obstacle==null?"PASS":"BLOCKED "+obstacle)}");}
 var candidates=new[]{
 ("Desk books",new Vector3(-5.63f,Surface(Existing("Writing desk"),-5.63f,4.98f)+.08f,4.98f),new Vector3(-5.63f,5.18f,2.6f),new Vector3(-6.75f,5.185f,5.2f)),
 ("Sofa cushion edge",new Vector3(6.37f,Surface(Existing("Sofa seat 1"),6.37f,16.15f)+.08f,16.15f),new Vector3(9.4f,1.88f,15.5f),new Vector3(5.45f,1.88f,15.4f)),
 ("Coffee service",new Vector3(5.58f,Surface(Existing("Breakfast table"),5.58f,19.53f)+.08f,19.53f),new Vector3(5.58f,1.88f,17.7f),new Vector3(4.5f,1.88f,19.5f)),
 ("Living low palm",new Vector3(9.4f,.33f,15.64f),new Vector3(9.4f,1.88f,12.6f),new Vector3(8.7f,1.88f,15f)),
 ("Reading low palm",new Vector3(-2.98f,3.635f,9.15f),new Vector3(-2.98f,5.18f,6.4f),new Vector3(-3.8f,5.185f,9.05f)),
 ("Retreat low palm",new Vector3(7.65f,3.13f,19.63f),new Vector3(7.65f,4.68f,17.1f),new Vector3(6.65f,4.68f,19.5f))};
 foreach(var p in candidates){var feet=p.Item4-Vector3.up*1.63f;var hits=Physics.OverlapCapsule(feet+Vector3.up*.32f,feet+Vector3.up*1.5f,.28f,~0,QueryTriggerInteraction.Ignore);log.AppendLine("SIDE APPROACH "+p.Item1+": "+(hits.Length==0?"PASS":string.Join(", ",hits.Select(c=>c.name))));}
 var probes=new System.Collections.Generic.List<MeshCollider>();
 try{
 var meshes=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).Where(m=>m.sharedMesh && m.GetComponent<Renderer>() && m.GetComponent<Renderer>().enabled).ToArray();
 foreach(var mf in meshes){if(!candidates.Any(p=>B(mf.gameObject).SqrDistance(p.Item2)<36))continue;var g=new GameObject("Temporary sightline mesh"){hideFlags=HideFlags.HideAndDontSave};g.transform.SetPositionAndRotation(mf.transform.position,mf.transform.rotation);g.transform.localScale=mf.transform.lossyScale;var c=g.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;probes.Add(c);}
 Physics.SyncTransforms();
 foreach(var p in candidates){
 Func<Vector3,int> blocked=eye=>{int count=0;foreach(float dx in new[]{-.04f,0,.04f})foreach(float dy in new[]{-.04f,0,.04f}){var target=p.Item2+new Vector3(dx,dy,0);var ray=new Ray(eye,(target-eye).normalized);if(probes.Any(c=>c.Raycast(ray,out var h,Vector3.Distance(eye,target)-.005f)))count++;}return count;};
 int far=blocked(p.Item3),near=blocked(p.Item4);
 log.AppendLine($"CANDIDATE {p.Item1}: center={p.Item2:F4}; entry eye={p.Item3:F3}; side eye={p.Item4:F3}; blocked sight samples entry={far}/9, side={near}/9 (lower side is better).");
 }
 }finally{foreach(var c in probes)if(c)UnityEngine.Object.DestroyImmediate(c.gameObject);}
 foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)))log.AppendLine($"FINAL {t.name}: min={B(t.gameObject).min:F4}, max={B(t.gameObject).max:F4}, scale={t.localScale:F3}");
 File.WriteAllText(Report+"/validation.txt",log.ToString());Review();Debug.Log("Modern Search mesh sightline and sampled route review ready");
 }
}
}
