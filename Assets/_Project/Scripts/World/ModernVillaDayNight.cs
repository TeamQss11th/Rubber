using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Rubber.World
{
    [DisallowMultipleComponent]
    public sealed class ModernVillaDayNight : MonoBehaviour
    {
        public bool startAtNight=false;
        public Light[] sceneLights;
        public bool nightProbesReady;
        [Serializable] public struct BakedMap {public Texture2D color,direction,mask;}
        [Serializable] public struct BakedRenderer {public Renderer renderer;public int index;public Vector4 scaleOffset;}
        [Serializable] public struct BakedTerrain {public Terrain terrain;public int index;public Vector4 scaleOffset;}
        public BakedMap[] nightMaps=Array.Empty<BakedMap>();
        public BakedRenderer[] nightRenderers=Array.Empty<BakedRenderer>();
        public BakedTerrain[] nightTerrains=Array.Empty<BakedTerrain>();
        public bool IsNight {get;private set;}
        struct LightState {public bool enabled;public float intensity;public Color color;}
        LightState[] lightStates;
        ReflectionProbe[] reflections; float[] reflectionIntensities;
        Renderer[] renderers;
        int[] rendererIndices;
        Vector4[] rendererScaleOffsets,terrainScaleOffsets;
        LightProbeUsage[] rendererProbeUsage;
        Terrain[] terrains;
        int[] terrainIndices;
        LightmapData[] dayMaps;
        Material daySky,nightSky;
        AmbientMode dayAmbientMode;
        Color dayAmbientLight,daySkyColor,dayEquator,dayGround;
        Color dayFogColor;
        SphericalHarmonicsL2 dayProbe;
        float dayAmbientIntensity,dayReflection;
        Volume volume;
        VolumeProfile profile;
        bool initialized,capturing;

        void OnEnable()
        {
            if(!Application.isPlaying)return;
            // Snapshot only once per activation. Shared materials and baked assets are never edited.
            renderers=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();
            terrains=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
            rendererIndices=renderers.Select(r=>r.lightmapIndex).ToArray();
            rendererScaleOffsets=renderers.Select(r=>r.lightmapScaleOffset).ToArray();
            terrainScaleOffsets=terrains.Select(t=>t.lightmapScaleOffset).ToArray();
            rendererProbeUsage=renderers.Select(r=>r.lightProbeUsage).ToArray();
            terrainIndices=terrains.Select(t=>t.lightmapIndex).ToArray();
            dayMaps=LightmapSettings.lightmaps;
            reflections=gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ReflectionProbe>(true)).ToArray(); reflectionIntensities=reflections.Select(p=>p.intensity).ToArray();
            if(sceneLights==null)sceneLights=Array.Empty<Light>();
            lightStates=sceneLights.Select(l=>l?new LightState{enabled=l.enabled,intensity=l.intensity,color=l.color}:default).ToArray();
            daySky=RenderSettings.skybox;dayAmbientMode=RenderSettings.ambientMode;dayAmbientLight=RenderSettings.ambientLight;
            daySkyColor=RenderSettings.ambientSkyColor;dayEquator=RenderSettings.ambientEquatorColor;dayGround=RenderSettings.ambientGroundColor;
            dayProbe=RenderSettings.ambientProbe;dayAmbientIntensity=RenderSettings.ambientIntensity;dayReflection=RenderSettings.reflectionIntensity;
            dayFogColor=RenderSettings.fogColor;
            if(daySky)
            {
                nightSky=new Material(daySky){name="Modern Villa dark night (runtime)",hideFlags=HideFlags.DontSave};
                if(nightSky.HasProperty("_Exposure"))nightSky.SetFloat("_Exposure",.001f);
                if(nightSky.HasProperty("_SkyTint"))nightSky.SetColor("_SkyTint",new Color(.15f,.2f,.3f));
                if(nightSky.HasProperty("_Tint"))nightSky.SetColor("_Tint",new Color(.15f,.2f,.3f));
            }
            var volumeObject=new GameObject("Dark night GI override (runtime)"){hideFlags=HideFlags.DontSave};
            volumeObject.transform.SetParent(transform,false);
            volume=volumeObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10000;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.hideFlags=HideFlags.DontSave;
            profile.Add<ProbeVolumesOptions>(true).intensityMultiplier.Override(0);
            volume.sharedProfile=profile;volume.enabled=false;
            initialized=true;
            if(startAtNight)SetNight(true);
        }
        public void SetNight(bool night)
        {
            var time = GetComponent<VillaTimeOfDay>();
            if(time && time.isActiveAndEnabled) { time.SetHour(night?0:12); return; }
            if(!initialized || IsNight==night)return;
            if(night)
            {
                foreach(var r in renderers)if(r){r.lightmapIndex=-1;r.lightProbeUsage=LightProbeUsage.BlendProbes;}
                foreach(var t in terrains)if(t)t.lightmapIndex=-1;
                LightmapSettings.lightmaps=Array.Empty<LightmapData>();
                if(nightProbesReady && nightMaps.Length>0)
                {
                    LightmapSettings.lightmaps=nightMaps.Select(m=>new LightmapData{lightmapColor=m.color,lightmapDir=m.direction,shadowMask=m.mask}).ToArray();
                    foreach(var b in nightRenderers)if(b.renderer){b.renderer.lightmapIndex=b.index;if(!b.renderer.isPartOfStaticBatch)b.renderer.lightmapScaleOffset=b.scaleOffset;}
                    foreach(var b in nightTerrains)if(b.terrain){b.terrain.lightmapIndex=b.index;b.terrain.lightmapScaleOffset=b.scaleOffset;}
                }
                RenderSettings.skybox=nightSky;
                RenderSettings.ambientMode=AmbientMode.Flat;
                RenderSettings.ambientLight=new Color(.0001f,.00015f,.00025f);
                RenderSettings.ambientIntensity=0;
                var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.0001f,.00015f,.00025f));RenderSettings.ambientProbe=sh;
                RenderSettings.reflectionIntensity=0; foreach(var p in reflections)if(p)p.intensity=0;
                RenderSettings.fogColor=Color.black;
                for(int i=0;i<sceneLights.Length;i++)
                {
                    var light=sceneLights[i];if(!light)continue;
                    if(light.type==LightType.Directional){light.enabled=false;continue;}
                    light.enabled=lightStates[i].enabled;
                    light.color=new Color(1,.67f,.37f);
                    light.intensity=lightStates[i].intensity;
                }
                if(nightProbesReady)ProbeReferenceVolume.instance.lightingScenario="Night";
                profile.TryGet<ProbeVolumesOptions>(out var options);
                options.intensityMultiplier.Override(nightProbesReady?1:0);
                if(volume)volume.enabled=true;
            }
            else
            {
                if(volume)volume.enabled=false;
                if(nightProbesReady)ProbeReferenceVolume.instance.lightingScenario="Default";
                LightmapSettings.lightmaps=dayMaps;
                for(int i=0;i<renderers.Length;i++)if(renderers[i]){renderers[i].lightmapIndex=rendererIndices[i];if(!renderers[i].isPartOfStaticBatch)renderers[i].lightmapScaleOffset=rendererScaleOffsets[i];renderers[i].lightProbeUsage=rendererProbeUsage[i];}
                for(int i=0;i<terrains.Length;i++)if(terrains[i]){terrains[i].lightmapIndex=terrainIndices[i];terrains[i].lightmapScaleOffset=terrainScaleOffsets[i];}
                for(int i=0;i<sceneLights.Length;i++)if(sceneLights[i])
                {sceneLights[i].enabled=lightStates[i].enabled;sceneLights[i].intensity=lightStates[i].intensity;sceneLights[i].color=lightStates[i].color;}
                RenderSettings.skybox=daySky;RenderSettings.ambientMode=dayAmbientMode;RenderSettings.ambientLight=dayAmbientLight;
                RenderSettings.ambientSkyColor=daySkyColor;RenderSettings.ambientEquatorColor=dayEquator;RenderSettings.ambientGroundColor=dayGround;
                RenderSettings.ambientIntensity=dayAmbientIntensity;RenderSettings.ambientProbe=dayProbe;RenderSettings.reflectionIntensity=dayReflection;
                RenderSettings.fogColor=dayFogColor; for(int i=0;i<reflections.Length;i++)if(reflections[i])reflections[i].intensity=reflectionIntensities[i];
            }
            IsNight=night;
        }
        void Update()
        {
            // APV can initialize after MonoBehaviour.OnEnable when entering Play mode.
            if(initialized && nightProbesReady && ProbeReferenceVolume.instance.currentBakingSet)
            {
                string desired=IsNight?"Night":"Default";
                if(ProbeReferenceVolume.instance.lightingScenario!=desired)
                    ProbeReferenceVolume.instance.lightingScenario=desired;
            }
            var keys=Keyboard.current;
            if(Application.isFocused && keys!=null && keys.nKey.wasPressedThisFrame)SetNight(!IsNight);
        }
        void OnDisable()
        {
            if(!initialized)return;
            StopAllCoroutines();capturing=false;
            SetNight(false);
            if(volume)Destroy(volume.gameObject);
            if(profile)Destroy(profile);
            if(nightSky)Destroy(nightSky);
            initialized=false;
        }
        public void CaptureCurrentView()
        {
            if(!Application.isPlaying || capturing)return;
            StartCoroutine(Capture());
        }
        IEnumerator Capture()
        {
            capturing=true;
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            string folder=Application.isEditor?Path.GetFullPath("Docs/ModernVillaDayNight"):Path.Combine(Application.persistentDataPath,"NightReview");
            Directory.CreateDirectory(folder);
            string name=IsNight?"night-gameview":"day-gameview";
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
            File.WriteAllText(Path.Combine(folder,name+"-state.txt"),$"Night={IsNight}; resolution={Screen.width}x{Screen.height}; lightmaps={LightmapSettings.lightmaps.Length}; directional enabled={sceneLights.Count(l=>l && l.isActiveAndEnabled && l.type==LightType.Directional)}; local lights enabled={sceneLights.Count(l=>l && l.isActiveAndEnabled && l.type!=LightType.Directional)}; reflection={RenderSettings.reflectionIntensity}\nNormal Game View capture; no Camera.Render. Not an FPS benchmark.\n");
            capturing=false;
        }
    }
}



