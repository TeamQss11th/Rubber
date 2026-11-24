using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubber.Gameplay.Ducks;
using Rubber.World;
namespace Rubber.EditorTools
{
    public static class VillaDuckHuntSetup
    {
        const string Folder="Docs/DuckHunt";
        static IEnumerable<Transform> All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true));
        static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)return new Bounds(t.position,Vector3.zero);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
        public static void Survey()
        {
            Directory.CreateDirectory(Folder);var lines=new List<string>();
            foreach(var t in All().Where(t=>t.gameObject.activeInHierarchy))
            {
                string path=ModernVillaSurfaceAudit.PathOf(t);
                if(t.parent==null || path.StartsWith("Modern Villa - Interior Furnishing/")&&t.parent.parent!=null&&t.parent.parent.name=="Modern Villa - Interior Furnishing" || path.StartsWith("Modern Villa - Search Details/")&&t.GetComponent<Renderer>() || PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&new[]{"table","bench","chair","plant","fence","wall","couch","lounger"}.Any(n=>t.name.ToLowerInvariant().Contains(n)))
                {var b=BoundsOf(t);lines.Add(path+" min="+b.min.ToString("F2")+" max="+b.max.ToString("F2")+" colliders="+t.GetComponentsInChildren<Collider>().Length);}
            }
            File.WriteAllLines(Folder+"/survey.txt",lines);
            var hits=new List<string>();foreach(var spot in Spots.Skip(19))foreach(var h in Physics.RaycastAll(spot.hint+Vector3.up*3,Vector3.down,6).OrderBy(h=>h.distance))hits.Add(spot.name+" y="+h.point.y+" "+ModernVillaSurfaceAudit.PathOf(h.transform));File.WriteAllLines(Folder+"/support-rays.txt",hits);
        }
        struct Spot
        {
            public string name;public Vector3 hint;
            public Spot(string n,float x,float y,float z){name=n;hint=new Vector3(x,y,z);}
        }
        static readonly Spot[] Spots={
            new Spot("Main lounge - beside couch table",-5.95f,.75f,3.25f),
            new Spot("Dining - under table",-4.25f,.75f,8.1f),
            new Spot("Dining - beside corner planter",-2.95f,.75f,9.7f),
            new Spot("Dining - behind flowers",-4.7f,1.495f,8.4f),
            new Spot("Study - behind reference books",-5.55f,4.3f,5.0f),
            new Spot("Study - beside window armchair",-6.5f,3.555f,1.8f),
            new Spot("Study - coffee table edge",-4.85f,3.907f,2.7f),
            new Spot("Reading - beside floor planter",-3.2f,3.555f,8.4f),
            new Spot("Reading - between chairs",-4.9f,3.555f,8.5f),
            new Spot("Reading - behind coffee book",-5.05f,3.907f,7.48f),
            new Spot("Living - behind sofa arm",5.65f,.25f,16.3f),
            new Spot("Living - beside low palm",9.3f,.25f,14.65f),
            new Spot("Living - coffee table edge",7.1f,.65f,14.1f),
            new Spot("Breakfast - beside coffee tray",5.95f,.995f,19.38f),
            new Spot("Breakfast - near window planter",8.5f,.25f,20.0f),
            new Spot("Studio - behind reference books",7.85f,3.795f,13.55f),
            new Spot("Studio - beside reading chair",4.5f,3.05f,12.55f),
            new Spot("Retreat - beside tea table",6.25f,3.05f,18.75f),
            new Spot("Retreat - beside low palm",7.15f,3.05f,19.3f),
            new Spot("Front pool - beside lounge corner",7.4f,-.24f,6.75f),
            new Spot("Front pool - beside lamp table",8.72f,.10f,5.0f),
            new Spot("Garden - beside lounger",-5.15f,.25f,21.7f),
            new Spot("Garden - behind second lounger",3.55f,.25f,26.7f),
            new Spot("Garden - beside bench corner",-13.9f,.25f,20.85f),
            new Spot("Garden - beside garden chair",-.95f,.25f,17.55f)
        };
        static bool FindPosition(Spot spot,List<Vector3> placed,out Vector3 position,out Vector3 approach)
        {
            position=approach=default;
            foreach(var offset in (from x in Enumerable.Range(-3,7) from z in Enumerable.Range(-3,7) select new Vector3(x*.14f,0,z*.14f)).OrderBy(v=>v.sqrMagnitude))
            {
                var p=spot.hint+offset;
                if(!Physics.Raycast(p+Vector3.up*.28f,Vector3.down,out var h,.58f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||h.normal.y<.92f)continue;
                p.y=h.point.y+.006f;
                if(placed.Any(q=>Vector3.Distance(q,p)<.65f))continue;
                if(Physics.CheckBox(p+Vector3.up*.10f,new Vector3(.135f,.085f,.14f),Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))continue;
                bool support=true;
                foreach(var corner in new[]{new Vector3(.10f,0,.10f),new Vector3(-.10f,0,.10f),new Vector3(.10f,0,-.10f),new Vector3(-.10f,0,-.10f)})
                    if(!Physics.Raycast(p+corner+Vector3.up*.06f,Vector3.down,out var hit,.12f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-h.point.y)>.025f){support=false;break;}
                if(!support||!FindApproach(p,out approach))continue;
                position=p;return true;
            }
            return false;
        }
        public static bool FindApproach(Vector3 p,out Vector3 approach)
        {
            approach=default;
            foreach(float radius in new[]{.9f,1.4f,2f,2.6f})for(int angle=0;angle<360;angle+=30)
            {
                var q=p+Quaternion.Euler(0,angle,0)*Vector3.forward*radius;
                if(!Physics.Raycast(q+Vector3.up*.5f,Vector3.down,out var floor,2.1f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||floor.normal.y<.9f)continue;
                // Only use an actual floor/roof/terrain, not the top of a chair.
                var name=floor.collider.name.ToLowerInvariant();if(!new[]{"floor","roof","terrain","ground","stair","terrace"}.Any(name.Contains))continue;
                q=floor.point+Vector3.up*.04f;
                if(Physics.CheckCapsule(q+Vector3.up*.31f,q+Vector3.up*1.49f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))continue;
                var eye=q+Vector3.up*1.6f;var target=p+Vector3.up*.10f;var delta=target-eye;
                if(delta.magnitude>4.3f||Physics.Raycast(eye,delta.normalized,delta.magnitude-.05f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))continue;
                approach=q;return true;
            }
            return false;
        }
        public static void Install()
        {
            var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.path!=ModernVillaSetup.ScenePath)throw new InvalidOperationException("Modern Villa Edit mode required.");
            if(GameObject.Find("Villa Duck Hunt - 25"))throw new InvalidOperationException("Already installed; preserve edits.");
            Directory.CreateDirectory(Folder);EditorSceneManager.SaveScene(scene);File.Copy(scene.path,Folder+"/BeforeHunt.unity.backup",true);
            var originals=UnityEngine.Object.FindObjectsByType<RubberDuckInteractable>().OrderBy(d=>d.Data.Id).ToArray();
            if(originals.Length!=5)throw new InvalidOperationException("Expected original five ducks.");
            foreach(var d in originals)d.gameObject.SetActive(false);
            Physics.SyncTransforms();var positions=new List<Vector3>();var approaches=new List<Vector3>();var report=new List<string>();
            for(int i=0;i<Spots.Length;i++)
            {
                if(!FindPosition(Spots[i],positions,out var p,out var eye))
                {foreach(var d in originals)d.gameObject.SetActive(true);File.WriteAllLines(Folder+"/placement.txt",report);throw new InvalidOperationException("No safe reachable placement: "+Spots[i].name);}
                positions.Add(p);approaches.Add(eye);report.Add((i+1)+" "+Spots[i].name+" position="+p.ToString("F3")+" approach="+eye.ToString("F3"));
            }
            const string dataFolder="Assets/_Project/ScriptableObjects/Ducks/Villa";
            var root=new GameObject("Villa Duck Hunt - 25");var registry=UnityEngine.Object.FindAnyObjectByType<RubberDuckReturnRegistry>();var dataList=new List<RubberDuckData>();
            for(int i=0;i<25;i++)
            {
                var source=originals[i%5];var go=UnityEngine.Object.Instantiate(source.gameObject,root.transform);go.name=$"Duck {i+1:00} - {Spots[i].name}";
                go.transform.SetPositionAndRotation(positions[i],Quaternion.Euler(0,(i*137)%360,0));
                var data=i<5?source.Data:UnityEngine.Object.Instantiate(source.Data);
                if(i>=5)AssetDatabase.CreateAsset(data,dataFolder+$"/VillaDuck{i+1:00}.asset");
                var serialized=new SerializedObject(data);serialized.FindProperty("id").intValue=1001+i;serialized.FindProperty("displayName").stringValue=$"{source.Data.DisplayName.Split('#')[0].Trim()} #{i/5+1}";serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);
                go.GetComponent<RubberDuckInteractable>().Configure(data);go.GetComponent<VillaDuckReturn>().registry=registry;go.GetComponent<VillaDuckBuoyancy>().water=null;
                go.SetActive(true);dataList.Add(data);
            }
            registry.Configure(dataList);EditorUtility.SetDirty(registry);
            foreach(var d in originals)UnityEngine.Object.DestroyImmediate(d.gameObject);
            var boundary=new GameObject("Villa Boundary - Invisible Colliders");
            void Wall(string name,Vector3 center,Vector3 size){var g=new GameObject(name);g.transform.SetParent(boundary.transform);g.transform.position=center;g.isStatic=true;g.AddComponent<BoxCollider>().size=size;}
            // Outside all surveyed building, pool, pavilion and terrace bounds. Overlap corners.
            Wall("West",new Vector3(-19.25f,5,14.25f),new Vector3(.5f,30,37.5f));
            Wall("East",new Vector3(13.25f,5,14.25f),new Vector3(.5f,30,37.5f));
            Wall("South",new Vector3(-3,5,-4.25f),new Vector3(33,30,.5f));
            Wall("North",new Vector3(-3,5,32.75f),new Vector3(33,30,.5f));
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            report.Add("25 unique IDs; 5 models x 5; 4 static BoxCollider barriers; no renderer or Rigidbody on barriers.");File.WriteAllLines(Folder+"/placement.txt",report);
        }
    }
}
