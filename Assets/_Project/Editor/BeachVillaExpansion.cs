using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    public static class BeachVillaExpansion
    {
        const string ScenePath="Assets/Modern Villa/Scenes/Beach Villa.unity";
        const string Art="Assets/_Project/Lighting/BeachVillaExpanded";
        const string Report=BeachVillaExpansionSurvey.Reports;
        static Transform root,landscape,paths,props;
        static List<BeachVillaHideLocations.Spot> spots;
        static StringBuilder log;
        static int decorLayer;
        static GameObject Piece(string relative,Vector3 bottom,float yaw=0,Transform parent=null,Vector3? scale=null)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/"+relative+".prefab");
            if(!prefab)throw new InvalidOperationException("Missing vendor prefab: "+relative);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent?parent:props,false);
            go.transform.rotation=Quaternion.Euler(0,yaw,0);
            if(scale.HasValue)go.transform.localScale=scale.Value;
            var b=BeachVillaExpansionSurvey.BoundsOf(go);
            go.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf=r.GetComponent<MeshFilter>();
                bool small=r.bounds.size.x*r.bounds.size.y*r.bounds.size.z<.03f;
                var flags=StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic;
                if(!small)flags|=StaticEditorFlags.ContributeGI;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,flags);
                r.receiveGI=small?ReceiveGI.LightProbes:ReceiveGI.Lightmaps;
                r.lightProbeUsage=LightProbeUsage.BlendProbes;r.scaleInLightmap=relative.StartsWith("floors/")?.35f:1;
                r.lightmapIndex=-1;
            }
            log.AppendLine($"ADD {relative}; base={bottom}; yaw={yaw}; scale={go.transform.localScale}");
            return go;
        }
        static Transform Group(string name){var go=new GameObject(name);go.transform.SetParent(root,false);return go.transform;}
        static void Deck(string name,float x,float z,int nx,int nz)
        {
            var group=Group(name);
            for(int ix=0;ix<nx;ix++)for(int iz=0;iz<nz;iz++)
                Piece("floors/Floor2x2",new Vector3(x+(ix-(nx-1)*.5f)*2,-.95f,z+(iz-(nz-1)*.5f)*2),0,group);
        }
        static void Route(string name,Vector3 a,Vector3 b,float width=3)
        {
            var group=Group(name);float length=Vector3.Distance(a,b);int count=Mathf.CeilToInt(length/2);
            float yaw=Mathf.Atan2(b.x-a.x,b.z-a.z)*Mathf.Rad2Deg;
            for(int i=0;i<count;i++)Piece("floors/Floor2x2",Vector3.Lerp(a,b,(i+.5f)/count)+Vector3.down*.95f,yaw,group,new Vector3(width/2,1,length/count/2));
        }
        static void Hide(string name,Vector3 pos,Vector3 approach)=>spots.Add(new BeachVillaHideLocations.Spot{label=name,position=pos,approach=approach});
        static void PlantBed(float x,float z,float yaw=0)
        {
            Piece("Stoneboxes/StoneboxMedium",new Vector3(x,.05f,z),yaw,landscape);
            for(int i=-1;i<=1;i++)Piece(i==0?"Plants/PlantA":"Plants/PlantC",new Vector3(x,.72f,z)+Quaternion.Euler(0,yaw,0)*new Vector3(i*1.1f,0,0),i*35+15,landscape);
            Hide("Planter pocket "+spots.Count,new Vector3(x+1.5f,.82f,z),new Vector3(x+1.6f,.1f,z-1.2f));
        }
        static void Seating(float x,float z,float yaw,string label)
        {
            var p=new Vector3(x,.05f,z);var q=Quaternion.Euler(0,yaw,0);
            Piece("furniture/CouchD",p+q*new Vector3(0,0,1.65f),yaw+180);
            Piece("furniture/GardenChair",p+q*new Vector3(-1.9f,0,-.1f),yaw+80);
            Piece("furniture/GardenChair",p+q*new Vector3(1.9f,0,-.1f),yaw-80);
            Piece("furniture/CouchTableC",p,yaw);
            Piece("Props/TabletA",p+new Vector3(0,.45f,0),yaw);
            Piece("Props/CoffeePot",p+q*new Vector3(.12f,.52f,0),yaw);
            Piece("Props/CoffeeCupC",p+q*new Vector3(-.18f,.52f,.1f),yaw+20);
            Piece("Plants/PottedPlantA",p+q*new Vector3(2.5f,0,1.5f),yaw);
            Hide(label+" sofa side",p+q*new Vector3(1.35f,.14f,1.5f),p+q*new Vector3(2.5f,0,.65f));
            Hide(label+" table foot",p+q*new Vector3(.3f,.14f,0),p+q*new Vector3(0,0,-1.3f));
        }
        [MenuItem("Rubber/Expansion/2 Extend Existing Beach Villa")]
        public static void Build()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ScenePath || Application.isPlaying)throw new InvalidOperationException("Open Beach Villa outside Play.");
            if(GameObject.Find("Beach Villa - Expanded Grounds"))throw new InvalidOperationException("Expansion exists; inspect and refine it instead of duplicating.");
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene backup save failed.");
            Directory.CreateDirectory(Report);Directory.CreateDirectory(Art);
            var backup="Library/BeachVillaExpansionBackup-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
            File.Copy(ScenePath,backup+"/Beach Villa.unity");
            File.WriteAllText(Report+"/backup.txt",backup);
            log=new StringBuilder();spots=new List<BeachVillaHideLocations.Spot>();
            var r=new GameObject("Beach Villa - Expanded Grounds");root=r.transform;landscape=Group("Layered coastal planting");paths=Group("Circulation");props=Group("Furniture and lived-in details");
            // Existing main villa and pool remain in place. Expansion occupies separate courtyards.
            TerrainAndSky();
            Deck("West wellness court",-28,24,6,8);
            Deck("East guest terrace",29,17,7,13);
            Deck("North arrival courtyard",5,40,11,5);
            Deck("South beach lounge",0,-16,12,4);
            Deck("Outdoor dining court",-28,3,6,6);
            Route("West garden spine",new Vector3(-20,0,-16),new Vector3(-20,0,42));
            Route("East garden spine",new Vector3(21,0,-16),new Vector3(21,0,42));
            Route("North loop",new Vector3(-30,0,36),new Vector3(30,0,36));
            Route("South loop",new Vector3(-29,0,-10),new Vector3(30,0,-10));
            Route("Pool return west",new Vector3(-29,0,19),new Vector3(-16,0,19));
            Route("Pool return south",new Vector3(-10,0,-10),new Vector3(-10,0,-2));
            Route("East villa connection",new Vector3(16,0,30),new Vector3(29,0,30));
            Route("Beach access",new Vector3(0,0,-10),new Vector3(0,0,-16),4);
            var guest=Piece("buildings/BuildingC",new Vector3(30,.05f,18),0,root);guest.name="Guest wing - vendor BuildingC";
            Piece("buildings/SpaPavillon",new Vector3(-28,.05f,25),0,root).name="Garden spa pavilion";
            Piece("Props/Jacuzzi",new Vector3(-28,.12f,25),0);
            Piece("Props/PavillonOpen",new Vector3(-28,.05f,3),0,root);
            Piece("Props/Barbecue",new Vector3(-32,.05f,6),90);
            var table=Piece("furniture/DiningTable",new Vector3(-28,.05f,3),0);
            for(int i=-1;i<=1;i++)foreach(int side in new[]{-1,1})
                Piece("furniture/DiningChair",new Vector3(-28+i*.8f,.05f,3+side*1.25f),side<0?0:180);
            foreach(float x in new[]{-28.7f,-28f,-27.3f})foreach(float z in new[]{2.7f,3.3f})
            {Piece("Props/LargePlate",new Vector3(x,.84f,z));Piece("Props/Glas",new Vector3(x+.17f,.84f,z));}
            Hide("Outdoor dining chair",new Vector3(-29,.2f,4.15f),new Vector3(-30,.1f,4.2f));
            Hide("Barbecue side",new Vector3(-32,.25f,7.3f),new Vector3(-31,.1f,7.5f));
            Seating(-28,13,90,"Garden conversation");
            Seating(29,33,180,"Guest terrace");
            Seating(6,-16,0,"Beach sunset lounge");
            Seating(-8,-16,0,"Beach reading lounge");
            // Reuse vendor seating and decorations at actual human scale.
            foreach(float x in new[]{-32f,-24f})
            {
                Piece("furniture/Lounger",new Vector3(x,.05f,31),180);
                Piece("furniture/PavillonSideTable",new Vector3(x+1.1f,.05f,31),0);
                Piece("Props/TowelA",new Vector3(x,.7f,31.6f),0);
                Hide("Spa lounger towel "+x,new Vector3(x,.8f,31.3f),new Vector3(x+1,.1f,30));
            }
            // A small library/tea destination in the open guest wing, never a duplicate main villa.
            Seating(30,13,0,"Guest sitting room");
            Piece("furniture/DiningTable",new Vector3(30,.05f,22),90);
            Piece("Props/BookC",new Vector3(30,.84f,22),0);
            Piece("Props/BookE",new Vector3(30.12f,.84f,22),12);
            Piece("Props/VaseA",new Vector3(30,.84f,23),0);
            Hide("Guest tea books",new Vector3(30.2f,.95f,22.3f),new Vector3(28.7f,.1f,22));
            foreach(var p in new[]{new Vector2(-34,12),new Vector2(-34,34),new Vector2(-24,-3),new Vector2(25,2),new Vector2(35,33),new Vector2(-7,40),new Vector2(15,40),new Vector2(-12,-19),new Vector2(12,-19)})PlantBed(p.x,p.y);
            BoundaryPlanting();
            foreach(var p in new[]{new Vector2(-28,3),new Vector2(-28,25),new Vector2(29,13),new Vector2(29,24),new Vector2(5,40),new Vector2(-8,-16)})
            {
                var fixture=Piece("lighting/FloorLight on Variant",new Vector3(p.x+2.8f,.05f,p.y),0);
                var lamp=new GameObject("Courtyard practical light").AddComponent<Light>();lamp.transform.SetParent(fixture.transform);lamp.transform.position=new Vector3(p.x+2.8f,.9f,p.y);
                lamp.type=LightType.Point;lamp.range=5;lamp.intensity=2;lamp.color=new Color(1,.75f,.48f);lamp.shadows=LightShadows.None;lamp.lightmapBakeType=LightmapBakeType.Mixed;
            }
            FixExistingScene();
            ConfigureCollisions();
            var hide=r.AddComponent<BeachVillaHideLocations>();hide.spots=spots.ToArray();hide.poolReturnPoint=new Vector3(-15.8f,.1f,9);
            var night=UnityEngine.Object.FindAnyObjectByType<BeachVillaDarkNight>();night.nightProbesReady=false;
            night.sceneLights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
            var outer=UnityEngine.Object.FindObjectsByType<ProbeVolume>().First(v=>v.name.Contains("terrace"));outer.transform.position=new Vector3(0,3,14);outer.size=new Vector3(86,14,82);
            var guestAPV=new GameObject("APV - guest and spa courts").AddComponent<ProbeVolume>();guestAPV.transform.SetParent(root);guestAPV.mode=ProbeVolume.Mode.Local;guestAPV.transform.position=new Vector3(29,2,19);guestAPV.size=new Vector3(13,7,28);guestAPV.overridesSubdivLevels=true;guestAPV.lowestSubdivLevelOverride=0;guestAPV.highestSubdivLevelOverride=1;
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();
            walk.checkpoints=walk.checkpoints.Concat(new[]{CP("F7 | West dining garden",-20,0,3,270),CP("F8 | Spa courtyard",-20,0,25,270),CP("F9 | Guest wing",21,0,18,90),CP("F10 | Arrival garden",5,0,39,180),CP("F11 | Beach lounge",0,0,-12,180)}).ToArray();
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Report+"/changes.txt",log.ToString());
        }
        static BeachVillaWalkthrough.Checkpoint CP(string name,float x,float y,float z,float yaw)=>new BeachVillaWalkthrough.Checkpoint{label=name,feet=new Vector3(x,y+.1f,z),yaw=yaw};
        static void TerrainAndSky()
        {
            var terrain=UnityEngine.Object.FindAnyObjectByType<Terrain>();var original=terrain.terrainData;var origin=terrain.transform.position;
            var data=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(data,Art+"/ExpandedCoast.asset");
            // Extend the existing terrain; preserve the old height field beneath the original building/pool.
            var start=new Vector3(-160,-12,-140);data.size=new Vector3(360,60,360);data.heightmapResolution=1025;int res=data.heightmapResolution;var heights=new float[res,res];
            for(int z=0;z<res;z++)for(int x=0;x<res;x++)
            {
                float wx=start.x+x/(float)(res-1)*data.size.x,wz=start.z+z/(float)(res-1)*data.size.z;
                float dx=Mathf.Max(-19-wx,wx-19),dz=Mathf.Max(-10-wz,wz-32),outside=Mathf.Max(dx,dz);
                float old=original.GetInterpolatedHeight(Mathf.Clamp01((wx-origin.x)/original.size.x),Mathf.Clamp01((wz-origin.z)/original.size.z))+origin.y;
                float coast=-.14f;
                if(wz < -24)coast-=Mathf.SmoothStep(0,3,Mathf.InverseLerp(-24,-65,wz));
                float rim=Mathf.Max(Mathf.Abs(wx)-42,wz-50);
                if(rim>0)coast+=Mathf.SmoothStep(0,4,Mathf.Clamp01(rim/18))*(.65f+.35f*Mathf.PerlinNoise(wx*.04f,wz*.04f));
                float h=Mathf.Lerp(old,coast,Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/3)));
                heights[z,x]=Mathf.Clamp01((h-start.y)/data.size.y);
            }
            data.SetHeights(0,0,heights);terrain.terrainData=data;terrain.transform.position=start;terrain.GetComponent<TerrainCollider>().terrainData=data;
            terrain.drawInstanced=true;terrain.heightmapPixelError=8;terrain.basemapDistance=100;
            var sky=new Material(Shader.Find("Skybox/Cubemap"));sky.name="Beach Villa coastal sky";
            sky.SetTexture("_Tex",AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Modern Villa/Textures/mv_skybox.exr"));sky.SetFloat("_Exposure",.8f);sky.SetFloat("_Rotation",155);
            AssetDatabase.CreateAsset(sky,Art+"/CoastalSky.mat");RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Skybox;RenderSettings.ambientIntensity=.65f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.64f,.72f,.75f);RenderSettings.fogDensity=.003f;
            log.AppendLine("Existing terrain extended to 360m background with central height preservation; vendor panoramic HDR sky assigned; coastal haze.");
        }
        static void BoundaryPlanting()
        {
            // Three landward sides: staggered palms, low planting and real modular walls.
            var random=new System.Random(2471);
            foreach(int side in new[]{-1,1})for(int i=0;i<17;i++)
            {
                float z=-18+i*4;float x=side*40;
                Piece("Walls/Wall2x2,5",new Vector3(x,-.14f,z),90,landscape,new Vector3(2,.6f,1));
                Piece(i%2==0?"Plants/PalmA":"Plants/PalmC",new Vector3(x-side*(1.5f+(float)random.NextDouble()),-.14f,z+(float)random.NextDouble()),(float)random.NextDouble()*360,landscape,Vector3.one*(.8f+(float)random.NextDouble()*.3f));
                Piece("Plants/Shrub",new Vector3(x-side*1.3f,-.14f,z+1.4f),i*31,landscape,Vector3.one*2.1f);
            }
            for(int i=0;i<20;i++)
            {
                float x=-38+i*4;
                Piece("Walls/Wall2x2,5",new Vector3(x,-.14f,50),0,landscape,new Vector3(2,.6f,1));
                if(i%2==0)Piece("Plants/PalmB",new Vector3(x,-.14f,47.5f),i*43,landscape);
                Piece("Plants/Shrub",new Vector3(x,-.14f,48.4f),i*25,landscape,Vector3.one*2.2f);
            }
            // Low seaside barrier preserves a broad horizon while making the playable edge legible.
            for(int i=0;i<40;i++)Piece("railing/GlasRailing2m",new Vector3(-39+i*2,-.14f,-23),0,landscape);
            foreach(var p in new[]{new Vector2(-32,-15),new Vector2(-17,-19),new Vector2(18,-18),new Vector2(31,-15),new Vector2(-15,40),new Vector2(19,43)})
                Piece("Plants/PalmC",new Vector3(p.x,-.14f,p.y),p.x*11,landscape);
        }
        static void FixExistingScene()
        {
            var sunshade=SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g=>g.name=="Sunshade");
            if(sunshade){sunshade.transform.rotation=Quaternion.Euler(0,15,0);var b=BeachVillaExpansionSurvey.BoundsOf(sunshade);sunshade.transform.position+=new Vector3(10,.05f,-16)-new Vector3(b.center.x,b.min.y,b.center.z);log.AppendLine("Moved tilted/buried existing Sunshade to beach lounge and stood it upright.");}
            // Keep a clear return edge along the west side of the original pool.
            var palm=GameObject.Find("Plants/mv_PalmA");if(palm){palm.transform.position=new Vector3(-17.5f,-.14f,-5);log.AppendLine("Moved pool-front palm away from return approach.");}
            foreach(var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var r in rootObj.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n=r.name.ToLowerInvariant();
                if(n.Contains("loungechair")){GameObjectUtility.SetStaticEditorFlags(r.gameObject,GameObjectUtility.GetStaticEditorFlags(r.gameObject)&~StaticEditorFlags.ContributeGI);r.receiveGI=ReceiveGI.LightProbes;r.lightmapIndex=-1;}
            }
        }
        static void ConfigureCollisions()
        {
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");decorLayer=LayerMask.NameToLayer("MapSmallDecor");
            if(decorLayer<0){for(int i=31;i>=8;i--)if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)){decorLayer=i;layers.GetArrayElementAtIndex(i).stringValue="MapSmallDecor";break;}tags.ApplyModifiedProperties();}
            if(decorLayer<0)throw new InvalidOperationException("No free decor layer.");
            int small=0,cloth=0,repaired=0;
            foreach(var obj in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var c in obj.GetComponentsInChildren<Collider>(true))
            {
                if(c is TerrainCollider || c is CharacterController)continue;
                string n=c.name.ToLowerInvariant();var b=c.bounds;float max=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
                if(n.Contains("curtain") || n.Contains("carpet")){c.enabled=false;cloth++;}
                else if(max<.55f || n.Contains("pillow") || n.Contains("towel") || n.Contains("flower") || n.Contains("shrub")){c.gameObject.layer=decorLayer;small++;}
                // Full boxes on a wall with an opening block the doorway. Use the vendor mesh instead.
                if(c is BoxCollider && n.Contains("wall") && (n.Contains("door")||n.Contains("window")))
                {
                    var mf=c.GetComponent<MeshFilter>();if(mf && mf.sharedMesh){c.enabled=false;var mc=c.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;repaired++;}
                }
            }
            var body=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>().GetComponent<CharacterController>();body.excludeLayers=body.excludeLayers.value | (1<<decorLayer);
            log.AppendLine($"COLLIDERS small decor excluded only for CharacterController={small}; cloth/carpet blockers disabled={cloth}; doorway/window boxes replaced with source mesh={repaired}; player radius={body.radius}; step={body.stepOffset}");
        }
    }
}
