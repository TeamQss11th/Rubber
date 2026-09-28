using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Rubber.World;
using Unity.Collections;

namespace Rubber.EditorTools
{
    // Scene-specific furnishing pass. No runtime generation or shared asset edits.
    public static class BeachVillaDiningPass
    {
        const string ScenePath="Assets/Modern Villa/Scenes/Beach Villa.unity";
        const string Report="Docs/BeachVillaDining";
        const string Marker="Beach Villa - Outdoor Dining Detail";
        static Bounds B(Transform t)=>BeachVillaExpansionSurvey.BoundsOf(t.gameObject);
        static Transform Props=>GameObject.Find("Beach Villa - Expanded Grounds").transform.Find("Furniture and lived-in details");
        static bool InZone(Transform t) {var b=B(t);return b.center.x> -35 && b.center.x< -23 && b.center.z> -1 && b.center.z<9;}
        static Transform[] Furnishings()=>Props.Cast<Transform>().Where(t=>t.gameObject.activeInHierarchy && InZone(t)).ToArray();
        static void Check()
        {
            if(Application.isPlaying || Lightmapping.isRunning || SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Beach Villa outside Play and baking.");
            Directory.CreateDirectory(Report);
        }
        [MenuItem("Rubber/Outdoor Dining/1 Survey")]
        public static void Survey()
        {
            Check();var log=new StringBuilder("Current outdoor dining furnishings and actual support samples\n");
            foreach(var t in Furnishings())
                log.AppendLine($"id={GlobalObjectId.GetGlobalObjectIdSlow(t)}; {t.name}; pos={t.position:F4}; rot={t.eulerAngles:F2}; scale={t.lossyScale:F3}; min={B(t).min:F4}; max={B(t).max:F4}; source={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)}");
            foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name.Contains("Pavillon") && InZone(t)))
                log.AppendLine($"PAVILION {BeachVillaExpansionSurvey.PathOf(t)}; min={B(t).min:F3}; max={B(t).max:F3}; pos={t.position:F3}");
            Physics.SyncTransforms();
            foreach(float x in new[]{-33f,-31f,-29f,-27f,-25f})foreach(float z in new[]{0f,3f,6f,8f})
                log.AppendLine($"SUPPORT {x},{z}: "+string.Join(" | ",Physics.RaycastAll(new Vector3(x,5,z),Vector3.down,6).OrderBy(h=>h.distance).Select(h=>$"{h.point.y:F3} {h.transform.name}")));
            bool done=GameObject.Find(Marker);
            File.WriteAllText(Report+(done?"/survey-after.txt":"/survey-before.txt"),log.ToString());Capture(done?"after":"before");
            Debug.Log("Outdoor dining survey complete.");
        }
        static void Capture(string prefix)
        {
            var source=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>().view.GetComponent<Camera>();
            var go=new GameObject("Temporary dining review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
            var data=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();var original=source.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if(original){data.renderPostProcessing=original.renderPostProcessing;data.volumeLayerMask=original.volumeLayerMask;}
            var rt=RenderTexture.GetTemporary(1440,960,24);var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;camera.aspect=1.5f;
                foreach(var v in new[]{("overview",new Vector3(-21,10,-6),new Vector3(-28,.5f,3)),("approach",new Vector3(-23,1.73f,1),new Vector3(-29,1,3.5f)),("service",new Vector3(-30,1.73f,8),new Vector3(-30,.9f,3))})
                {
                    camera.transform.SetPositionAndRotation(v.Item2,Quaternion.LookRotation(v.Item3-v.Item2));camera.Render();RenderTexture.active=rt;
                    var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
                    File.WriteAllBytes(Report+"/"+prefix+"-"+v.Item1+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally {camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(go);}
        }
        static float MeshSurface(Transform target,Vector3 point,float below)
        {
            float top=float.NegativeInfinity;
            foreach(var mf in target.GetComponentsInChildren<MeshFilter>())
            {
                if(!mf.sharedMesh)continue;
                using(var data=Mesh.AcquireReadOnlyMeshData(mf.sharedMesh))
                using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
                {
                    var mesh=data[0];mesh.GetVertices(vertices);
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var descriptor=mesh.GetSubMesh(sub);if(descriptor.topology!=MeshTopology.Triangles)continue;
                        using(var indices=new NativeArray<int>(descriptor.indexCount,Allocator.Temp))
                        {
                            mesh.GetIndices(indices,sub);
                            for(int i=0;i<indices.Length;i+=3)
                            {
                                var a=mf.transform.TransformPoint(vertices[indices[i]]);var b=mf.transform.TransformPoint(vertices[indices[i+1]]);var c=mf.transform.TransformPoint(vertices[indices[i+2]]);
                                float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(det)<1e-9f)continue;
                                float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.z-c.z))/det;
                                float v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.z-c.z))/det;float w=1-u-v;
                                if(u<-.00001f || v<-.00001f || w<-.00001f)continue;
                                float y=u*a.y+v*b.y+w*c.y;if(y<below)top=Mathf.Max(top,y);
                            }
                        }
                    }
                }
            }
            if(float.IsNegativeInfinity(top))throw new InvalidOperationException("No mesh support: "+target.name+" at "+point);
            return top;
        }
        [MenuItem("Rubber/Outdoor Dining/2 Inspect Surfaces and Assets")]
        public static void Inspect()
        {
            Check();var pavilion=GameObject.Find("Beach Villa - Expanded Grounds").transform.Find("PavillonOpen");
            var log=new StringBuilder();
            foreach(var p in new[]{new Vector3(-28,0,3),new Vector3(-28.7f,0,2.05f),new Vector3(-27.3f,0,3.95f),new Vector3(-26.5f,0,3)})
                log.AppendLine($"Pavilion mesh deck top at {p}: {MeshSurface(pavilion,p,1):F5}");
            var bbq=Furnishings().Single(t=>t.name=="Barbecue");
            foreach(var r in bbq.GetComponentsInChildren<Renderer>())log.AppendLine($"BBQ {BeachVillaExpansionSurvey.PathOf(r.transform)}; enabled={r.enabled}; min={r.bounds.min:F3}; max={r.bounds.max:F3}");
            File.WriteAllText(Report+"/model-surfaces.txt",log.ToString());
            var preview=EditorSceneManager.NewPreviewScene();var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(800,600,24);
            try
            {
                var cg=new GameObject("Preview camera");SceneManager.MoveGameObjectToScene(cg,preview);var cam=cg.AddComponent<Camera>();cam.scene=preview;cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.25f,.28f,.3f);cam.nearClipPlane=.01f;cam.targetTexture=rt;cam.aspect=4f/3;
                var lg=new GameObject("Preview light");SceneManager.MoveGameObjectToScene(lg,preview);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;lg.transform.rotation=Quaternion.Euler(40,-35,0);
                foreach(var path in new[]{"Props/TabletB","Props/JugA","Props/SmallPlate"})
                {
                    var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+path+".prefab");if(!asset)throw new InvalidOperationException(path);
                    var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,preview);foreach(var lod in g.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);
                    var b=B(g.transform);cam.transform.position=b.center+new Vector3(1,.9f,-1.5f).normalized*b.size.magnitude*1.5f;cam.transform.LookAt(b.center);cam.Render();RenderTexture.active=rt;
                    var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(Report+"/asset-"+g.name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(g);
                }
            }
            finally {RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);EditorSceneManager.ClosePreviewScene(preview);}
        }
        static void Bottom(Transform t,Vector3 p,StringBuilder log)
        {
            Undo.RecordObject(t,"Refine outdoor dining");var b=B(t);var old=t.position;
            t.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            log.AppendLine($"MOVE {GlobalObjectId.GetGlobalObjectIdSlow(t)} {t.name}: {old:F4} -> {t.position:F4}");
        }
        static float TableTop(Transform table,Vector3 p)
        {
            // Slatted tables can have a gap exactly at the prop pivot; sample the footprint.
            float y=float.NegativeInfinity;
            foreach(float dx in new[]{-.025f,0,.025f})foreach(float dz in new[]{-.025f,0,.025f})
            {
                try {y=Mathf.Max(y,MeshSurface(table,p+new Vector3(dx,0,dz),1.4f));}
                catch(InvalidOperationException) { /* A ray in a slat gap has no support. */ }
            }
            if(y<.7f)throw new InvalidOperationException("No tabletop support at "+p);
            return y;
        }
        static Transform Add(string path,string name,Vector3 bottom,float yaw,Transform parent,StringBuilder log)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+path+".prefab");if(!asset)throw new InvalidOperationException(path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,SceneManager.GetActiveScene());Undo.RegisterCreatedObjectUndo(go,"Refine outdoor dining");
            go.name=name;go.transform.SetParent(parent,false);go.transform.rotation=Quaternion.Euler(0,yaw,0);Bottom(go.transform,bottom,log);
            foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true)) {r.lightmapIndex=-1;r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);}
            if(path.StartsWith("Props/"))foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
            log.AppendLine("ADD "+path+" as "+name);return go.transform;
        }
        static void RepairBarbecue(Transform bbq,StringBuilder log)
        {
            // Vendor duplicates double-apply the mesh's baked offsets. Match the intact part in each LOD.
            for(int lod=0;lod<2;lod++)foreach(string part in new[]{"Knob","Wheel"})
            {
                var reference=bbq.Find($"Barbecue_{part}_LOD{lod}");if(!reference)throw new InvalidOperationException("Missing BBQ reference");
                for(int i=1;i<=3;i++)
                {
                    var t=bbq.Find($"Barbecue_{part}_LOD{lod} ({i})");
                    if(!t || t.GetComponent<MeshFilter>().sharedMesh!=reference.GetComponent<MeshFilter>().sharedMesh)throw new InvalidOperationException("Unexpected BBQ duplicate geometry");
                    Undo.RecordObject(t,"Repair dining BBQ instance");
                    t.localRotation=reference.localRotation;t.localScale=reference.localScale;
                    Vector3 offset=part=="Knob"?new Vector3(0,0,.235f*i):new Vector3(i==2?0:-.55f,0,i==1?0:-.96f);
                    t.position=reference.position+offset;PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    log.AppendLine($"REPAIR BBQ {t.name}: matched intact {part} mesh pose, offset={offset:F3}; center={B(t).center:F3}");
                }
            }
        }
        [MenuItem("Rubber/Outdoor Dining/3 Apply Inspected Layout")]
        public static void Apply()
        {
            Check();if(GameObject.Find(Marker)){Debug.Log("Outdoor dining already applied; manual edits preserved.");return;}
            var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Resolve unsaved scene edits first.");
            var furnishings=Furnishings();var table=furnishings.Single(t=>t.name=="DiningTable");var chairs=furnishings.Where(t=>t.name=="DiningChair").ToArray();
            if(chairs.Length!=6)throw new InvalidOperationException("Expected six existing dining chairs.");
            var pavilion=GameObject.Find("Beach Villa - Expanded Grounds").transform.Find("PavillonOpen");
            float deck=MeshSurface(pavilion,new Vector3(-28,0,3),1);
            if(!File.Exists(Report+"/BeforeDining.unity.backup"))File.Copy(ScenePath,Report+"/BeforeDining.unity.backup",false);
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Refine outdoor dining");
            var log=new StringBuilder("Scene-only outdoor dining pass; no bake or collection work.\n");
            try
            {
                var group=new GameObject(Marker);Undo.RegisterCreatedObjectUndo(group,"Refine outdoor dining");
                Bottom(table,new Vector3(-28,deck,3),log);
                foreach(int side in new[]{-1,1})
                {
                    var row=chairs.Where(t=>Mathf.Sign(B(t).center.z-3)==side).OrderBy(t=>B(t).center.x).ToArray();
                    if(row.Length!=3)throw new InvalidOperationException("Chair row changed.");
                    for(int i=0;i<3;i++)Bottom(row[i],new Vector3(-28+(i-1)*.7f,deck,3+side*.95f),log);
                }
                foreach(var t in furnishings.Where(t=>t.name=="LargePlate" || t.name=="Glas"))
                {
                    var b=B(t);var p=new Vector3(b.center.x,0,b.center.z);
                    if(t.name=="Glas") {p.x=b.center.x-.17f+(p.z<3?.23f:-.23f);p.z=p.z<3?2.93f:3.07f;}
                    p.y=TableTop(table,p)+.001f;Bottom(t,p,log);
                }
                RepairBarbecue(furnishings.Single(t=>t.name=="Barbecue"),log);
                // The existing entrance light becomes a corner marker rather than standing on the approach axis.
                Bottom(furnishings.Single(t=>t.name=="FloorLight on Variant"),new Vector3(-25.65f,.05f,4.75f),log);
                var service=Add("furniture/DiningTable","Preparation table beside barbecue",new Vector3(-32.1f,.05f,3),90,group.transform,log);
                var jugPosition=new Vector3(-32.1f,0,3.63f);jugPosition.y=TableTop(service,jugPosition);
                Add("Props/JugA","Shared drinks pitcher",jugPosition,90,group.transform,log);
                for(int i=0;i<2;i++)
                {
                    var p=new Vector3(-31.9f,0,3.37f-i*.18f);p.y=TableTop(service,p);
                    Add("Props/Glas","Prepared drink "+(i+1),p,0,group.transform,log);
                }
                var platePoint=new Vector3(-32.05f,0,2.35f);platePoint.y=TableTop(service,platePoint);
                for(int i=0;i<3;i++)
                {
                    var plate=Add("Props/SmallPlate","Serving plate "+(i+1),platePoint,0,group.transform,log);
                    platePoint.y=MeshSurface(plate,platePoint,.99f);
                }
                Add("Plants/PottedPlantA","Preparation corner planter",new Vector3(-32.1f,.05f,1),15,group.transform,log);
                Physics.SyncTransforms();File.WriteAllText(Report+"/changes.txt",log.ToString());
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(undo);
            }
            catch {Undo.RevertAllDownToGroup(undo);throw;}
            Capture("after");Debug.Log("Outdoor dining saved.");
        }
        [MenuItem("Rubber/Outdoor Dining/4 Validate Layout")]
        public static void Validate()
        {
            if(SceneManager.GetActiveScene().path!=ScenePath || !GameObject.Find(Marker))throw new InvalidOperationException("Apply dining layout first.");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();var body=walk.GetComponent<CharacterController>();var saved=walk.transform.position;bool enabled=body.enabled;
            var log=new StringBuilder($"Play={Application.isPlaying}; existing controller height={body.height} radius={body.radius}. Sampled routes and support checks.\n");int failures=0;
            var routes=new[]{
                ("pool to east opening",new Vector3(-17,.1f,3),new Vector3(-26.5f,.2f,3)),
                ("preparation aisle",new Vector3(-30.75f,.1f,0),new Vector3(-30.75f,.1f,8)),
                ("west opening",new Vector3(-30.75f,.1f,3),new Vector3(-29.5f,.2f,3)),
                ("north seat backs",new Vector3(-29.3f,.2f,4.55f),new Vector3(-26.7f,.2f,4.55f)),
                ("south seat backs",new Vector3(-29.3f,.2f,1.45f),new Vector3(-26.7f,.2f,1.45f)),
                ("barbecue working side",new Vector3(-30.75f,.1f,6),new Vector3(-31.45f,.1f,6))};
            try
            {
                foreach(var route in routes)for(int direction=0;direction<2;direction++)
                {
                    var start=direction==0?route.Item2:route.Item3;var end=direction==0?route.Item3:route.Item2;
                    body.enabled=false;walk.transform.position=start;body.enabled=true;Physics.SyncTransforms();float vy=0,peak=start.y;bool eyeHit=false;int stalled=0;
                    for(int f=0;f<1400;f++)
                    {
                        var d=end-walk.transform.position;d.y=0;if(d.magnitude<.08f)break;
                        vy=body.isGrounded?-2:Mathf.Max(-30,vy-9.81f/60);var before=walk.transform.position;
                        body.Move(Vector3.ClampMagnitude(d,2.6f/60)+Vector3.up*vy/60);Physics.SyncTransforms();peak=Mathf.Max(peak,walk.transform.position.y);
                        eyeHit|=Physics.OverlapSphere(walk.view.position,.09f,~0,QueryTriggerInteraction.Ignore).Any(c=>c!=body && !c.transform.IsChildOf(walk.transform));
                        var moved=walk.transform.position-before;moved.y=0;stalled=moved.magnitude<.001f?stalled+1:0;if(stalled>45 || walk.transform.position.y< -2)break;
                    }
                    var delta=walk.transform.position-end;delta.y=0;bool pass=delta.magnitude<.15f && Mathf.Abs(walk.transform.position.y-end.y)<.15f && peak<.3f && !eyeHit;if(!pass)failures++;
                    log.AppendLine($"{route.Item1} {direction}: pass={pass}; remaining={delta.magnitude:F3}; end={walk.transform.position:F3}; peak={peak:F3}; cameraOverlap={eyeHit}");
                }
            }
            finally {body.enabled=false;walk.transform.position=saved;body.enabled=enabled;Physics.SyncTransforms();}
            foreach(var t in Furnishings().Where(t=>t.name=="DiningTable" || t.name=="DiningChair"))
            {float error=Mathf.Abs(B(t).min.y-.15f);if(error>.003f)failures++;log.AppendLine($"DECK CONTACT {t.name}: bottom={B(t).min.y:F4}; error={error:F4}");}
            var dining=Furnishings().Single(t=>t.name=="DiningTable");
            // Source mesh support is checked in Edit mode; runtime static batching changes mesh coordinates.
            foreach(var t in Furnishings().Where(t=>!Application.isPlaying && (t.name=="Glas" || t.name=="LargePlate")))
            {var b=B(t);float error=Mathf.Abs(b.min.y-TableTop(dining,b.center));if(error>.005f)failures++;log.AppendLine($"TABLE CONTACT {t.name}: error={error:F4}");}
            log.AppendLine("Failures="+failures);File.WriteAllText(Report+(Application.isPlaying?"/play-validation.txt":"/validation.txt"),log.ToString());
            Debug.Log("Dining validation failures: "+failures);
        }
        [MenuItem("Rubber/Outdoor Dining/5 Player View in Play")]
        public static void ReviewInPlay()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");Validate();
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();walk.StartCoroutine(PlayerImages(walk));
        }
        static IEnumerator PlayerImages(BeachVillaWalkthrough walk)
        {
            var body=walk.GetComponent<CharacterController>();var position=walk.transform.position;var rotation=walk.transform.rotation;var view=walk.view.localRotation;bool we=walk.enabled,be=body.enabled;
            try
            {
                walk.enabled=false;
                foreach(var v in new[]{("approach",new Vector3(-24,.1f,3),new Vector3(-29,1.1f,3)),("service",new Vector3(-30.6f,.1f,5),new Vector3(-32.1f,.9f,3))})
                {
                    body.enabled=false;walk.transform.SetPositionAndRotation(v.Item2,Quaternion.identity);body.enabled=true;Physics.SyncTransforms();
                    for(int f=0;f<30;f++){body.Move(Vector3.down*2*Mathf.Min(Time.deltaTime,.05f));yield return null;}
                    walk.view.rotation=Quaternion.LookRotation(v.Item3-walk.view.position);yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/play-"+v.Item1+".png"));yield return new WaitForSecondsRealtime(.5f);
                }
            }
            finally {body.enabled=false;walk.transform.SetPositionAndRotation(position,rotation);walk.view.localRotation=view;body.enabled=be;walk.enabled=we;File.WriteAllText(Report+"/play-review.txt","Actual player camera screenshots captured; rig restored.");}
        }
    }
}
