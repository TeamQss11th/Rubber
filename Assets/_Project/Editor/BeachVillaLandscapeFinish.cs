using System.Linq;
using Rubber.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rubber.EditorTools
{
    public static class BeachVillaLandscapeFinish
    {
        public static void Apply()
        {
            var root=GameObject.Find("Beach Villa - Expanded Grounds");
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Lighting/BeachVillaExpanded/CoastalSky.mat");
            // The package HDR is a photographed night scene. Use a tuned daytime atmosphere instead.
            sky.shader=Shader.Find("Skybox/Procedural");sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",.85f);
            sky.SetColor("_SkyTint",new Color(.43f,.57f,.68f));sky.SetColor("_GroundColor",new Color(.49f,.47f,.40f));sky.SetFloat("_Exposure",1.1f);
            RenderSettings.ambientIntensity=.65f;RenderSettings.fogDensity=.008f;EditorUtility.SetDirty(sky);
            if(root.transform.Find("Garden finishing"))return;
            var group=new GameObject("Garden finishing").transform;group.SetParent(root.transform);
            var terrain=Object.FindAnyObjectByType<Terrain>();var data=terrain.terrainData;
            data.terrainLayers=new[]{AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Modern Villa/Materials/terrain/Terrrain_SandLayer.terrainlayer"),AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Modern Villa/Materials/terrain/Terrain_GrassLayer.terrainlayer")};
            int res=data.alphamapResolution;var a=new float[res,res,2];
            for(int z=0;z<res;z++)for(int x=0;x<res;x++)
            {
                float wx=terrain.transform.position.x+x/(float)(res-1)*data.size.x,wz=terrain.transform.position.z+z/(float)(res-1)*data.size.z;
                float edge=Mathf.Min(39-Mathf.Abs(wx),Mathf.Min(wz+8,49-wz));
                float lawn=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/3));
                // Keep the pool/villa footprint and the seaside strip sandy; lawns fill garden courts.
                float central=Mathf.Max(Mathf.Abs(wx-1)-18,Mathf.Abs(wz-12)-22);
                lawn*=Mathf.SmoothStep(0,1,Mathf.Clamp01(central/2));
                a[z,x,1]=lawn;a[z,x,0]=1-lawn;
            }
            data.SetAlphamaps(0,0,a);EditorUtility.SetDirty(data);
            var random=new System.Random(725);
            void Plant(string prefab,float x,float z,float scale)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modern Villa/Prefabs/Plants/"+prefab+".prefab"));
                go.transform.SetParent(group);go.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);go.transform.localScale=Vector3.one*scale;
                var b=BeachVillaExpansionSurvey.BoundsOf(go);float y=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
                go.transform.position+=new Vector3(x,y,z)-new Vector3(b.center.x,b.min.y,b.center.z);
                foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
                foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.lightmapIndex=-1;}
            }
            // Layered garden islands avoid the 3m circulation spine and building entrances.
            foreach(var p in new[]{new Vector2(-29,42),new Vector2(28,43),new Vector2(-27,-5),new Vector2(29,-4),new Vector2(17,16),new Vector2(-36,21),new Vector2(36,14)})
            {
                Plant("PalmC",p.x,p.y,1);
                for(int i=0;i<13;i++){float angle=i*2.39996f,radius=1.1f+2.0f*(float)random.NextDouble();Plant(i%3==0?"Shrub":i%2==0?"PlantA":"PlantC",p.x+Mathf.Cos(angle)*radius,p.y+Mathf.Sin(angle)*radius,1.2f+(float)random.NextDouble()*.5f);}
            }
            // Background vegetation breaks the landward horizon without expanding gameplay collision.
            for(int i=0;i<24;i++){float x=-56+i*4.8f;Plant(i%2==0?"PalmA":"PalmC",x,58+(float)random.NextDouble()*8,1.2f);}
            for(int side=-1;side<=1;side+=2)for(int i=0;i<14;i++)Plant(i%2==0?"PalmB":"PalmC",side*(48+(float)random.NextDouble()*7),-18+i*5,1.1f);
            // Existing frame boxes filled the openings. Preserve frame and fixed glazing collision, slide the door open.
            foreach(var frame in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.name=="Window door frame"))
            {
                foreach(var box in frame.GetComponents<BoxCollider>())box.enabled=false;
                var mc=frame.GetComponent<MeshCollider>();if(!mc)mc=frame.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=frame.sharedMesh;
                foreach(var t in frame.GetComponentsInChildren<Transform>())
                {
                    if(t.name=="Window door door" || t.name=="Window door knob")t.localPosition+=Vector3.left*1.65f;
                    if(t.name.Contains("window") && t.GetComponent<MeshFilter>()) {var glass=t.GetComponent<MeshCollider>();if(!glass)glass=t.gameObject.AddComponent<MeshCollider>();glass.sharedMesh=t.GetComponent<MeshFilter>().sharedMesh;}
                }
            }
            // Open two vendor wall modules on the pool-facing side of the guest wing.
            var guest=GameObject.Find("Guest wing - vendor BuildingC");
            foreach(var r in guest.GetComponentsInChildren<MeshRenderer>())
            {
                var b=r.bounds;
                if(b.size.y>1.8f && b.size.x<.5f && b.center.x<26.4f && Mathf.Abs(b.center.z-18)<2.2f)r.gameObject.SetActive(false);
            }
            // Move the tea grouping into the accessible front room; do not leave rewards behind a partition.
            var props=root.transform.Find("Furniture and lived-in details");
            foreach(Transform t in props)
            {var b=BeachVillaExpansionSurvey.BoundsOf(t.gameObject);if(Mathf.Abs(b.center.x-30)<2 && b.center.z>21 && b.center.z<24 && !t.GetComponentInChildren<Light>())t.position+=new Vector3(-1.8f,0,-4);}
            var hide=root.GetComponent<BeachVillaHideLocations>();var spot=hide.spots[14];spot.position=new Vector3(28.4f,.95f,18.3f);spot.approach=new Vector3(27,.1f,18);hide.spots[14]=spot;
            EditorUtility.SetDirty(hide);
        }
    }
}
