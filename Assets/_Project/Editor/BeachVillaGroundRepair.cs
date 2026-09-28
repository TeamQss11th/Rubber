using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Rubber.World;

namespace Rubber.EditorTools
{
    public static class BeachVillaGroundRepair
    {
        const string Report = "Docs/BeachVillaGroundRepair";
        const string ScenePath = "Assets/Modern Villa/Scenes/Beach Villa.unity";
        static Transform root;
        static readonly StringBuilder log = new StringBuilder();
        static Bounds B(GameObject g) => BeachVillaExpansionSurvey.BoundsOf(g);
        static Transform Find(string path)
        {
            var parts=path.Split(new[]{'/'},2);
            var first=SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g=>g.name==parts[0]);
            return first ? (parts.Length==1?first.transform:first.transform.Find(parts[1])) : null;
        }
        static void Park(Transform t) { if(t) { log.AppendLine("Inactive: " + BeachVillaExpansionSurvey.PathOf(t)); t.gameObject.SetActive(false); } }
        static GameObject Put(string path, Vector3 bottom, float yaw = 0, Vector3? scale = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/" + path + ".prefab");
            if(!asset) throw new InvalidOperationException(path);
            var g = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            g.transform.SetParent(root, false); g.transform.rotation = Quaternion.Euler(0,yaw,0);
            if(scale.HasValue) g.transform.localScale = scale.Value;
            var b=B(g); g.transform.position += bottom-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var r in g.GetComponentsInChildren<MeshRenderer>())
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
            return g;
        }
        static void Pave(float x0,float x1,float z0,float z1,float top=.05f)
        {
            for(float x=x0;x<x1-.001f;x+=2) for(float z=z0;z<z1-.001f;z+=2)
            {
                float w=Mathf.Min(2,x1-x), d=Mathf.Min(2,z1-z);
                Put("floors/Floor2x2",new Vector3(x+w/2,top-1,z+d/2),0,new Vector3(w/2,1,d/2));
            }
        }
        static void Check()
        {
            if(Application.isPlaying || Lightmapping.isRunning || SceneManager.GetActiveScene().path!=ScenePath)
                throw new InvalidOperationException("Beach Villa must be open in Edit mode, with no bake running.");
            Directory.CreateDirectory(Report);
        }
        [MenuItem("Rubber/Ground Repair/1 Repair Ground and Garden - No Bake")]
        public static void Apply()
        {
            Check(); if(Find("Beach Villa - Ground Repair")) throw new InvalidOperationException("Repair already applied.");
            var scene=SceneManager.GetActiveScene(); EditorSceneManager.SaveScene(scene);
            File.Copy(ScenePath,Report+"/BeforeRepair.unity.backup",false);
            log.Clear(); root=new GameObject("Beach Villa - Ground Repair").transform;
            // Replace the disconnected lower decks with one continuous exterior datum.
            Park(Find("PoolLevel/Floor")); Park(Find("PoolLevel/Terrace"));
            Park(Find("Beach Villa - Layout Revision/Original pool deck junction"));
            var paving=Find("Beach Villa - Layout Revision/Continuous paving - deduplicated 2m grid");
            foreach(Transform t in paving)
            {
                var b=B(t.gameObject);
                if(b.center.x>-18 && b.center.x<18 && b.center.z>-10 && b.center.z<34) Park(t);
            }
            // These rectangles meet exactly. The pool basin and raised main house are excluded.
            Pave(-18,18,-10,-1); Pave(-18,18,31,34);
            Pave(-18,-14.02f,-1,31); Pave(-14.02f,-1,-1,0);
            Pave(-14.02f,-1,18,31); Pave(-1.02f,-1,0,18); Pave(15,18,-1,31);
            // Original foundation has deliberately missing modules around obsolete rear steps.
            var foundation=Find("Floor");
            var existing=foundation.Cast<Transform>().Where(t=>t.gameObject.activeInHierarchy && t.name.StartsWith("Floor")).ToArray();
            for(float x=0;x<=14;x+=2) for(float z=0;z<=30;z+=2)
            {
                if(!existing.Any(t=>{var b=B(t.gameObject);return x>b.min.x-.01f&&x<b.max.x+.01f&&z>b.min.z-.01f&&z<b.max.z+.01f;}))
                    Pave(x-1,x+1,z-1,z+1,1);
            }
            // Rear sunken landing and its buried flight no longer have a purpose.
            var front=Find("Front Area");
            foreach(Transform t in front) if(t.name.StartsWith("Stairs")||t.name.StartsWith("Lantern")) Park(t);
            // A single readable entry flight at each end of the raised house.
            Park(Find("PoolLevel/Stairs"));
            for(int i=0;i<4;i++)
            {
                Put("floors/StairsA",new Vector3(5.5f+i,.05f,-1.61f));
                Put("floors/StairsA",new Vector3(5.5f+i,.05f,31.61f),180);
            }
            // Seat the original pool-side furniture on the same 5cm exterior datum.
            foreach(Transform t in Find("PoolLevel")) if(t.name.StartsWith("Lounger")) t.position+=Vector3.up*.05f;
            foreach(Transform t in Find("LoungeArea"))
            {
                var b=B(t.gameObject);
                if(b.min.y<.02f) t.position+=Vector3.up*.05f;
            }
            foreach(Transform t in Find("PoolLevel/Objects"))
            {
                var b=B(t.gameObject);
                if(b.center.z<0 && b.max.y<1.3f) t.position+=Vector3.down*.05f;
            }
            var couchTable=Find("PoolLevel/mv_CouchTableD"); if(couchTable)couchTable.position+=Vector3.down*.05f;
            TerrainGround();
            // Remove bathing props from open gardens and exposed upper terrace.
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if(t.name.IndexOf("Jacuzzi",StringComparison.OrdinalIgnoreCase)>=0) Park(t);
            Park(Find("Beach Villa - Expanded Grounds/Garden spa pavilion"));
            // Replace oversized spa architecture with a small shaded conversation destination.
            Put("Props/PavillonC",new Vector3(-28,.05f,25));
            Put("furniture/GardenChair",new Vector3(-29,.05f,25),90);
            Put("furniture/GardenChair",new Vector3(-27,.05f,25),270);
            var table=Put("furniture/PavillonSideTable",new Vector3(-28,.05f,25));
            Put("Props/LanternC",new Vector3(-28,B(table).max.y,25));
            RepairTrees();
            // Checkpoints are grounded positions, not camera positions over a former deck edge.
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            var cp=walk.checkpoints[0]; cp.label="F1 / Pool garden arrival";cp.feet=new Vector3(-7,.1f,-9);cp.yaw=25;walk.checkpoints[0]=cp;
            cp=walk.checkpoints[7];cp.label="F8 / Shaded garden seating";walk.checkpoints[7]=cp;
            walk.GoTo(0);EditorUtility.SetDirty(walk);
            var hide=UnityEngine.Object.FindAnyObjectByType<BeachVillaHideLocations>();
            if(hide) {for(int i=0;i<hide.spots.Length;i++){var s=hide.spots[i];s.label=s.label.Replace("Spa lounger","Garden lounger");hide.spots[i]=s;}EditorUtility.SetDirty(hide);}
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Report+"/changes.txt",log.ToString());Validate();
        }
        static void TerrainGround()
        {
            var terrain=UnityEngine.Object.FindAnyObjectByType<Terrain>();
            var data=UnityEngine.Object.Instantiate(terrain.terrainData);
            AssetDatabase.CreateAsset(data,"Assets/_Project/Lighting/BeachVillaExpanded/GroundRepairTerrain.asset");
            terrain.terrainData=data; terrain.GetComponent<TerrainCollider>().terrainData=data;
            int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);var origin=terrain.transform.position;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float wx=origin.x+x/(float)(n-1)*data.size.x,wz=origin.z+z/(float)(n-1)*data.size.z;
                if(wx< -40.5f||wx>40.5f||wz< -23.5f||wz>50.5f)continue;
                float y=.025f;
                // The pool alone is recessed; its existing basin meshes supply the floor.
                if(wx> -14.5f&&wx<-.5f&&wz>-.5f&&wz<18.5f)y=-1.15f;
                heights[z,x]=(y-origin.y)/data.size.y;
            }
            data.SetHeights(0,0,heights);EditorUtility.SetDirty(data);
            log.AppendLine("External paving top=0.05m; planted soil=0.025m. Main house top=1m. Pool basin retained. No legacy terrain depression remains under exterior routes.");
        }
        // Canopy bounds are not the planting point. Inspect the lowest actual mesh vertices.
        static Vector3 TrunkBase(Transform tree)
        {
            var points=new List<Vector3>();
            foreach(var mf in tree.GetComponentsInChildren<MeshFilter>())
            {
                if(!mf.sharedMesh)continue;
                using(var meshes=Mesh.AcquireReadOnlyMeshData(mf.sharedMesh))
                using(var vertices=new NativeArray<Vector3>(meshes[0].vertexCount,Allocator.Temp))
                {
                    meshes[0].GetVertices(vertices);
                    for(int i=0;i<vertices.Length;i++)points.Add(mf.transform.TransformPoint(vertices[i]));
                }
            }
            if(points.Count==0)throw new InvalidOperationException("Tree has no geometry: "+tree.name);
            float y=points.Min(v=>v.y);var bottom=points.Where(v=>v.y<y+.025f).ToArray();
            return new Vector3(bottom.Average(v=>v.x),y,bottom.Average(v=>v.z));
        }
        static void RepairTrees()
        {
            Park(Find("Plants")); // These original roots were planted against the old depressed terrain.
            var terrain=UnityEngine.Object.FindAnyObjectByType<Terrain>();
            var group=Find("Beach Villa - Layout Revision/Layered planting clusters");
            var trees=group.Cast<Transform>().Where(t=>t.name.StartsWith("Palm")).ToArray();
            var planted=new[]{new Vector2(-37,-15),new Vector2(-35.7f,-12.5f),new Vector2(-37,8),new Vector2(-36.2f,11),new Vector2(-37,39),new Vector2(-35.8f,42),new Vector2(37,-15),new Vector2(35.8f,-12.5f),new Vector2(38,4),new Vector2(37.8f,7),new Vector2(38,39),new Vector2(37,42),new Vector2(-19,47),new Vector2(-16.5f,47.5f),new Vector2(20,47),new Vector2(22.5f,47.5f)};
            var audit=new StringBuilder("Tree\troot\tsoil\terror\n");
            for(int i=0;i<trees.Length;i++)
            {
                var t=trees[i];var before=TrunkBase(t);Vector2 xz=i<planted.Length?planted[i]:new Vector2(before.x,before.z);
                float y=terrain.SampleHeight(new Vector3(xz.x,0,xz.y))+terrain.transform.position.y;
                t.position+=new Vector3(xz.x,y-.025f,xz.y)-before;
                foreach(var c in t.GetComponentsInChildren<Collider>())c.enabled=false;
                if(i<planted.Length)
                {
                    var collider=t.GetComponent<CapsuleCollider>();if(!collider)collider=t.gameObject.AddComponent<CapsuleCollider>();
                    collider.enabled=true;collider.radius=.18f;collider.height=2.8f;
                    collider.center=t.InverseTransformPoint(new Vector3(xz.x,y+1.4f,xz.y));
                }
                var after=TrunkBase(t);audit.AppendLine($"{t.name}\t{after:F3}\t{y:F3}\t{after.y-y:F3}");
            }
            File.WriteAllText(Report+"/tree-roots.tsv",audit.ToString());
            // The south-east secondary palm used to protrude through paving. No tree now uses that position.
            log.AppendLine($"Grounded {trees.Length} palms using mesh root positions; 16 courtyard trunks placed exclusively in soil strips. Old 5 uncontained palms inactive.");
        }
        static bool FloorAt(float x,float z,out float y)
        {
            var hits=Physics.RaycastAll(new Vector3(x,1.4f,z),Vector3.down,4,~0,QueryTriggerInteraction.Ignore);
            var floors=hits.Where(h=>h.normal.y>.9f && (h.collider is TerrainCollider || h.collider.name.StartsWith("Floor") || h.collider.name.StartsWith("TerraceFloor"))).OrderByDescending(h=>h.point.y).ToArray();
            y=floors.Length>0?floors[0].point.y:float.NaN;return floors.Length>0;
        }
        [MenuItem("Rubber/Ground Repair/4 Finish Walkable Entries")]
        public static void Finish()
        {
            Check();root=Find("Beach Villa - Ground Repair");
            // Keep collision on the vendor treads and orient the high end toward the house.
            foreach(Transform stair in root)if(stair.name.StartsWith("Stairs"))
            {
                var before=B(stair.gameObject);
                stair.rotation=Quaternion.Euler(0,before.center.z<0?180:0,0);
                var after=B(stair.gameObject);
                stair.position+=new Vector3(before.center.x,.05f,before.center.z)-new Vector3(after.center.x,after.min.y,after.center.z);
                foreach(var c in stair.GetComponentsInChildren<Collider>())c.enabled=false;
                foreach(var mf in stair.GetComponentsInChildren<MeshFilter>())
                {
                    var c=mf.GetComponent<MeshCollider>();if(!c)c=mf.gameObject.AddComponent<MeshCollider>();
                    c.sharedMesh=mf.sharedMesh;c.convex=false;c.enabled=true;
                }
            }
            foreach(Transform t in Find("Floor"))
                if(t.name.Contains("WallSupport")&&B(t.gameObject).center.x>16)Park(t);
            Park(Find("PavillonC")); // Its elevated pillars stood directly across the new east path.
            var gardenRoof=Find("Beach Villa - Ground Repair/PavillonC");if(gardenRoof)gardenRoof.gameObject.SetActive(true);
            var planting=Find("Beach Villa - Layout Revision/Layered planting clusters");
            foreach(Transform t in planting)
            {
                if(t.name!="Shrub"&&t.name!="PlantC")continue;
                var b=B(t.gameObject);if(b.min.y>.1f)continue; // Keep the plants already inside stone planters.
                float x=b.center.x,z=b.center.z;
                if(z>44){z=Mathf.Clamp(z,45.5f,48.5f);}
                else if(x<0){x=Mathf.Clamp(x,-38,-35.2f);}
                else{x=Mathf.Clamp(x,37.1f,38.6f);}
                t.position+=new Vector3(x,.025f,z)-new Vector3(b.center.x,b.min.y,b.center.z);
            }
            foreach(Transform t in Find("FloorLevelTerrace"))
            {
                var b=B(t.gameObject);
                if(b.center.x>6 && b.center.x<9 && b.center.z>-.2f && b.center.z<2.2f)
                    t.position+=Vector3.right*4.2f;
            }
            // Keep the approach to the front flight free of the original low sofa and planters.
            var sofa=Find("PoolLevel/Objects/Couch Group A");
            if(sofa && B(sofa.gameObject).center.x<9)sofa.position+=Vector3.right*5;
            var tableCollider=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).FirstOrDefault(c=>BeachVillaExpansionSurvey.PathOf(c.transform)=="PoolLevel/Objects/CouchTableB (1)"&&c.bounds.center.x>5&&c.bounds.center.x<9);
            var oldTable=tableCollider?tableCollider.transform:null;
            if(oldTable)
            {
                var own=tableCollider.bounds;
                // Some distant decorations were parented to this table in the sample scene.
                foreach(var child in oldTable.Cast<Transform>().ToArray())
                {
                    var b=B(child.gameObject);var d=b.center-own.center;d.y=0;
                    if(d.magnitude>1.2f)child.SetParent(oldTable.parent,true);
                }
                oldTable.position+=Vector3.right*5;
            }
            var endTable=Find("PoolLevel/mv_CouchTableD");
            if(endTable && B(endTable.gameObject).center.x<9)endTable.position+=new Vector3(7.4f,0,-.4f);
            foreach(var name in new[]{"StoneboxLow (1)","StoneboxLow (2)"})
            {
                Park(Find("PoolLevel/Objects/"+name));
            }
            var edgePlanter=Find("PoolLevel/Objects/StoneboxLow");
            if(edgePlanter){var b=B(edgePlanter.gameObject);edgePlanter.position+=new Vector3(15.5f,.05f,-6.5f)-new Vector3(b.center.x,b.min.y,b.center.z);}
            Physics.SyncTransforms();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.AppendAllText(Report+"/changes.txt","Entry flights: high ends face the house, retaining source mesh collision. East unsupported pavilion and legacy support columns inactive. South sofa and planter obstructions moved clear of approaches.\n");
            Validate();
        }
        [MenuItem("Rubber/Ground Repair/2 Validate Heights and Movement")]
        public static void Validate()
        {
            Check();Physics.SyncTransforms();var sb=new StringBuilder();
            foreach(var p in new[]{new Vector3(17,.9f,24.2f),new Vector3(17,.9f,27),new Vector3(6.8f,.9f,-2.15f),new Vector3(6.8f,.35f,-5)})
                foreach(var c in Physics.OverlapSphere(p,.35f))sb.AppendLine($"BLOCKER {p}: {BeachVillaExpansionSurvey.PathOf(c.transform)}; {c.GetType().Name}; bounds={c.bounds}");
            for(float z=-2.15f;z<-.95f;z+=.2f)
                foreach(var h in Physics.RaycastAll(new Vector3(6.8f,2,z),Vector3.down,3).Where(h=>h.collider.name.StartsWith("Stairs")))sb.AppendLine($"STEP z={z:F2} y={h.point.y:F3}");
            int missing=0,low=0;float min=100,max=-100;
            // Dense ground sampling of the repaired central outdoor region, excluding intentional house/pool.
            for(float x=-17.9f;x<18;x+=.25f)for(float z=-9.9f;z<34;z+=.25f)
            {
                if((x> -1.1f&&x<15.1f&&z> -1.1f&&z<31.1f)||(x> -14.12f&&x<-.92f&&z>-.1f&&z<18.1f))continue;
                if(!FloorAt(x,z,out float y)){missing++;continue;}
                min=Mathf.Min(min,y);max=Mathf.Max(max,y);if(y<.02f)low++;
            }
            sb.AppendLine($"Central outdoor ground: missing={missing}; below datum={low}; min={min:F3}; max={max:F3}");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();var body=walk.GetComponent<CharacterController>();var saved=walk.transform.position;bool enabled=body.enabled;
            var paths=new[]{("spawn to pool west",new Vector3(-7,.1f,-9),new Vector3(-16,.1f,-2)),("old spawn edge",new Vector3(-4.5f,.1f,-7),new Vector3(-4.5f,.1f,-.5f)),("south apron across",new Vector3(-16,.1f,-9),new Vector3(16,.1f,-9)),("east base along house",new Vector3(17,.1f,-8),new Vector3(17,.1f,33)),("west pool return",new Vector3(-17,.1f,-7),new Vector3(-17,.1f,33)),("north join",new Vector3(-17,.1f,33),new Vector3(17,.1f,33)),("front entrance up",new Vector3(6.8f,.1f,-5),new Vector3(6.8f,1.05f,2)),("rear entrance up",new Vector3(7, .1f,35),new Vector3(7,1.05f,26))};
            try
            {
                foreach(var p in paths)foreach(bool reverse in new[]{false,true})
                {
                    var start=reverse?p.Item3:p.Item2;var end=reverse?p.Item2:p.Item3;
                    body.enabled=false;walk.transform.position=start;body.enabled=true;Physics.SyncTransforms();float vy=0,drop=0;int stall=0;
                    for(int i=0;i<2400;i++)
                    {
                        var d=end-walk.transform.position;d.y=0;if(d.magnitude<.12f)break;
                        vy=body.isGrounded?-2:Mathf.Max(-30,vy-9.81f/60);var before=walk.transform.position;
                        body.Move(Vector3.ClampMagnitude(d,2.6f/60)+Vector3.up*vy/60);Physics.SyncTransforms();
                        drop=Mathf.Max(drop,before.y-walk.transform.position.y);stall=Vector3.Distance(before,walk.transform.position)<.001f?stall+1:0;
                        if(stall>60||walk.transform.position.y< -3)break;
                    }
                    var remain=end-walk.transform.position;remain.y=0;
                    bool pass=remain.magnitude<.15f&&Mathf.Abs(walk.transform.position.y-end.y)<.15f;
                    sb.AppendLine($"{p.Item1} {(reverse?"reverse":"forward")}: pass={pass}; remaining={remain.magnitude:F3}; feet={walk.transform.position:F3}; max frame drop={drop:F3}");
                }
            }
            finally{body.enabled=false;walk.transform.position=saved;body.enabled=enabled;Physics.SyncTransforms();File.WriteAllText(Report+"/validation.txt",sb.ToString());}
            Debug.Log(sb.ToString());
        }
        [MenuItem("Rubber/Ground Repair/3 Capture Repaired Layout in Play")]
        public static void Capture()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();walk.StartCoroutine(Images(walk));
        }
        static IEnumerator Images(BeachVillaWalkthrough walk)
        {
            Directory.CreateDirectory(Report);walk.GetComponent<BeachVillaDarkNight>().SetNight(false);
            walk.enabled=false;var body=walk.GetComponent<CharacterController>();body.enabled=false;
            var views=new[]{("01-start",new Vector3(-7,1.75f,-9),new Vector3(3,1.2f,5)),("02-old-start",new Vector3(-4.5f,1.75f,-6.5f),new Vector3(-14,.1f,-2)),("03-rear-entry",new Vector3(7,1.75f,37),new Vector3(7,1.2f,26)),("04-palm-soil",new Vector3(33,1.75f,0),new Vector3(38,1.4f,6)),("05-garden",new Vector3(-20,1.75f,18),new Vector3(-28,1.3f,25)),("06-overview",new Vector3(-43,48,-39),new Vector3(0,0,13)),("07-east-path",new Vector3(17,1.75f,-5),new Vector3(17,1.2f,24))};
            foreach(var v in views)
            {
                walk.transform.position=v.Item2-Vector3.up*1.65f;walk.transform.rotation=Quaternion.identity;walk.view.rotation=Quaternion.LookRotation(v.Item3-v.Item2);
                yield return new WaitForSecondsRealtime(1.5f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/"+v.Item1+".png"));yield return new WaitForSecondsRealtime(.8f);
            }
            walk.enabled=true;body.enabled=true;walk.GoTo(0);File.WriteAllText(Report+"/capture.txt",DateTime.UtcNow.ToString("O"));
        }
    }
}
