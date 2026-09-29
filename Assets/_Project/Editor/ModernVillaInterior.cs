using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Rubber.EditorTools {
public static class ModernVillaInterior {
 const string Path="Assets/Modern Villa/Scenes/Modern Villa.unity", Report="Docs/ModernVillaInterior";
 static Bounds B(Transform t) => BeachVillaExpansionSurvey.BoundsOf(t.gameObject);
 static string P(Transform t)=>t.parent?P(t.parent)+"/"+t.name:t.name;
 [MenuItem("Rubber/Modern Interior/1 Open and Inspect")]
 public static void Inspect(){
 if(Application.isPlaying)throw new InvalidOperationException("Exit Play first");
 if(SceneManager.GetActiveScene().path!=Path){if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save open scene first"); EditorSceneManager.OpenScene(Path);}
 Directory.CreateDirectory(Report); if(!File.Exists(Report+"/Before.unity.backup"))File.Copy(Path,Report+"/Before.unity.backup");
 var s=new StringBuilder();
 foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))){
 if(t.GetComponentsInChildren<Renderer>(true).Length>0 && (P(t).Split('/').Length<=3 || PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)))s.AppendLine($"{P(t)} active={t.gameObject.activeInHierarchy} pos={t.position:F3} yaw={t.eulerAngles.y:F1} min={B(t).min:F3} max={B(t).max:F3} source={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)}");
 }
 foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))s.AppendLine($"CAMERA {P(c.transform)} enabled={c.enabled} pos={c.transform.position} rot={c.transform.eulerAngles}");
 foreach(var c in UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include))s.AppendLine($"PLAYER {P(c.transform)} height={c.height} radius={c.radius}");
 File.WriteAllText(Report+"/survey.txt",s.ToString());
 Capture("before-exterior",new Vector3(22,18,-16),new Vector3(0,2,10));
 CapturePlan("before-ground",1);CapturePlan("before-upper",3.8f);
 Debug.Log("Modern Villa inspection complete.");
 }
 [MenuItem("Rubber/Modern Interior/2 Room and Asset Views")]
 public static void RoomViews(){
 Directory.CreateDirectory(Report); string pre=GameObject.Find("Modern Villa - Interior Furnishing")?"after":"before";
 Capture(pre+"-main-dining",new Vector3(-2,2.38f,5.3f),new Vector3(-5,1.5f,8.3f));
 Capture(pre+"-main-study",new Vector3(-2,5.18f,4.8f),new Vector3(-5,4.2f,1.8f));
 Capture(pre+"-main-reading",new Vector3(-2.8f,5.18f,6.4f),new Vector3(-5,4.2f,8));
 Capture(pre+"-annex-living",new Vector3(9.9f,1.88f,11.8f),new Vector3(7,1,15));
 Capture(pre+"-annex-breakfast",new Vector3(10.3f,1.88f,18),new Vector3(6.5f,1,19.1f));
 Capture(pre+"-annex-upper",new Vector3(8.6f,4.68f,11.5f),new Vector3(5,3.8f,14));
 Capture(pre+"-annex-retreat",new Vector3(6.4f,4.68f,17.5f),new Vector3(4.5f,3.8f,19));
 CapturePlan(pre+"-ground",.75f);CapturePlan(pre+"-upper",3.55f);
 var log=new StringBuilder();Physics.SyncTransforms();foreach(var p in new[]{new Vector3(-5,2,8),new Vector3(-5,5,2),new Vector3(-5,5,8),new Vector3(7,1.5f,14),new Vector3(7,1.5f,19),new Vector3(6,4.5f,14),new Vector3(6,4.5f,19)})log.AppendLine(p+": "+string.Join(" | ",Physics.RaycastAll(p,Vector3.down,2).Select(h=>$"{h.point.y:F3} {P(h.transform)}")));
 File.WriteAllText(Report+"/floor-supports.txt",log.ToString());
 Debug.Log("Interior room views ready");
 }
 [MenuItem("Rubber/Modern Interior/3 Asset Previews")]
 public static void Assets(){
 var preview=EditorSceneManager.NewPreviewScene(); var rt=RenderTexture.GetTemporary(900,600,24);var old=RenderTexture.active;
 try{
 var cg=new GameObject("Camera");SceneManager.MoveGameObjectToScene(cg,preview);var cam=cg.AddComponent<Camera>();cam.scene=preview;cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.3f,.32f,.35f);cam.nearClipPlane=.01f;cam.targetTexture=rt;cam.aspect=1.5f;
 var lg=new GameObject("Light");SceneManager.MoveGameObjectToScene(lg,preview);var l=lg.AddComponent<Light>();l.type=LightType.Directional;l.intensity=2;lg.transform.rotation=Quaternion.Euler(40,-35,0);
 foreach(string a in new[]{"furniture/Couch Group B","furniture/LeatherCouchStraight","furniture/LeatherSideElementLeft","furniture/LeatherSideElementRight","furniture/CouchTableB","furniture/LeatherSeat","furniture/CouchTableF","furniture/DiningTableB Variant","furniture/DiningChairB Variant","furniture/SideTable Variant","Props/BookA","Props/CoffeeCupC","Props/VaseA","Props/CarpetB"}){
 var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+a+".prefab"),preview);foreach(var lod in g.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);var b=B(g.transform);cam.transform.position=b.center+new Vector3(1,.9f,1.5f).normalized*b.size.magnitude*1.5f;cam.transform.LookAt(b.center);cam.Render();RenderTexture.active=rt;var t=new Texture2D(900,600,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,900,600),0,0);t.Apply();File.WriteAllBytes(Report+"/asset-"+g.name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(g);
 }
 }finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);EditorSceneManager.ClosePreviewScene(preview);}
 Debug.Log("Interior asset previews ready");
 }
 const string Marker="Modern Villa - Interior Furnishing";
 static readonly System.Collections.Generic.List<string> additions=new System.Collections.Generic.List<string>();
 static Transform Group(string name,Transform parent){var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Furnish Modern Villa interior");g.transform.SetParent(parent,false);return g.transform;}
 static Transform Piece(string asset,string name,Vector3 bottom,Quaternion rotation,Transform parent,bool floor=true){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+asset+".prefab");if(!prefab)throw new InvalidOperationException(asset);
 if(floor){Physics.SyncTransforms();var hits=Physics.RaycastAll(bottom+Vector3.up*.2f,Vector3.down,.4f).Where(h=>!P(h.transform).StartsWith(Marker)).OrderByDescending(h=>h.point.y).ToArray();if(hits.Length==0)throw new InvalidOperationException("Missing floor "+name+" "+bottom); bottom.y=hits[0].point.y;}
 var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,SceneManager.GetActiveScene());Undo.RegisterCreatedObjectUndo(g,"Furnish Modern Villa interior");g.name=name;g.transform.SetParent(parent,false);g.transform.rotation=rotation;var b=B(g.transform);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
 foreach(var r in g.GetComponentsInChildren<MeshRenderer>(true)){r.lightmapIndex=-1;r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 if(asset.StartsWith("Props/") || asset.StartsWith("lighting/"))foreach(var c in g.GetComponentsInChildren<Collider>()){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
 additions.Add($"{P(g.transform)} | {asset} | bottom={bottom:F4} | yaw={rotation.eulerAngles.y:F1} | size={B(g.transform).size:F3}");return g.transform;
 }
 static Transform Put(string a,string n,float x,float y,float z,float yaw,Transform p)=>Piece(a,n,new Vector3(x,y,z),Quaternion.Euler(0,yaw,0),p);
 static Transform On(string a,string n,Transform table,float dx,float dz,Transform p,bool book=false){var b=B(table);return Piece(a,n,new Vector3(b.center.x+dx,b.max.y+.001f,b.center.z+dz),book?Quaternion.Euler(0,12,90):Quaternion.identity,p,false);}
 static void Sofa(float x,float y,float z,Transform p){
 for(int i=0;i<3;i++)Put("furniture/LeatherCouchStraight","Sofa seat "+(i+1),x+(i-1)*.75f,y,z,180,p);
 Put("furniture/LeatherSideElementLeft","Sofa left arm",x+1.265f,y,z,90,p);Put("furniture/LeatherSideElementRight","Sofa right arm",x-1.265f,y,z,90,p);
 }
 static void Art(string a,string n,Vector3 center,float yaw,Transform p){var t=Piece(a,n,center,Quaternion.Euler(0,yaw,0),p,false);t.position+=center-B(t).center;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
 static void Reading(Transform p,float x,float y,float z){
 Put("furniture/LeatherSeat","Reading armchair",x-.85f,y,z+.6f,155,p);Put("furniture/LeatherSeat","Companion armchair",x+.85f,y,z+.6f,205,p);
 var t=Put("furniture/CouchTableB","Shared reading table",x,y,z-.3f,0,p);On("Props/BookA","Book left after reading",t,-.16f,0,p,true);On("Props/CoffeeCupC","Coffee beside book",t,.2f,.13f,p);
 var side=Put("furniture/SideTable Variant","Reading side table",x-1.8f,y,z+.55f,0,p);On("lighting/CouchTableLamp","Reading lamp",side,0,0,p);
 }
 [MenuItem("Rubber/Modern Interior/4 Apply Furnishing")]
 public static void Apply(){
 if(Application.isPlaying || SceneManager.GetActiveScene().path!=Path || Lightmapping.isRunning)throw new InvalidOperationException("Open Modern Villa in Edit mode");
 if(GameObject.Find(Marker)){Debug.Log("Interior already furnished; manual edits preserved.");return;}
 if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Resolve unsaved edits before furnishing");
 additions.Clear();Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Furnish Modern Villa interior");
 try{
 var root=Group(Marker,null);
 var dining=Group("01 Main ground - family dining",root);
 var t=Put("furniture/DiningTableB Variant","Family dining table",-4.7f,.75f,8.1f,90,dining);
 foreach(int side in new[]{-1,1})foreach(float offset in new[]{-.9f,0,.9f}){
 Put("furniture/DiningChairB Variant","Dining chair",-4.7f+offset,.75f,8.1f+side*.95f,side<0?0:180,dining);
 On("Props/LargePlate","Place setting",t,offset,side*.28f,dining);On("Props/Glas","Water glass",t,offset+.21f,side*.12f,dining);
 }
 On("Props/VaseA","Dining flowers",t,0,0,dining);
 Put("Plants/PottedPlantA","Dining corner plant",-2.15f,.75f,9.65f,20,dining);
 Art("Props/PaintingD","Dining artwork",new Vector3(-4.7f,2.25f,10.47f),180,dining);
 var study=Group("02 Main upper - writing and coffee",root);
 var desk=Put("furniture/DiningTableB Variant","Writing desk",-4.9f,3.555f,4.8f,90,study);
 Put("furniture/DiningChairB Variant","Desk chair",-4.9f,3.555f,3.88f,0,study);
 On("Props/BookA","Current reading on desk",desk,-.15f,-.15f,study,true);On("Props/BookA","Reference book",desk,.5f,.12f,study,true);On("Props/CoffeeCupC","Desk coffee",desk,.33f,-.19f,study);On("lighting/CouchTableLamp","Desk lamp",desk,-1.13f,.06f,study);On("Props/VaseA","Small desk flowers",desk,1.12f,.08f,study);
 Put("furniture/LeatherSeat","Window armchair",-5.8f,3.555f,1.5f,30,study);Put("furniture/LeatherSeat","Window companion chair",-3.7f,3.555f,1.5f,330,study);
 var ct=Put("furniture/CouchTableB","Window coffee table",-4.75f,3.555f,2.45f,0,study);On("Props/CoffeeCupC","Window coffee",ct,-.18f,0,study);On("Props/BookA","Window book",ct,.13f,0,study,true);
 Put("Plants/PottedPlantA","Window corner plant",-6.5f,3.555f,.55f,15,study);
 var reading=Group("03 Main upper - quiet reading room",root);Reading(reading,-4.9f,3.555f,7.5f);Put("Plants/PottedPlantA","Reading room plant",-2.15f,3.555f,8.65f,0,reading);Art("Props/PaintingA","Reading room artwork",new Vector3(-4.9f,5.05f,9.47f),180,reading);
 var living=Group("04 Annex ground - family living",root);Sofa(7.4f,.25f,16.15f,living);
 var rug=Put("Props/CarpetB","Conversation rug",7.4f,.251f,14.45f,0,living);
 var coffee=Piece("furniture/CouchTableF","Round coffee table",new Vector3(7.4f,B(rug).max.y,14.45f),Quaternion.identity,living,false);
 Put("furniture/LeatherSeat","Living armchair left",6.05f,.25f,12.85f,30,living);Put("furniture/LeatherSeat","Living armchair right",8.8f,.25f,12.85f,330,living);
 On("Props/BookA","Living room book",coffee,-.25f,0,living,true);On("Props/CoffeeCupC","Living coffee",coffee,.27f,-.18f,living);On("Props/VaseA","Living flowers",coffee,.1f,.28f,living);
 var ls=Put("furniture/SideTable Variant","Sofa end table",9.5f,.25f,16.1f,0,living);On("lighting/CouchTableLamp","Sofa lamp",ls,0,0,living);Put("Plants/PottedPlantA","Living corner plant",10.25f,.25f,15.6f,10,living);
 Art("Props/PaintingD","Living artwork",new Vector3(7.4f,1.75f,16.97f),180,living);
 var breakfast=Group("05 Annex ground - breakfast room",root);var bt=Put("furniture/DiningTableB Variant","Breakfast table",6.5f,.25f,19.15f,90,breakfast);
 foreach(int side in new[]{-1,1})foreach(float offset in new[]{-.65f,.65f})Put("furniture/DiningChairB Variant","Breakfast chair",6.5f+offset,.25f,19.15f+side*.9f,side<0?0:180,breakfast);
 On("Props/CoffeeCupC","Breakfast cup one",bt,-.65f,-.25f,breakfast);On("Props/CoffeeCupC","Breakfast cup two",bt,.65f,.25f,breakfast);On("Props/BookA","Breakfast reading",bt,-.1f,-.13f,breakfast,true);On("Props/VaseA","Breakfast flowers",bt,.65f,-.06f,breakfast);Put("Plants/PottedPlantA","Breakfast window plant",9.3f,.25f,20.1f,10,breakfast);
 var upstairs=Group("06 Annex upper - home studio",root);var work=Put("furniture/DiningTableB Variant","Studio work table",7.65f,3.05f,13.35f,0,upstairs);Put("furniture/DiningChairB Variant","Studio chair",6.7f,3.05f,13.35f,90,upstairs);On("Props/BookA","Studio reference",work,-.1f,-.1f,upstairs,true);On("Props/BookA","Second reference",work,.1f,.55f,upstairs,true);On("Props/CoffeeCupC","Studio coffee",work,-.22f,-.48f,upstairs);On("lighting/CouchTableLamp","Studio lamp",work,.08f,-1.1f,upstairs);On("Props/VaseA","Studio flowers",work,.04f,1.08f,upstairs);
 Put("furniture/LeatherSeat","Studio reading chair",4.95f,3.05f,12.1f,35,upstairs);var st=Put("furniture/SideTable Variant","Studio reading side table",5.8f,3.05f,12.15f,0,upstairs);On("Props/BookA","Studio reading book",st,0,0,upstairs,true);Put("Plants/PottedPlantA","Studio corner plant",8.45f,3.05f,15.85f,10,upstairs);
 var retreat=Group("07 Annex upper - terrace reading",root);Put("furniture/LeatherSeat","Retreat chair one",4.2f,3.05f,18,75,retreat);Put("furniture/LeatherSeat","Retreat chair two",4.2f,3.05f,19.35f,105,retreat);var rt=Put("furniture/CouchTableB","Retreat tea table",5.45f,3.05f,18.7f,0,retreat);On("Props/CoffeeCupC","Retreat tea cup",rt,0,-.2f,retreat);On("Props/BookA","Retreat reading",rt,0,.13f,retreat,true);Put("Plants/PottedPlantA","Retreat corner plant",8.35f,3.05f,19.55f,0,retreat);
 File.WriteAllLines(Report+"/additions.txt",additions);Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());RoomViews();Debug.Log("Modern Villa interior furnishings saved.");
 }catch{Undo.RevertAllDownToGroup(undo);throw;}
 }
 [MenuItem("Rubber/Modern Interior/5 Check Furnishing")]
 public static void Validate(){
 if(SceneManager.GetActiveScene().path!=Path)throw new InvalidOperationException("Open Modern Villa");var root=GameObject.Find(Marker).transform;Physics.SyncTransforms();var log=new StringBuilder("Scene has no CharacterController. Clearance sample uses a temporary 1.8m height / 0.28m radius capsule, not a verified gameplay player.\n");
 foreach(var zone in root.Cast<Transform>())foreach(var t in zone.Cast<Transform>()){
 var b=B(t);log.AppendLine($"PIECE {P(t)} min={b.min:F4} max={b.max:F4}");
 if(t.name.Contains("Place setting"))foreach(var r in t.GetComponentsInChildren<Renderer>())log.AppendLine($"PLATE {P(r.transform)} enabled={r.enabled} active={r.gameObject.activeInHierarchy} min={r.bounds.min:F4} max={r.bounds.max:F4}");
 var hits=Physics.RaycastAll(new Vector3(b.center.x,b.min.y+.04f,b.center.z),Vector3.down,.15f).Where(h=>!h.transform.IsChildOf(t)).OrderBy(h=>h.distance).ToArray();
 if(hits.Length>0)log.AppendLine($"SUPPORT gap={b.min.y-hits[0].point.y:F4} {P(hits[0].transform)}");else log.AppendLine("SUPPORT mesh visual review required (disabled prop collider / wall art / slatted surface)");
 if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("/furniture/")){
 foreach(var c in Physics.OverlapBox(b.center,Vector3.Max(Vector3.one*.001f,b.extents-Vector3.one*.035f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(root) && (P(c.transform).ToLower().Contains("wall") || P(c.transform).ToLower().Contains("stairs"))))log.AppendLine("REVIEW architecture intersection "+P(t)+" vs "+P(c.transform));
 }
 }
 var routes=new[]{
 ("Main ground window aisle",new Vector3(-2.85f,.75f,1.5f),new Vector3(-2.85f,.75f,8.2f)),
 ("Main dining entry",new Vector3(-6.65f,.75f,7.5f),new Vector3(-6.65f,.75f,9.7f)),
 ("Main upper east aisle",new Vector3(-2.5f,3.555f,1),new Vector3(-2.5f,3.555f,5.3f)),
 ("Main reading entry",new Vector3(-2.75f,3.555f,6.3f),new Vector3(-4.8f,3.555f,6.3f)),
 ("Annex living stair aisle",new Vector3(4.95f,.25f,11.7f),new Vector3(4.95f,.25f,16.4f)),
 ("Annex living east aisle",new Vector3(10.25f,.25f,11.7f),new Vector3(10.25f,.25f,14.6f)),
 ("Breakfast approach",new Vector3(9.8f,.25f,17.65f),new Vector3(9.8f,.25f,19)),
 ("Studio landing",new Vector3(4.5f,3.05f,16.2f),new Vector3(6.1f,3.05f,16.2f)),
 ("Studio desk approach",new Vector3(6.0f,3.05f,16.2f),new Vector3(6.0f,3.05f,13.4f)),
 ("Retreat passage",new Vector3(6.6f,3.05f,17.4f),new Vector3(6.6f,3.05f,20.1f))};
 int failures=0;foreach(var route in routes){string obstacle=null;for(float d=0;d<=1.001f;d+=.025f){var p=Vector3.Lerp(route.Item2,route.Item3,d);var hit=Physics.OverlapCapsule(p+Vector3.up*.32f,p+Vector3.up*1.5f,.28f,~0,QueryTriggerInteraction.Ignore);if(hit.Length>0){obstacle=P(hit[0].transform);break;}}if(obstacle!=null)failures++;log.AppendLine($"ROUTE {route.Item1}: {(obstacle==null?"PASS":"BLOCKED "+obstacle)}");}
 log.AppendLine("Blocked sampled routes="+failures);File.WriteAllText(Report+"/validation.txt",log.ToString());Debug.Log("Interior sampled route failures: "+failures);
 }
 [MenuItem("Rubber/Modern Interior/6 Comparison and Focus")]
 public static void Comparison(){
 var root=GameObject.Find(Marker);if(!root)throw new InvalidOperationException("Furnish first");
 root.SetActive(false);try{RoomViews();}finally{root.SetActive(true);}RoomViews();
 if(SceneView.lastActiveSceneView){var pos=new Vector3(9.9f,1.88f,11.8f);var target=new Vector3(7.4f,1.2f,15);SceneView.lastActiveSceneView.LookAtDirect(target,Quaternion.LookRotation(target-pos),1.65f);SceneView.lastActiveSceneView.sceneLighting=false;SceneView.lastActiveSceneView.Repaint();}
 }
 [MenuItem("Rubber/Modern Interior/7 Open Saved Interior")]
 public static void OpenSaved(){
 if(Application.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Resolve unsaved edits first");
 EditorSceneManager.OpenScene(Path);
 if(SceneView.lastActiveSceneView){var pos=new Vector3(9.9f,1.88f,11.8f);var target=new Vector3(7.4f,1.2f,15);SceneView.lastActiveSceneView.LookAtDirect(target,Quaternion.LookRotation(target-pos),1.65f);SceneView.lastActiveSceneView.sceneLighting=false;SceneView.lastActiveSceneView.drawGizmos=false;}
 }
 static void CapturePlan(string name,float floor){
 var rs=UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.enabled && (r.bounds.min.y>floor+2.2f || (P(r.transform).ToLower().Contains("roof") && r.bounds.max.y>floor+.1f))).ToArray();
 foreach(var r in rs)r.enabled=false;
 try{Capture(name,new Vector3(-1,30,10),new Vector3(-1,floor,10),true);}finally{foreach(var r in rs)r.enabled=true;}
 }
 static void Capture(string name,Vector3 position,Vector3 target,bool ortho=false){
 var go=new GameObject("Temporary interior review"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.enabled=false;cam.nearClipPlane=.05f;cam.farClipPlane=200;cam.fieldOfView=65;cam.useOcclusionCulling=false;cam.orthographic=ortho;cam.orthographicSize=16;cam.aspect=1.5f;
 cam.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position,ortho?Vector3.forward:Vector3.up));
 GameObject fill=null; if(!ortho && !name.Contains("exterior")){fill=new GameObject("Temporary inspection fill"){hideFlags=HideFlags.HideAndDontSave};var light=fill.AddComponent<Light>();light.type=LightType.Point;light.intensity=2.5f;light.range=12;light.shadows=LightShadows.None;fill.transform.position=position;} var rt=RenderTexture.GetTemporary(1440,960,24);var old=RenderTexture.active;
 try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,960,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,960),0,0);tex.Apply();File.WriteAllBytes(Report+"/"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
 finally{RenderTexture.active=old;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(go);if(fill)UnityEngine.Object.DestroyImmediate(fill);}
 }
}
}
