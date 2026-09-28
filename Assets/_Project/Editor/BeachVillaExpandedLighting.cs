using System;
using System.IO;
using System.Linq;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    public static class BeachVillaExpandedLighting
    {
        const string Folder="Assets/_Project/Lighting/BeachVillaExpanded";
        const string Report="Docs/BeachVillaExpansion/lighting.txt";
        const string ScenePath="Assets/Modern Villa/Scenes/Beach Villa.unity";
        static int phase;
        static double start;
        static BeachVillaDarkNight controller;
        static BeachVillaDarkNight.BakedRenderer[] dayRenderers;
        static BeachVillaDarkNight.BakedTerrain[] dayTerrains;
        static LightmapData[] dayMaps;
        static Material daySky,nightSky;
        static Color[] colors;
        static bool[] enabled;
        static Color ambient,fog;
        static AmbientMode ambientMode;
        static float intensity,reflection;
        static LightingDataAsset dayData;
        static ProbeVolumeBakingSet Set=>AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>("Assets/_Project/Lighting/BeachVilla/BeachVillaBakingSet.asset");
        [MenuItem("Rubber/Expansion/5 Rebuild Day and Night Lighting")]
        public static void Start()
        {
            if(Application.isPlaying || Lightmapping.isRunning || AdaptiveProbeVolumes.isRunning || SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Beach Villa outside Play, with no active bake.");
            if(new[]{"Day","Night"}.Any(label=>Directory.Exists(Folder+"/"+label) && Directory.EnumerateFiles(Folder+"/"+label,"Lightmap-*").Any()))
                throw new IOException("Preserve the existing Day/Night exports before rebuilding. No scene or bake setting was changed.");
            controller=UnityEngine.Object.FindAnyObjectByType<BeachVillaDarkNight>();
            controller.nightProbesReady=false;controller.nightMaps=Array.Empty<BeachVillaDarkNight.BakedMap>();
            var settings=Lightmapping.lightingSettings;
            var so=new SerializedObject(settings);
            so.FindProperty("m_BakeResolution").floatValue=16;
            so.FindProperty("m_LightmapMaxSize").intValue=1024;
            so.FindProperty("m_PVRDirectSampleCount").intValue=32;
            so.FindProperty("m_PVRSampleCount").intValue=256;
            so.FindProperty("m_PVREnvironmentSampleCount").intValue=128;
            so.FindProperty("m_LightProbeSampleCountMultiplier").floatValue=2;
            so.ApplyModifiedProperties();
            int probes=0;
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var b=r.bounds;string n=r.name.ToLowerInvariant();
                bool detail=b.size.x*b.size.y*b.size.z<.03f || n.Contains("palm") || n.Contains("plant") || n.Contains("shrub") || n.Contains("flower") || n.Contains("loungechair");
                if(detail){GameObjectUtility.SetStaticEditorFlags(r.gameObject,GameObjectUtility.GetStaticEditorFlags(r.gameObject)&~StaticEditorFlags.ContributeGI);r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.lightmapIndex=-1;probes++;}
                else if((GameObjectUtility.GetStaticEditorFlags(r.gameObject)&StaticEditorFlags.ContributeGI)!=0){r.receiveGI=ReceiveGI.Lightmaps;r.scaleInLightmap=Mathf.Min(r.scaleInLightmap,.75f);}
            }
            foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {var ts=new SerializedObject(t);var scale=ts.FindProperty("m_ScaleInLightmap");if(scale!=null)scale.floatValue=.025f;ts.ApplyModifiedProperties();}
            foreach(var light in controller.sceneLights)if(light && light.type!=LightType.Directional)light.lightmapBakeType=LightmapBakeType.Baked;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Folder+"/Day");Directory.CreateDirectory(Folder+"/Night");AssetDatabase.Refresh();
            string backup="Library/BeachVillaBeforeExpandedBake-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);File.Copy(ScenePath,backup+"/Beach Villa.unity");
            File.WriteAllText(Report,$"Start UTC={DateTime.UtcNow:O}\nBackup={backup}\n16 texels/m, 1024 atlases, 32 direct/256 indirect/128 environment samples, APV L1; small details/foliage via APV={probes}; fixed practical lights baked; flashlight stays realtime.\n");
            daySky=RenderSettings.skybox;ambientMode=RenderSettings.ambientMode;ambient=RenderSettings.ambientLight;intensity=RenderSettings.ambientIntensity;reflection=RenderSettings.reflectionIntensity;fog=RenderSettings.fogColor;
            colors=controller.sceneLights.Select(l=>l?l.color:Color.white).ToArray();enabled=controller.sceneLights.Select(l=>l&&l.enabled).ToArray();
            Scenario("Default",false);phase=1;start=EditorApplication.timeSinceStartup;
            Lightmapping.bakeCompleted+=Completed;
            if(!Lightmapping.BakeAsync()){Lightmapping.bakeCompleted-=Completed;phase=0;throw new InvalidOperationException("Day bake failed to start.");}
        }
        static void Scenario(string name,bool freeze)
        {
            var set=Set;if(!set.lightingScenarios.Contains(name))set.TryAddScenario(name);
            var so=new SerializedObject(set);so.FindProperty("lightingScenario").stringValue=name;so.FindProperty("freezePlacement").boolValue=freeze;so.ApplyModifiedProperties();AssetDatabase.SaveAssetIfDirty(set);
        }
        static void Completed(){EditorApplication.delayCall+=FinishPhase;}
        static void FinishPhase()
        {
            if(Lightmapping.isRunning || AdaptiveProbeVolumes.isRunning){EditorApplication.delayCall+=FinishPhase;return;}
            try
            {
                File.AppendAllText(Report,$"Phase={phase}; duration={EditorApplication.timeSinceStartup-start:F1}s; atlases={LightmapSettings.lightmaps.Length}; APV={Set.HasBakedData()}\n");
                if(LightmapSettings.lightmaps.Length==0)throw new InvalidOperationException("Bake produced no static lightmaps.");
                if(phase==1)
                {
                    dayMaps=CopyMaps("Day");dayRenderers=Bindings();dayTerrains=TerrainBindings();
                    string dataSource=AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset),dataTarget=AssetDatabase.GenerateUniqueAssetPath(Folder+"/Day/LightingData.asset");
                    if(!AssetDatabase.CopyAsset(dataSource,dataTarget))throw new IOException("Cannot preserve day LightingData.");
                    dayData=AssetDatabase.LoadAssetAtPath<LightingDataAsset>(dataTarget);
                    var ds=new SerializedObject(dayData);var it=ds.GetIterator();
                    while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference && it.objectReferenceValue is Texture2D texture)
                    {
                        string src=AssetDatabase.GetAssetPath(texture);var replacement=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Day/"+Path.GetFileName(src));if(replacement)it.objectReferenceValue=replacement;
                    }
                    ds.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(dayData);
                    Scenario("Night",true);
                    for(int i=0;i<controller.sceneLights.Length;i++)if(controller.sceneLights[i]){var l=controller.sceneLights[i];if(l.type==LightType.Directional)l.enabled=false;else l.color=new Color(1,.67f,.37f);}
                    nightSky=new Material(daySky){hideFlags=HideFlags.DontSave};nightSky.SetFloat("_Exposure",.001f);if(nightSky.HasProperty("_SkyTint"))nightSky.SetColor("_SkyTint",new Color(.15f,.2f,.3f));RenderSettings.skybox=nightSky;
                    RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.0001f,.00015f,.00025f);RenderSettings.ambientIntensity=0;RenderSettings.reflectionIntensity=0;RenderSettings.fogColor=Color.black;
                    phase=2;start=EditorApplication.timeSinceStartup;
                    if(!Lightmapping.BakeAsync())throw new InvalidOperationException("Night bake failed to start.");
                }
                else if(phase==2)
                {
                    var maps=CopyMaps("Night");controller.nightMaps=maps.Select(m=>new BeachVillaDarkNight.BakedMap{color=m.lightmapColor,direction=m.lightmapDir,mask=m.shadowMask}).ToArray();
                    controller.nightRenderers=Bindings();controller.nightTerrains=TerrainBindings();controller.nightProbesReady=Set.HasBakedData();
                    RestoreDay();phase=0;Lightmapping.bakeCompleted-=Completed;
                    BeachVillaLightingAudit.Run();
                    File.AppendAllText(Report,$"COMPLETE UTC={DateTime.UtcNow:O}; day={dayMaps.Length}; night={maps.Length}; defaultSceneRestored=true\n");
                }
            }
            catch(Exception e){File.AppendAllText(Report,"FAILED "+e+"\n");Lightmapping.bakeCompleted-=Completed;phase=0;RestoreDay();Debug.LogException(e);}
        }
        static LightmapData[] CopyMaps(string label)
        {
            Texture2D Copy(Texture2D texture)
            {
                if(!texture)return null;string source=AssetDatabase.GetAssetPath(texture),target=Folder+"/"+label+"/"+Path.GetFileName(source);
                if(File.Exists(target))throw new IOException("Existing bake export: "+target+". Preserve/review before rebaking.");
                if(!AssetDatabase.CopyAsset(source,target))throw new IOException("Copy failed: "+source);return AssetDatabase.LoadAssetAtPath<Texture2D>(target);
            }
            return LightmapSettings.lightmaps.Select(m=>new LightmapData{lightmapColor=Copy(m.lightmapColor),lightmapDir=Copy(m.lightmapDir),shadowMask=Copy(m.shadowMask)}).ToArray();
        }
        static BeachVillaDarkNight.BakedRenderer[] Bindings()=>UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(r=>new BeachVillaDarkNight.BakedRenderer{renderer=r,index=r.lightmapIndex,scaleOffset=r.lightmapScaleOffset}).ToArray();
        static BeachVillaDarkNight.BakedTerrain[] TerrainBindings()=>UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(t=>new BeachVillaDarkNight.BakedTerrain{terrain=t,index=t.lightmapIndex,scaleOffset=t.lightmapScaleOffset}).ToArray();
        static void RestoreDay()
        {
            Scenario("Default",true);RenderSettings.skybox=daySky;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambient;RenderSettings.ambientIntensity=intensity;RenderSettings.reflectionIntensity=reflection;RenderSettings.fogColor=fog;
            for(int i=0;i<controller.sceneLights.Length;i++)if(controller.sceneLights[i]){controller.sceneLights[i].color=colors[i];controller.sceneLights[i].enabled=enabled[i];}
            if(dayData)Lightmapping.lightingDataAsset=dayData;
            if(dayMaps!=null){LightmapSettings.lightmaps=dayMaps;foreach(var b in dayRenderers)if(b.renderer){b.renderer.lightmapIndex=b.index;b.renderer.lightmapScaleOffset=b.scaleOffset;}foreach(var b in dayTerrains)if(b.terrain){b.terrain.lightmapIndex=b.index;b.terrain.lightmapScaleOffset=b.scaleOffset;}}
            if(nightSky)UnityEngine.Object.DestroyImmediate(nightSky);
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
    }
}
