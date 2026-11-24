using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubber.World;

namespace Rubber.EditorTools
{
    // A bounded edit of the existing north pool lounge, never a whole-map rebuild.
    public static class BeachVillaPoolLounge
    {
        const string ScenePath = "Assets/Modern Villa/Scenes/Beach Villa.unity";
        const string Report = "Docs/BeachVillaPoolLounge";
        const string RootName = "Beach Villa - Pool Lounge Detail";
        static readonly string[] Furniture = {"PoolLevel/Lounger (3)","PoolLevel/Lounger (2)","PoolLevel/Lounger (1)","PoolLevel/Lounger", "LoungeArea/CouchTable","LoungeArea/CouchTable (1)","LoungeArea/GrandSunshade","LoungeArea/GrandSunshade (1)","LoungeArea/LoungerB Variant"};
        static Transform Find(string path)
        {
            var parts = path.Split(new[] {'/'}, 2);
            var root = SceneManager.GetActiveScene().GetRootGameObjects().SingleOrDefault(g => g.name == parts[0]);
            return root ? (parts.Length == 1 ? root.transform : root.transform.Find(parts[1])) : null;
        }
        static Bounds Bounds(Transform t) => BeachVillaExpansionSurvey.BoundsOf(t.gameObject);
        static void Check()
        {
            if (Application.isPlaying || Lightmapping.isRunning || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open Beach Villa in Edit mode, outside a light bake.");
            Directory.CreateDirectory(Report);
        }
        [MenuItem("Rubber/Pool Lounge/1 Survey and Before Images")]
        public static void Survey()
        {
            Check();
            var sb = new StringBuilder("CURRENT active scene objects, not an earlier survey\n");
            foreach (var path in new[] {"PoolLevel", "LoungeArea"})
            foreach (var t in Find(path).GetComponentsInChildren<Transform>().Where(t => t.gameObject.activeInHierarchy && (t.parent == Find(path) || t.GetComponent<Renderer>())))
            {
                var b = Bounds(t);
                if (b.max.z < 18 || b.min.z > 26) continue;
                sb.AppendLine($"{BeachVillaExpansionSurvey.PathOf(t)}\tposition={t.position:F3}\trotation={t.eulerAngles:F2}\tscale={t.lossyScale:F3}\tmin={b.min:F3}\tmax={b.max:F3}\tsource={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)}");
                foreach (var c in t.GetComponents<Collider>()) sb.AppendLine($"  COLLIDER {c.GetType().Name} enabled={c.enabled} min={c.bounds.min:F3} max={c.bounds.max:F3}");
            }
            var walk = UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            var body = walk.GetComponent<CharacterController>();
            sb.AppendLine($"Player height={body.height}; radius={body.radius}; step={body.stepOffset}; skin={body.skinWidth}; camera local={walk.view.localPosition}; FOV={walk.view.GetComponent<Camera>().fieldOfView}");
            Physics.SyncTransforms();
            foreach (float x in new[] {-12f,-10f,-8f,-6f,-4f,-2f})
            foreach (float z in new[] {18.6f,20f,21.7f,23f,25f})
            {
                var hits = Physics.RaycastAll(new Vector3(x,4,z),Vector3.down,5).OrderBy(h => h.distance);
                sb.AppendLine($"SURFACE {x},{z}: " + string.Join(" | ", hits.Select(h => $"{h.point.y:F3} {BeachVillaExpansionSurvey.PathOf(h.transform)}")));
            }
            bool applied=Find(RootName);
            File.WriteAllText(Report + (applied?"/survey-current.txt":"/survey.txt"), sb.ToString());
            Capture(applied?"current":"before");
            Debug.Log("Pool lounge survey complete: " + Report);
        }
        static void Capture(string prefix)
        {
            // Temporary camera only; the player, main camera and render settings are untouched.
            var source = UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>().view.GetComponent<Camera>();
            var go = new GameObject("Pool lounge temporary review camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = go.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled = false;
            var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            var sourceData = source.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (sourceData) { data.renderPostProcessing = sourceData.renderPostProcessing; data.volumeLayerMask = sourceData.volumeLayerMask; data.antialiasing = sourceData.antialiasing; }
            var rt = RenderTexture.GetTemporary(1440,960,24,RenderTextureFormat.ARGB32);
            var old = RenderTexture.active;
            try
            {
                camera.targetTexture = rt; camera.aspect = 1.5f;
                var views = new[] {
                    ("overview",new Vector3(-17,11,29),new Vector3(-7,0.4f,20.8f)),
                    ("player-west",new Vector3(-12.1f,1.7f,21.8f),new Vector3(-5.5f,.6f,20.5f)),
                    ("player-east",new Vector3(-2.4f,1.7f,21.8f),new Vector3(-9,.6f,20.5f)) };
                foreach (var v in views)
                {
                    camera.transform.SetPositionAndRotation(v.Item2,Quaternion.LookRotation(v.Item3-v.Item2));
                    camera.Render(); RenderTexture.active = rt;
                    var texture = new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                    texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); texture.Apply();
                    File.WriteAllBytes(Report+"/"+prefix+"-"+v.Item1+".png",texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally { camera.targetTexture = null; RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(go); }
        }
        [MenuItem("Rubber/Pool Lounge/2 Inspect Detail Assets")]
        public static void InspectAssets()
        {
            Check();
            var preview = EditorSceneManager.NewPreviewScene();
            var old = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(960,720,24);
            try
            {
                var cg = new GameObject("Preview camera"); SceneManager.MoveGameObjectToScene(cg,preview);
                var cam = cg.AddComponent<Camera>(); cam.scene = preview; cam.enabled=false; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.26f,.29f,.32f); cam.nearClipPlane=.01f;
                cam.targetTexture=rt; cam.aspect=960f/720;
                var lg = new GameObject("Preview light"); SceneManager.MoveGameObjectToScene(lg,preview);
                var light=lg.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=2; lg.transform.rotation=Quaternion.Euler(40,-35,0);
                foreach (var path in new[]{"Props/TowelA","Props/Glas","Plants/PottedPlantA"})
                {
                    var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+path+".prefab");
                    if(!asset)throw new InvalidOperationException(path);
                    var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,preview);
                    foreach(var lod in g.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);
                    foreach(var renderer in g.GetComponentsInChildren<Renderer>()) renderer.enabled=true;
                    var b=Bounds(g.transform); float size=b.size.magnitude;
                    cam.transform.position=b.center+new Vector3(1,.8f,-1.6f).normalized*size*1.55f;
                    cam.transform.LookAt(b.center); cam.Render(); RenderTexture.active=rt;
                    var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                    texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
                    File.WriteAllBytes(Report+"/asset-"+g.name+".png",texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(g);
                }
            }
            finally {RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);EditorSceneManager.ClosePreviewScene(preview);}
        }
        static void Move(Transform t,Vector3 position,StringBuilder log)
        {
            Undo.RecordObject(t,"Refine north pool lounge");
            log.AppendLine($"MOVE {BeachVillaExpansionSurvey.PathOf(t)}: {t.position:F4} -> {position:F4}; original orientation and scale retained");
            t.position=position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        static float FloorAt(Vector3 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,.8f,p.z),Vector3.down,1f,~0,QueryTriggerInteraction.Ignore);
            var floor=hits.Where(h=>h.transform.name.StartsWith("Floor2x2")).OrderBy(h=>h.distance).ToArray();
            if(floor.Length==0 || Mathf.Abs(floor[0].point.y-.05f)>.01f)throw new InvalidOperationException("Inspected paving missing at "+p);
            return floor[0].point.y;
        }
        static Transform Detail(string assetPath,string name,Vector3 bottom,float yaw,Transform parent,StringBuilder log)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+assetPath+".prefab");
            if(!asset)throw new InvalidOperationException(assetPath);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(g,"Refine north pool lounge");
            g.name=name;g.transform.SetParent(parent,false);g.transform.rotation=Quaternion.Euler(0,yaw,0);
            var b=Bounds(g.transform);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var renderer in g.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.lightmapIndex=-1;renderer.receiveGI=ReceiveGI.LightProbes;
                renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
            }
            if(assetPath=="Props/Glas")
                foreach(var c in g.GetComponentsInChildren<Collider>())c.enabled=false;
            log.AppendLine($"ADD {name}: {assetPath}; min={Bounds(g.transform).min:F4}; max={Bounds(g.transform).max:F4}; native scale");
            return g.transform;
        }
        [MenuItem("Rubber/Pool Lounge/3 Apply Inspected Lounge")]
        public static void Apply()
        {
            Check();
            if(Find(RootName)) {Debug.Log("Pool lounge already exists; no changes made. Hand edits preserved.");return;}
            foreach(var path in Furniture)if(!Find(path) || !Find(path).gameObject.activeInHierarchy)throw new InvalidOperationException("Missing original furniture: "+path);
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save or resolve current scene edits before applying the lounge pass.");
            Physics.SyncTransforms();
            foreach(var p in new[]{new Vector3(-10.05f,0,20.45f),new Vector3(-8.35f,0,20.45f),new Vector3(-6.05f,0,20.45f),new Vector3(-4.35f,0,20.45f),new Vector3(-13.05f,0,24.7f),new Vector3(-4.45f,0,25.1f)})FloorAt(p);
            File.Copy(ScenePath,Report+"/BeforePoolLounge.unity.backup",false);
            Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Refine north pool lounge");
            var log=new StringBuilder("North pool lounge only. Existing prefab links, hierarchy, rotation and native scale preserved.\n");
            try
            {
                var root=new GameObject(RootName);Undo.RegisterCreatedObjectUndo(root,"Refine north pool lounge");
                // Each pair faces the pool (-Z). Tables are shared at arm's reach; shade supports sit behind heads.
                for(int pair=0;pair<2;pair++)
                {
                    float x=pair==0?-9.2f:-5.2f;
                    for(int side=0;side<2;side++)
                    {
                        var t=Find(Furniture[pair*2+side]);
                        var p=new Vector3(x+(side==0?-.85f:.85f),0,20.45f);p.y=FloorAt(p);
                        Move(t,p,log);
                    }
                    var table=Find(Furniture[4+pair]);var tablePosition=new Vector3(x,0,20.7f);tablePosition.y=FloorAt(tablePosition);
                    Move(table,tablePosition,log);
                    var shade=Find(Furniture[6+pair]);var shadePosition=new Vector3(x,0,21.7f);shadePosition.y=FloorAt(shadePosition);
                    Move(shade,shadePosition,log);
                    Physics.SyncTransforms();
                    // Table-only collider ray, not the bounds of the fruit bowl above it.
                    var ray=new Ray(new Vector3(x-.13f,2,20.59f),Vector3.down);
                    if(!table.GetComponent<Collider>().Raycast(ray,out var top,3))throw new InvalidOperationException("Tabletop not found");
                    Detail("Props/Glas",pair==0?"West - juice beside fruit":"East - juice beside fruit",top.point,0,root.transform,log);
                }
                var bench=Find(Furniture[8]);Move(bench,new Vector3(bench.position.x,bench.position.y,24.9f),log);
                foreach(var p in new[]{new Vector3(-13.05f,0,24.7f),new Vector3(-4.45f,0,25.1f)})
                    Detail("Plants/PottedPlantA",p.x< -10?"West - lounge edge planter":"East - lounge edge planter",new Vector3(p.x,FloorAt(p),p.z),p.x< -10?25:155,root.transform,log);
                Physics.SyncTransforms();
                File.WriteAllText(Report+"/changes.txt",log.ToString());
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                Undo.CollapseUndoOperations(undo);
            }
            catch {Undo.RevertAllDownToGroup(undo);throw;}
            Capture("after");
            Debug.Log("North pool lounge saved. No other zone was rebuilt.");
        }
        [MenuItem("Rubber/Pool Lounge/4 Validate and Capture")]
        public static void Validate()
        {
            if(!Application.isPlaying)Check();
            if(SceneManager.GetActiveScene().path!=ScenePath || !Find(RootName))throw new InvalidOperationException("Apply lounge first.");
            var log=new StringBuilder($"Play mode={Application.isPlaying}. Actual existing CharacterController, fixed 1/60 step at walking speed. Sampled routes, not exhaustive movement coverage.\n");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();var body=walk.GetComponent<CharacterController>();
            var saved=walk.transform.position;bool enabled=body.enabled;Physics.SyncTransforms();
            var routes=new[]{
                ("pool edge",new Vector3(-12,.1f,18.65f),new Vector3(-3,.1f,18.65f)),
                ("rear promenade",new Vector3(-12,.1f,22.95f),new Vector3(-3,.1f,22.95f)),
                ("west entry",new Vector3(-12,.1f,18.65f),new Vector3(-12,.1f,22.95f)),
                ("east entry",new Vector3(-3,.1f,18.65f),new Vector3(-3,.1f,22.95f)),
                ("between pairs",new Vector3(-7.2f,.1f,18.65f),new Vector3(-7.2f,.1f,22.95f)),
                ("existing villa stairs",new Vector3(-3,.1f,21.45f),new Vector3(.2f,1.05f,21.45f))};
            int failed=0;
            try
            {
                foreach(var route in routes)for(int direction=0;direction<2;direction++)
                {
                    var start=direction==0?route.Item2:route.Item3;var end=direction==0?route.Item3:route.Item2;
                    body.enabled=false;walk.transform.position=start;body.enabled=true;Physics.SyncTransforms();
                    float vy=0,maxRise=0;bool eyeBlocked=false;int stalled=0;
                    for(int frame=0;frame<1600;frame++)
                    {
                        var delta=end-walk.transform.position;delta.y=0;if(delta.magnitude<.08f)break;
                        if(body.isGrounded && vy<0)vy=-2;vy=Mathf.Max(vy-9.81f/60,-30);
                        var before=walk.transform.position;body.Move(Vector3.ClampMagnitude(delta,2.6f/60)+Vector3.up*vy/60);Physics.SyncTransforms();
                        maxRise=Mathf.Max(maxRise,walk.transform.position.y-start.y);
                        var eye=walk.view.position;
                        eyeBlocked |= Physics.OverlapSphere(eye,.09f,~0,QueryTriggerInteraction.Ignore).Any(c=>c!=body && !c.transform.IsChildOf(walk.transform));
                        var d=walk.transform.position-before;d.y=0;stalled=d.magnitude<.001f?stalled+1:0;
                        if(stalled>45 || walk.transform.position.y< -2)break;
                    }
                    var remainder=walk.transform.position-end;remainder.y=0;
                    bool pass=remainder.magnitude<.15f&&Mathf.Abs(walk.transform.position.y-end.y)<.15f&&!eyeBlocked;
                    if(route.Item1!="existing villa stairs")pass &= maxRise<.12f;
                    if(!pass)failed++;
                    log.AppendLine($"{route.Item1} {(direction==0?"forward":"reverse")}: pass={pass}; remaining={remainder.magnitude:F3}; end={walk.transform.position:F3}; maxRise={maxRise:F3}; cameraOverlap={eyeBlocked}");
                }
            }
            finally {body.enabled=false;walk.transform.position=saved;body.enabled=enabled;Physics.SyncTransforms();}
            foreach(var path in Furniture)
            {
                var t=Find(path);var b=Bounds(t);
                log.AppendLine($"FURNITURE {path}: min={b.min:F4} max={b.max:F4} scale={t.lossyScale:F2}");
            }
            foreach(Transform t in Find(RootName))log.AppendLine($"DETAIL {t.name}: min={Bounds(t).min:F4} max={Bounds(t).max:F4}");
            log.AppendLine($"Route failures={failed}. Duck collection not checked: no collection component identified; no ducks moved.");
            File.WriteAllText(Report+(Application.isPlaying?"/play-validation.txt":"/validation.txt"),log.ToString());
            if(!Application.isPlaying)Capture("after");
            Debug.Log("Pool lounge route failures: "+failed);
        }
        [MenuItem("Rubber/Pool Lounge/5 Play Mode Review")]
        public static void PlayReview()
        {
            if(!Application.isPlaying || SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Enter Play in Beach Villa first.");
            Validate();
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            walk.StartCoroutine(PlayImages(walk));
        }
        static IEnumerator PlayImages(BeachVillaWalkthrough walk)
        {
            var body=walk.GetComponent<CharacterController>();
            var position=walk.transform.position;var rotation=walk.transform.rotation;var viewRotation=walk.view.localRotation;
            bool walkEnabled=walk.enabled,bodyEnabled=body.enabled;
            var log=new StringBuilder("Runtime frame-by-frame movement using existing controller; original rig and camera restored afterwards.\n");
            try
            {
                walk.enabled=false;
                var views=new[]{("west",new Vector3(-12,.1f,22.95f),new Vector3(-8.8f,.65f,20.6f)),("east",new Vector3(-3,.1f,22.95f),new Vector3(-8.5f,.7f,20.7f))};
                foreach(var v in views)
                {
                    body.enabled=false;walk.transform.SetPositionAndRotation(v.Item2,Quaternion.identity);body.enabled=true;Physics.SyncTransforms();
                    for(int frame=0;frame<30;frame++){body.Move(Vector3.down*2*Time.deltaTime);yield return null;}
                    walk.view.rotation=Quaternion.LookRotation(v.Item3-walk.view.position);
                    yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/play-"+v.Item1+".png"));
                    yield return new WaitForSecondsRealtime(.5f);
                    var start=walk.transform.position;
                    float targetX=start.x+(v.Item1=="west"?2:-2);float elapsed=0;
                    while(Mathf.Abs(walk.transform.position.x-targetX)>.08f && elapsed<3)
                    {
                        float dt=Mathf.Min(Time.deltaTime,.05f);elapsed+=dt;
                        body.Move(new Vector3(Mathf.Clamp(targetX-walk.transform.position.x,-2.6f*dt,2.6f*dt),-2*dt,0));yield return null;
                    }
                    log.AppendLine($"{v.Item1} rear approach: moved={Mathf.Abs(walk.transform.position.x-start.x):F3}; targetError={Mathf.Abs(walk.transform.position.x-targetX):F3}; grounded={body.isGrounded}; feet={walk.transform.position:F3}");
                }
            }
            finally
            {
                body.enabled=false;walk.transform.SetPositionAndRotation(position,rotation);walk.view.localRotation=viewRotation;body.enabled=bodyEnabled;walk.enabled=walkEnabled;
                File.WriteAllText(Report+"/play-frames.txt",log.ToString());
            }
        }
    }
}
