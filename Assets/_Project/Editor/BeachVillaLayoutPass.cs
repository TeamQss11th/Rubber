using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Rubber.World;

namespace Rubber.EditorTools
{
    public static class BeachVillaLayoutPass
    {
        const string Report="Docs/BeachVillaLayoutReview";
        const string ScenePath="Assets/Modern Villa/Scenes/Beach Villa.unity";
        static Transform revision;
        static StringBuilder changes;
        static Bounds B(GameObject g)=>BeachVillaExpansionSurvey.BoundsOf(g);
        static Transform Group(string name){var g=new GameObject(name);g.transform.SetParent(revision,false);return g.transform;}
        static GameObject Put(string path,Vector3 bottom,float yaw,Transform parent,Vector3? scale=null,Vector3? tilt=null)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+path+".prefab");
            if(!asset)throw new InvalidOperationException(path);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(asset);g.transform.SetParent(parent,false);
            g.transform.rotation=Quaternion.Euler(tilt??new Vector3(0,yaw,0));if(scale.HasValue)g.transform.localScale=scale.Value;
            var b=B(g);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var r in g.GetComponentsInChildren<MeshRenderer>(true)){r.lightmapIndex=-1;GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;}
            bool small=path.StartsWith("Props/") && B(g).size.magnitude<1.3f;
            if(small)foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=LayerMask.NameToLayer("MapSmallDecor");
            if(path.Contains("Carpet"))foreach(var c in g.GetComponentsInChildren<Collider>())c.enabled=false;
            changes.AppendLine($"ADD {path}; base={bottom:F3}; rotation={g.transform.eulerAngles:F1}");return g;
        }
        static void SetBottom(GameObject g,Vector3 p){var b=B(g);g.transform.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);}
        static void Park(Transform t){if(t){t.gameObject.SetActive(false);changes.AppendLine("REPLACED (retained inactive): "+BeachVillaExpansionSurvey.PathOf(t));}}
        [MenuItem("Rubber/Layout Review/2 Refine Layout Only - No Bake")]
        public static void Refine()
        {
            Check();Directory.CreateDirectory(Report);
            if(GameObject.Find("Beach Villa - Layout Revision"))throw new InvalidOperationException("Revision exists; refine it without duplicating.");
            var scene=SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);
            File.Copy(ScenePath,Report+"/BeforeRefine.unity.backup",true);
            changes=new StringBuilder("LAYOUT ONLY. No bake, shader, material, or pipeline edits.\n");
            revision=new GameObject("Beach Villa - Layout Revision").transform;
            var expanded=GameObject.Find("Beach Villa - Expanded Grounds").transform;
            Undo.RegisterFullObjectHierarchyUndo(expanded.gameObject,"Refine villa layout");
            foreach(var name in new[]{"Layered coastal planting","Garden finishing","West wellness court","East guest terrace","North arrival courtyard","South beach lounge","Outdoor dining court","West garden spine","East garden spine","North loop","South loop","Pool return west","Pool return south","East villa connection","Beach access"})Park(expanded.Find(name));
            Boundary();Ground();Planting();Interiors();CorrectOutdoorDetails(expanded);
            var night=UnityEngine.Object.FindAnyObjectByType<BeachVillaDarkNight>();
            // Start in daylight for the user's layout inspection. Existing baked assets remain untouched.
            if(night){night.startAtNight=false;EditorUtility.SetDirty(night);}
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Report+"/changes.txt",changes.ToString());Dump(scene,"after");Validate();
            Selection.activeGameObject=revision.gameObject;
            if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,13),Quaternion.Euler(65,0,0),65);
        }
        static void Boundary()
        {
            var p=Group("Continuous perimeter - matching wall modules");
            void Run(Vector3 a,Vector3 b)
            {
                float length=Vector3.Distance(a,b);int n=Mathf.CeilToInt(length/2);float span=length/n;
                float yaw=Mathf.Abs(a.x-b.x)>.1f?0:90;
                for(int i=0;i<n;i++)Put("Walls/Wall2x2,5",Vector3.Lerp(a,b,(i+.5f)/n),yaw,p,new Vector3(span/2,.6f,1));
            }
            // Side walls include the corners; front/back meet their inner faces without crossing.
            Run(new Vector3(-39.875f,-.15f,-23),new Vector3(-39.875f,-.15f,50));
            Run(new Vector3(39.875f,-.15f,-23),new Vector3(39.875f,-.15f,50));
            Run(new Vector3(-39.75f,-.15f,-22.875f),new Vector3(39.75f,-.15f,-22.875f));
            Run(new Vector3(-39.75f,-.15f,49.875f),new Vector3(39.75f,-.15f,49.875f));
            changes.AppendLine("PERIMETER: 80m x 73m outside bounds; all sides one vendor wall, 1.5m high, base -0.15, top 1.35. Flush butt joints at corners.");
        }
        static readonly HashSet<Vector2Int> tiles=new HashSet<Vector2Int>();
        static void Ground()
        {
            var terrain=UnityEngine.Object.FindAnyObjectByType<Terrain>();var old=terrain.terrainData;
            // Separate layout terrain from the already baked version for rollback.
            var data=UnityEngine.Object.Instantiate(old);AssetDatabase.CreateAsset(data,"Assets/_Project/Lighting/BeachVillaExpanded/LayoutReviewTerrain.asset");
            terrain.terrainData=data;terrain.GetComponent<TerrainCollider>().terrainData=data;
            int n=data.heightmapResolution;var h=data.GetHeights(0,0,n,n);var origin=terrain.transform.position;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float wx=origin.x+x/(float)(n-1)*data.size.x,wz=origin.z+z/(float)(n-1)*data.size.z;
                if(wx>=-40.5f && wx<=40.5f && wz>=-23.5f && wz<=50.5f && (wx< -18 || wx>18 || wz< -10 || wz>32))h[z,x]=(-.10f-origin.y)/data.size.y;
            }
            data.SetHeights(0,0,h);var holes=data.GetHoles(0,0,data.holesResolution,data.holesResolution);
            for(int z=0;z<data.holesResolution;z++)for(int x=0;x<data.holesResolution;x++)holes[z,x]=true;
            data.SetHoles(0,0,holes);EditorUtility.SetDirty(data);
            tiles.Clear();
            void Rect(int x0,int x1,int z0,int z1){for(int x=x0;x<x1;x+=2)for(int z=z0;z<z1;z+=2)tiles.Add(new Vector2Int(x,z));}
            Rect(-34,-22,-4,34);Rect(22,36,0,36);Rect(-30,30,34,44);Rect(-30,30,-20,-8);
            Rect(-22,-18,-8,34);Rect(18,22,-8,34);Rect(-30,30,-10,-6);Rect(-22,-16,18,22);Rect(14,24,28,32);Rect(-12,-8,-8,-2);
            // Fill the entire north arrival connection to the original rear terrace.
            Rect(-8,16,30,34);
            var p=Group("Continuous paving - deduplicated 2m grid");
            foreach(var key in tiles.OrderBy(v=>v.x).ThenBy(v=>v.y))Put("floors/Floor2x2",new Vector3(key.x+1,-.95f,key.y+1),0,p);
            changes.AppendLine($"GROUND: {tiles.Count} unique full-size vendor tiles; no intersecting route/deck copies; paved top=0.05. Garden soil=-0.10. Original pool and building levels preserved.");
        }
        static void Planting()
        {
            var p=Group("Layered planting clusters");var terrain=UnityEngine.Object.FindAnyObjectByType<Terrain>();var random=new System.Random(9828);
            var centers=new[]{new Vector2(-36,-15),new Vector2(-36,8),new Vector2(-35,40),new Vector2(35,-15),new Vector2(36,4),new Vector2(36,40),new Vector2(-19,46),new Vector2(20,46)};
            void Plant(string asset,float x,float z,float scale,bool trunk)
            {
                float y=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
                var g=Put("Plants/"+asset,new Vector3(x,y,z),(float)random.NextDouble()*360,p,Vector3.one*scale);
                foreach(var c in g.GetComponentsInChildren<Collider>())c.enabled=false;
                if(trunk){var c=g.AddComponent<CapsuleCollider>();c.radius=.2f;c.height=3;c.center=new Vector3(0,1.5f,0);}
            }
            for(int c=0;c<centers.Length;c++)
            {
                var v=centers[c];Plant(c%2==0?"PalmA":"PalmC",v.x,v.y,.9f+.16f*(float)random.NextDouble(),true);
                Plant("PalmB",v.x+(c%2==0?1.7f:-1.8f),v.y+2.4f,.72f+.13f*(float)random.NextDouble(),true);
                for(int i=0;i<5;i++){float a=i*2.4f;Plant(i%2==0?"Shrub":"PlantC",v.x+Mathf.Cos(a)*1.7f,v.y+Mathf.Sin(a)*1.8f,.85f+(float)random.NextDouble()*.45f,false);}
            }
            // Loose background clusters, not an evenly spaced fence of trees.
            foreach(var v in new[]{new Vector2(-49,1),new Vector2(-50,37),new Vector2(-29,61),new Vector2(7,65),new Vector2(39,61),new Vector2(52,20)})
                for(int i=0;i<4;i++)Plant(i%2==0?"PalmA":"PalmC",v.x+(float)random.NextDouble()*9-4,v.y+(float)random.NextDouble()*10-5,.85f+(float)random.NextDouble()*.5f,false);
            // Recreate the original hide-planter destinations with leaves seated in the trough.
            foreach(var v in new[]{new Vector2(-34,12),new Vector2(-34,34),new Vector2(-24,-3),new Vector2(25,2),new Vector2(35,33),new Vector2(-7,40),new Vector2(15,40),new Vector2(-12,-19),new Vector2(12,-19)})
            {
                var box=Put("Stoneboxes/StoneboxMedium",new Vector3(v.x,.05f,v.y),0,p);
                for(int i=-1;i<=1;i++)Put("Plants/PlantC",new Vector3(v.x+i*1.1f,B(box).max.y-.12f,v.y),20+i*47,p,Vector3.one*.85f);
            }
        }
        static void Interiors()
        {
            var p=Group("Interior living and reading details");var main=GameObject.Find("BuildingD").transform;
            var lower=main.Find("Level0 Objects");Park(lower.Find("DiningTableB Variant (2)"));Park(lower.Find("DiningTableB Variant (3)"));
            foreach(var name in new[]{"VaseB (2)","VaseB (3)","VaseB (4)"})Park(lower.Find(name));
            Put("Props/CarpetA",new Vector3(4.65f,1.008f,6.9f),0,p,new Vector3(.85f,1,.8f));
            Put("furniture/CouchD Variant",new Vector3(4.3f,1.025f,5.35f),0,p);
            Put("furniture/LeatherSeat",new Vector3(5.75f,1.025f,8.05f),200,p);
            var coffee=Put("furniture/CouchTableF",new Vector3(4.45f,1.026f,7.1f),0,p);Tea(coffee,p);
            var read=Put("furniture/SideTable Variant",new Vector3(4.35f,1.025f,9.6f),0,p);
            Put("furniture/LeatherSeat",new Vector3(3.3f,1.025f,9.7f),90,p);
            Books(read,p);Put("lighting/CouchTableLamp",new Vector3(4.35f,B(read).max.y,9.69f),0,p);
            Put("Plants/PottedPlantA",new Vector3(3.0f,1.025f,15.05f),0,p,Vector3.one*.7f);
            // A serving console sits against the rear of the east room, leaving the central passage free.
            var console=Put("furniture/DiningTable",new Vector3(9.6f,1.025f,14.9f),0,p,new Vector3(.85f,1,.6f));
            var top=B(console).max.y;Put("Props/DecorativeBowl",new Vector3(9.7f,top,14.9f),0,p);
            Put("Props/JugA",new Vector3(9.0f,top,14.9f),15,p);Put("Props/Glas",new Vector3(9.3f,top,14.85f),0,p);
            Put("Props/LanternC",new Vector3(10.2f,top,14.9f),0,p);
            var upper=main.Find("Level1 Objects");Park(upper.Find("LoungerB Variant"));
            Put("Props/CarpetA",new Vector3(9.6f,3.806f,16.4f),0,p,new Vector3(.85f,1,1.05f));
            Put("furniture/Couch Group B",new Vector3(9.6f,3.83f,16.4f),180,p);
            var side=Put("furniture/SideTable Variant",new Vector3(10.9f,3.83f,19.2f),0,p);Books(side,p);
            Put("Plants/VasePalmSmall",new Vector3(10.9f,3.82f,21.6f),30,p);
            // Guest wing furniture uses the actual west-room footprint (26.49..31.48), not its whole building bounds.
            var props=GameObject.Find("Beach Villa - Expanded Grounds").transform.Find("Furniture and lived-in details");
            foreach(Transform t in props){var b=B(t.gameObject);if(b.center.x>26&&b.center.x<34&&b.center.z>8&&b.center.z<27&&!t.GetComponentInChildren<Light>())Park(t);}
            Put("Props/CarpetA",new Vector3(28.9f,.061f,12.4f),0,p,new Vector3(.9f,1,1));
            Put("furniture/Couch Group A",new Vector3(28.9f,.08f,12.4f),180,p);
            var guestTea=Put("furniture/CouchTableC",new Vector3(28.75f,.08f,18.8f),0,p);Tea(guestTea,p);
            Put("furniture/LeatherSeat",new Vector3(28.65f,.05f,20.1f),180,p);
            Put("furniture/LeatherSeat",new Vector3(30.35f,.05f,18.6f),270,p);
            Put("Plants/PottedPlantB",new Vector3(27.35f,.05f,25.6f),0,p,Vector3.one*.75f);
            var desk=Put("furniture/DiningTable",new Vector3(28.9f,.05f,25.9f),0,p);Books(desk,p);
            Put("furniture/DiningChairB Variant",new Vector3(28.9f,.05f,24.8f),0,p);
            Put("Props/VaseC",new Vector3(29.6f,B(desk).max.y,25.9f),0,p);
            changes.AppendLine("INTERIOR: repeated dining sets replaced by living/reading; upper lounge and guest sitting/tea/writing areas. Tabletop objects seated on measured furniture tops; books laid flat.");
        }
        static void Books(GameObject table,Transform p)
        {
            var b=B(table);var q=new Vector3(b.center.x-.13f,b.max.y+.003f,b.center.z);
            var book=Put("Props/BookC",q,0,p,null,new Vector3(0,12,90));
            Put("Props/BookE",new Vector3(q.x+.025f,B(book).max.y+.002f,q.z+.02f),0,p,null,new Vector3(0,-8,90));
        }
        static void Tea(GameObject table,Transform p)
        {
            var b=B(table);float y=b.max.y+.002f;
            var tray=Put("Props/TabletB",new Vector3(b.center.x,y,b.center.z),0,p);float trayTop=B(tray).max.y+.002f;
            Put("Props/CoffeePot",new Vector3(b.center.x-.12f,trayTop,b.center.z),0,p);
            Put("Props/CoffeeCupC",new Vector3(b.center.x+.18f,trayTop,b.center.z+.10f),20,p);
        }
        [MenuItem("Rubber/Layout Review/5 Polish Inspected Layout - No Bake")]
        public static void Polish()
        {
            Check();revision=GameObject.Find("Beach Villa - Layout Revision").transform;
            if(revision.Find("Inspected finishing details"))throw new InvalidOperationException("Finishing pass already applied.");
            changes=new StringBuilder();var p=Group("Inspected finishing details");var interior=revision.Find("Interior living and reading details");
            var lamp=interior.Cast<Transform>().First(t=>t.name=="CouchTableLamp");
            SetBottom(lamp.gameObject,new Vector3(10.36f,1.807f,14.83f));
            var group=interior.Cast<Transform>().First(t=>t.name=="Couch Group A");var old=B(group.gameObject);
            group.rotation=Quaternion.identity;SetBottom(group.gameObject,new Vector3(old.center.x,old.min.y,old.center.z));
            var table=Put("furniture/CouchTableF",new Vector3(28.9f,.08f,12.3f),0,p);Tea(table,p);
            Put("Props/PaintingC",new Vector3(26.51f,1.05f,15.1f),90,p);
            var guest=GameObject.Find("Guest wing - vendor BuildingC").transform;
            foreach(var name in new[]{"Wall2x2,5 (6)","Wall2x2,5 (7)"})
            {var wall=guest.Find("Walls/"+name);var b=B(wall.gameObject);Park(wall);Put("Windows/WindowStraight",new Vector3(b.center.x,b.min.y,b.center.z),90,p);}
            // Seat all cups/pots on their trays by actual bounds, including pre-existing expansion details.
            var roots=new[]{revision,GameObject.Find("Beach Villa - Expanded Grounds").transform.Find("Furniture and lived-in details")};
            var candidates=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>()).Where(t=>PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject)==t.gameObject).Distinct().ToArray();
            var trays=candidates.Where(t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("/Props/Tablet")).ToArray();
            foreach(var t in candidates.Where(t=>t.name=="CoffeePot"||t.name=="CoffeeCupC"))
            {var b=B(t.gameObject);var tray=trays.OrderBy(t2=>(B(t2.gameObject).center-b.center).sqrMagnitude).FirstOrDefault();if(tray && (B(tray.gameObject).center-b.center).sqrMagnitude<1)SetBottom(t.gameObject,new Vector3(b.center.x,B(tray.gameObject).max.y+.002f,b.center.z));}
            int aligned=0;
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                if(light.type==LightType.Directional||!light.transform.parent)continue;
                var fixture=light.transform.parent.gameObject;var bounds=B(fixture);
                if(fixture.name.StartsWith("RoofLamp")){light.transform.position=new Vector3(bounds.center.x,bounds.min.y+.10f,bounds.center.z);aligned++;}
            }
            foreach(int side in new[]{-1,1})
            {
                Put("furniture/LoungerC Variant",new Vector3(side*22,.05f,-16),0,p);
                var sideTable=Put("furniture/PavillonSideTable",new Vector3(side*24,.05f,-16),0,p);
                Put("Props/LanternB",new Vector3(side*24,B(sideTable).max.y,-16),side*25,p);
                Put("Props/GrandSunshade",new Vector3(side*22,.05f,-13.5f),0,p,Vector3.one*.8f);
                for(int i=0;i<3;i++)Put("furniture/BenchStraight",new Vector3(side*5+(i-1)*.75f,.05f,41.5f),180,p);
            }
            changes.AppendLine($"POLISH: reading lamp separated from books; guest sofa faces entrance; 2 vendor window modules; coffee tray heights; {aligned} roof light positions aligned to fixtures; south sun loungers and arrival benches.");
            var hide=UnityEngine.Object.FindAnyObjectByType<BeachVillaHideLocations>();
            for(int i=0;i<hide.spots.Length;i++)
            {
                var spot=hide.spots[i];
                if(spot.label=="Guest sitting room sofa side"){spot.position=new Vector3(30.4f,.19f,14.4f);spot.approach=new Vector3(30.9f,.1f,15.1f);}
                if(spot.label=="Guest sitting room table foot"){spot.position=new Vector3(28.65f,.61f,18.65f);spot.approach=new Vector3(27.5f,.1f,18.5f);}
                if(spot.label=="Guest tea books"){spot.position=new Vector3(29.2f,.98f,25.9f);spot.approach=new Vector3(30.2f,.1f,25);}
                hide.spots[i]=spot;
            }
            EditorUtility.SetDirty(hide);
            var scene=SceneManager.GetActiveScene();Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.AppendAllText(Report+"/changes.txt",changes.ToString());Dump(scene,"after");Validate();InteriorMovement();AssetDatabase.SaveAssets();
        }
        static void InteriorMovement()
        {
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();var body=walk.GetComponent<CharacterController>();var saved=walk.transform.position;bool enabled=body.enabled;
            var log=new StringBuilder("Additional interior CharacterController.Move checks (1/60s, 2.6m/s).\n");
            var paths=new[]{("main living passage",new Vector3(6.8f,1.055f,6),new Vector3(6.8f,1.055f,15)),("reading approach",new Vector3(6.8f,1.055f,10.5f),new Vector3(4.5f,1.055f,10.5f)),("guest north passage",new Vector3(27.2f,.105f,18),new Vector3(27.2f,.105f,24)),("upper lounge passage",new Vector3(7.5f,3.855f,13),new Vector3(7.5f,3.855f,21))};
            try{foreach(var path in paths){body.enabled=false;walk.transform.position=path.Item2;body.enabled=true;Physics.SyncTransforms();float vy=0;int stall=0;
                for(int step=0;step<1500;step++){var d=path.Item3-walk.transform.position;d.y=0;if(d.magnitude<.12f)break;vy=body.isGrounded?-2:Mathf.Max(-30,vy-9.81f/60);var before=walk.transform.position;body.Move(Vector3.ClampMagnitude(d,2.6f/60)+Vector3.up*vy/60);Physics.SyncTransforms();stall=Vector3.Distance(before,walk.transform.position)<.001f?stall+1:0;if(stall>60||walk.transform.position.y< -5)break;}
                var remaining=path.Item3-walk.transform.position;remaining.y=0;log.AppendLine($"{path.Item1}: pass={remaining.magnitude<.15f}; remaining={remaining.magnitude:F3}; end={walk.transform.position:F3}");}}
            finally{body.enabled=false;walk.transform.position=saved;body.enabled=enabled;Physics.SyncTransforms();File.WriteAllText(Report+"/interior-movement.txt",log.ToString());}
        }
        [MenuItem("Rubber/Layout Review/6 Close Original Deck Junction - No Bake")]
        public static void CloseJunction()
        {
            Check();revision=GameObject.Find("Beach Villa - Layout Revision").transform;
            if(revision.Find("Original pool deck junction"))throw new InvalidOperationException("Junction already closed.");
            changes=new StringBuilder();var p=Group("Original pool deck junction");
            // Narrow L-shaped exposed trench between original pool platform and new circulation.
            for(int z=22;z<34;z+=2)Put("floors/Floor2x2",new Vector3(-17,-.95f,z+1),0,p);
            for(int x=-16;x< -8;x+=2)Put("floors/Floor2x2",new Vector3(x+1,-.95f,33),0,p);
            Physics.SyncTransforms();int missing=0;
            foreach(var c in p.GetComponentsInChildren<Collider>())if(!c.Raycast(new Ray(c.bounds.center+Vector3.up*2,Vector3.down),out var hit,4))missing++;
            File.AppendAllText(Report+"/changes.txt",changes.ToString());
            var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Dump(scene,"after");Validate();InteriorMovement();
            File.AppendAllText(Report+"/validation.txt",$"Original deck junction: 10 vendor floor modules; missing center support={missing}. Total new paving=719.\n");
        }
        static void CorrectOutdoorDetails(Transform expanded)
        {
            var props=expanded.Find("Furniture and lived-in details");
            var all=props.Cast<Transform>().Where(t=>t.gameObject.activeSelf).ToArray();
            foreach(var t in all)
            {
                var source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);var b=B(t.gameObject);
                if(source.Contains("/lighting/FloorLight"))
                {
                    // Place the point source inside the luminaire instead of above/outside its head.
                    foreach(var lamp in t.GetComponentsInChildren<Light>())lamp.transform.position=new Vector3(b.center.x,b.max.y-.08f,b.center.z);
                }
                if(source.Contains("/Props/Book"))
                {
                    t.rotation=Quaternion.Euler(0,15,90);var near=all.Where(a=>a!=t && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(a.gameObject).Contains("Table")).OrderBy(a=>(B(a.gameObject).center-b.center).sqrMagnitude).FirstOrDefault();
                    if(near)SetBottom(t.gameObject,new Vector3(b.center.x,B(near.gameObject).max.y+.005f,b.center.z));
                }
                if(source.Contains("/Props/LargePlate")||source.Contains("/Props/Glas"))
                {var near=all.Where(a=>a.name=="DiningTable").OrderBy(a=>(B(a.gameObject).center-b.center).sqrMagnitude).FirstOrDefault();if(near)SetBottom(t.gameObject,new Vector3(b.center.x,B(near.gameObject).max.y+.004f,b.center.z));}
                if(source.Contains("/Props/TowelA")){var near=all.Where(a=>a.name=="Lounger").OrderBy(a=>(B(a.gameObject).center-b.center).sqrMagnitude).FirstOrDefault();if(near)SetBottom(t.gameObject,new Vector3(b.center.x,.49f,b.center.z));}
            }
        }
        [MenuItem("Rubber/Layout Review/3 Validate Layout Only")]
        public static void Validate()
        {
            Check();Physics.SyncTransforms();var log=new StringBuilder("Layout validation; no lighting work.\n");
            var terrain=UnityEngine.Object.FindAnyObjectByType<Terrain>();var data=terrain.terrainData;var holes=data.GetHoles(0,0,data.holesResolution,data.holesResolution);int absent=0;foreach(bool ground in holes)if(!ground)absent++;
            log.AppendLine("Terrain hole cells="+absent);
            var root=GameObject.Find("Beach Villa - Layout Revision");
            var wall=root.transform.Find("Continuous perimeter - matching wall modules").GetComponentsInChildren<Renderer>();
            int wallGaps=0;
            foreach(int side in new[]{-1,1})for(float z=-22.5f;z<49.5f;z+=.25f)if(!Physics.Raycast(new Vector3(side*38, .65f,z),new Vector3(side,0,0),3))wallGaps++;
            foreach(int side in new[]{-1,1})for(float x=-39.5f;x<39.5f;x+=.25f)if(!Physics.Raycast(new Vector3(x,.65f,side<0?-21:48),new Vector3(0,0,side),3))wallGaps++;
            log.AppendLine($"Perimeter modules={wall.Length}; sampled wall gaps={wallGaps}; top min={wall.Min(r=>r.bounds.max.y):F3}; top max={wall.Max(r=>r.bounds.max.y):F3}");
            int pavingMiss=0;var paving=root.transform.Find("Continuous paving - deduplicated 2m grid").GetComponentsInChildren<Collider>();
            foreach(var c in paving){var b=c.bounds;foreach(float dx in new[]{-.98f,0,.98f})foreach(float dz in new[]{-.98f,0,.98f})if(!c.Raycast(new Ray(new Vector3(b.center.x+dx,1,b.center.z+dz),Vector3.down),out var hit,2))pavingMiss++;}
            log.AppendLine($"Paving modules={paving.Length}; per-module 9-point coverage misses={pavingMiss}");
            File.WriteAllText(Report+"/validation.txt",log.ToString());BeachVillaMovementChecks.Run();
        }
        static void Check(){if(Application.isPlaying || Lightmapping.isRunning || BuildPipeline.isBuildingPlayer || SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Beach Villa in Edit mode, with no bake/build running.");}
        [MenuItem("Rubber/Layout Review/1 Survey Current and Sample Scenes")]
        public static void Survey()
        {
            Check();Directory.CreateDirectory(Report);
            var scene=SceneManager.GetActiveScene();
            // Preserve the user's current unsaved edits before inspecting sample scenes.
            EditorSceneManager.SaveScene(scene);
            File.Copy(ScenePath,Report+"/BeforeLayout.unity.backup",true);
            Dump(scene,"current");
            foreach(var name in new[]{"Modern Villa","Spa","Showroom"})
            {
                var preview=EditorSceneManager.OpenPreviewScene("Assets/Modern Villa/Scenes/"+name+".unity");
                try{Dump(preview,"sample-"+name.Replace(' ','-'));}
                finally{EditorSceneManager.ClosePreviewScene(preview);}
            }
            Debug.Log("Layout survey complete; no lighting bake: "+Report);
        }
        static void Dump(Scene scene,string file)
        {
            var s=new StringBuilder("path\tprefab\tmin\tmax\trotation\n");
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(!t.gameObject.activeInHierarchy)continue;
                var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if(t.parent && PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject)!=t.gameObject && !t.GetComponent<Light>() && t.parent!=root.transform)continue;
                var b=BeachVillaExpansionSurvey.BoundsOf(t.gameObject);
                s.AppendLine($"{BeachVillaExpansionSurvey.PathOf(t)}\t{path}\t{b.min:F3}\t{b.max:F3}\t{t.eulerAngles:F1}");
            }
            File.WriteAllText(Report+"/"+file+".tsv",s.ToString());
        }
        [MenuItem("Rubber/Layout Review/4 Capture Review in Play - No Bake")]
        public static void Capture()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();walk.StartCoroutine(Captures(walk));
        }
        static IEnumerator Captures(BeachVillaWalkthrough walk)
        {
            var night=walk.GetComponent<BeachVillaDarkNight>();night.SetNight(false);walk.enabled=false;
            var body=walk.GetComponent<CharacterController>();body.enabled=false;
            bool fog=RenderSettings.fog;RenderSettings.fog=false;
            var views=new[]{
                ("01-overview",new Vector3(-49,61,-48),new Vector3(0,0,13)),
                ("02-boundary",new Vector3(-35,2,-19),new Vector3(-38,1,24)),
                ("03-garden",new Vector3(-19,1.7f,-5),new Vector3(-29,1.2f,10)),
                ("04-pool",new Vector3(-17,1.7f,-7),new Vector3(5,2,13)),
                ("05-living",new Vector3(6.45f,2.65f,10.8f),new Vector3(4.1f,1.6f,6.5f)),
                ("06-dining",new Vector3(6.8f,2.65f,15.6f),new Vector3(4.8f,1.7f,12.6f)),
                ("07-upper-lounge",new Vector3(7.9f,5.45f,21.4f),new Vector3(9.8f,4.5f,16.6f)),
                ("08-guest",new Vector3(27.4f,1.7f,17.8f),new Vector3(29.2f,1,12.5f)),
                ("09-arrival",new Vector3(5,1.7f,43),new Vector3(6,2,15))};
            foreach(var v in views)
            {
                walk.transform.position=v.Item2-Vector3.up*1.65f;walk.transform.rotation=Quaternion.identity;walk.view.rotation=Quaternion.LookRotation(v.Item3-v.Item2);
                yield return new WaitForSecondsRealtime(1.5f);yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/"+v.Item1+".png"));yield return new WaitForSecondsRealtime(.8f);
            }
            RenderSettings.fog=fog;walk.enabled=true;body.enabled=true;walk.GoTo(0);
            File.WriteAllText(Report+"/capture.txt","Layout preview, existing lighting only, fog temporarily hidden for geometry inspection; no new bake. "+DateTime.UtcNow.ToString("O"));
        }
    }
}
