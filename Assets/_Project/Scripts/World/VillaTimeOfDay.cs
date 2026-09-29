using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Rubber.World
{
    [DisallowMultipleComponent]
    public sealed class VillaTimeOfDay : MonoBehaviour
    {
        [Serializable] public class LightingState
        {
            public string scenario;
            public ModernVillaDayNight.BakedMap[] maps;
            public Material sky;
            public Vector3 sunAngles;
            public Color sunColor = Color.white;
            public float sunIntensity, reflection;
        }
        public LightingState morning, noon, sunset, night;
        public Light sun;
        public ComputeShader blender;
        [Range(0,24)] public float hour = 7;
        [Min(60)] public float dayLengthSeconds = 600;
        public bool running = true;
        [Range(1,30)] public float lightmapUpdatesPerSecond = 10;
        public string CurrentBlend { get; private set; }
        public int LightmapUpdates { get; private set; }
        public long BlendingBufferBytes
        {
            get
            {
                long bytes=0;if(colors==null)return bytes;
                foreach(var t in colors)if(t)bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
                foreach(var t in directions)if(t)bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
                foreach(var t in colorTargets)if(t)bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
                foreach(var t in directionTargets)if(t)bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
                return bytes;
            }
        }
        Material runtimeSky, savedSky;
        LightmapData[] savedMaps, outputMaps;
        Texture2D[] colors, directions;
        RenderTexture[] colorTargets, directionTargets;
        ReflectionProbe[] probes;
        float[] probeIntensities;
        float savedReflection, savedIntensity, nextUpdate, lastWeight = -1;
        Color savedSunColor;
        Quaternion savedSunRotation;
        bool savedSunEnabled, ready;
        LightingState lastA, lastB;
        int kernel, savedCells;
        float skipStart, skipDistance, skipElapsed = -1;

        void OnEnable()
        {
            if (!Application.isPlaying || !blender || !sun || noon?.maps == null || noon.maps.Length == 0) return;
            savedMaps = LightmapSettings.lightmaps; savedSky = RenderSettings.skybox;
            foreach(var state in new[]{morning,noon,sunset,night})foreach(var map in state.maps)
            {if(map.color.streamingMipmaps)map.color.requestedMipmapLevel=0;if(map.direction && map.direction.streamingMipmaps)map.direction.requestedMipmapLevel=0;}
            savedReflection = RenderSettings.reflectionIntensity; savedIntensity = sun.intensity;
            savedSunColor = sun.color; savedSunRotation = sun.transform.rotation; savedSunEnabled = sun.enabled;
            runtimeSky = new Material(noon.sky) { name = "Villa time sky (runtime)" };
            RenderSettings.skybox = runtimeSky;
            probes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            probeIntensities = new float[probes.Length];
            for (int i=0;i<probes.Length;i++) probeIntensities[i] = probes[i].intensity;
            int n = noon.maps.Length; colors = new Texture2D[n]; directions = new Texture2D[n];
            colorTargets = new RenderTexture[n]; directionTargets = new RenderTexture[n]; outputMaps = new LightmapData[n];
            for (int i=0;i<n;i++)
            {
                Create(noon.maps[i].color, out colors[i], out colorTargets[i]);
                if (noon.maps[i].direction) Create(noon.maps[i].direction, out directions[i], out directionTargets[i]);
                outputMaps[i] = new LightmapData { lightmapColor=colors[i], lightmapDir=directions[i] };
            }
            kernel = blender.FindKernel("Blend");
            savedCells = ProbeReferenceVolume.instance.numberOfCellsBlendedPerFrame;
            ProbeReferenceVolume.instance.numberOfCellsBlendedPerFrame = 4;
            ready = true; Apply(true); LightmapSettings.lightmaps = outputMaps;
        }
        static void Create(Texture2D source, out Texture2D texture, out RenderTexture target)
        {
            texture = new Texture2D(source.width,source.height,TextureFormat.RGBAHalf,true,true) { name="Villa blended lightmap", wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Trilinear };
            texture.Apply(false,true);
            target = new RenderTexture(source.width,source.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear) { enableRandomWrite=true,useMipMap=true,autoGenerateMips=false };
            target.Create();
        }
        public void SetHour(float value) { hour=Mathf.Repeat(value,24); skipElapsed=-1; Apply(true); }
        public void AdvancePeriod()
        {
            skipStart=hour;
            float target=hour<6.99f?7:hour<11.99f?12:hour<17.99f?18:hour<23.99f?24:31;
            skipDistance=target-hour; skipElapsed=0;
        }
        public void Evaluate(out LightingState a,out LightingState b,out float weight)
        {
            float h=Mathf.Repeat(hour,24);
            if(h<5 || h>=21){a=b=night;weight=0;}
            else if(h<7){a=night;b=morning;weight=(h-5)/2;}
            else if(h<12){a=morning;b=noon;weight=(h-7)/5;}
            else if(h<15){a=b=noon;weight=0;}
            else if(h<18){a=noon;b=sunset;weight=(h-15)/3;}
            else {a=sunset;b=night;weight=(h-18)/3;}
            weight=Mathf.SmoothStep(0,1,weight);
        }
        void Update()
        {
            if(!ready)return;
            var k=Keyboard.current;
            if(Application.isFocused && k!=null)
            {
                if(k.tKey.wasPressedThisFrame)running=!running;
                if(k.nKey.wasPressedThisFrame)AdvancePeriod();
                if(k.leftBracketKey.wasPressedThisFrame)SetHour(hour-1);
                if(k.rightBracketKey.wasPressedThisFrame)SetHour(hour+1);
            }
            if(skipElapsed>=0){skipElapsed+=Time.deltaTime;hour=Mathf.Repeat(skipStart+skipDistance*Mathf.SmoothStep(0,1,skipElapsed/5),24);if(skipElapsed>=5)skipElapsed=-1;}
            else if(running)hour=Mathf.Repeat(hour+Time.deltaTime*24/Mathf.Max(60,dayLengthSeconds),24);
            Apply(false);
        }
        void Apply(bool force)
        {
            if(!ready)return;
            Evaluate(out var a,out var b,out float w);
            if(sun)
            {
                sun.transform.rotation=Quaternion.Slerp(Quaternion.Euler(a.sunAngles),Quaternion.Euler(b.sunAngles),w);
                sun.color=Color.Lerp(a.sunColor,b.sunColor,w);sun.intensity=Mathf.Lerp(a.sunIntensity,b.sunIntensity,w);sun.enabled=sun.intensity>.0001f;
            }
            runtimeSky.Lerp(a.sky,b.sky,w);
            float reflection=Mathf.Lerp(a.reflection,b.reflection,w);
            RenderSettings.reflectionIntensity=reflection;
            for(int i=0;i<probes.Length;i++)if(probes[i])probes[i].intensity=probeIntensities[i]*reflection/Mathf.Max(.001f,noon.reflection);
            var apv=ProbeReferenceVolume.instance;
            if(apv.currentBakingSet)
            {
                if(apv.lightingScenario!=a.scenario)apv.lightingScenario=a.scenario;
                apv.BlendLightingScenario(a==b?null:b.scenario,w);
            }
            if(!force && (Time.unscaledTime<nextUpdate || (a==lastA && b==lastB && Mathf.Abs(lastWeight-w)<.0001f)))return;
            nextUpdate=Time.unscaledTime+1/Mathf.Max(1,lightmapUpdatesPerSecond);
            for(int i=0;i<colors.Length;i++)
            {
                Blend(a.maps[i].color,b.maps[i].color,colorTargets[i],colors[i],w);
                if(directions[i])Blend(a.maps[i].direction,b.maps[i].direction,directionTargets[i],directions[i],w);
            }
            lastA=a;lastB=b;lastWeight=w;LightmapUpdates++;
            CurrentBlend=$"{a.scenario} → {b.scenario} ({w:P0})";
        }
        void Blend(Texture2D a,Texture2D b,RenderTexture target,Texture2D output,float w)
        {
            blender.SetTexture(kernel,"SourceA",a);blender.SetTexture(kernel,"SourceB",b);blender.SetTexture(kernel,"Result",target);
            blender.SetFloat("Weight",w);blender.Dispatch(kernel,(target.width+7)/8,(target.height+7)/8,1);
            target.GenerateMips();Graphics.CopyTexture(target,output);
        }
        void OnGUI()
        {
            if(!ready)return;
            int h=Mathf.FloorToInt(hour),m=Mathf.FloorToInt((hour-h)*60);
            GUI.Box(new Rect(16,150,600,45),$"{h:00}:{m:00} | {(running?"Time running":"Paused")} | T pause | N next period (5s) | [ / ] hour\n{CurrentBlend}");
        }
        void OnDisable()
        {
            if(!ready)return;
            ready=false;
            LightmapSettings.lightmaps=savedMaps;RenderSettings.skybox=savedSky;RenderSettings.reflectionIntensity=savedReflection;
            // Scene teardown can destroy the Light before this component is disabled.
            // Use Unity's lifetime check and still release the runtime lighting resources.
            if(sun){sun.intensity=savedIntensity;sun.color=savedSunColor;sun.transform.rotation=savedSunRotation;sun.enabled=savedSunEnabled;}
            for(int i=0;i<probes.Length;i++)if(probes[i])probes[i].intensity=probeIntensities[i];
            var apv=ProbeReferenceVolume.instance;apv.BlendLightingScenario(null,0);apv.lightingScenario="Default";apv.numberOfCellsBlendedPerFrame=savedCells;
            foreach(var t in colors)if(t)Destroy(t);foreach(var t in directions)if(t)Destroy(t);
            foreach(var t in colorTargets)if(t){t.Release();Destroy(t);}foreach(var t in directionTargets)if(t){t.Release();Destroy(t);}
            Destroy(runtimeSky);ready=false;lastA=lastB=null;lastWeight=-1;
            foreach(var state in new[]{morning,noon,sunset,night})foreach(var map in state.maps)
            {map.color.ClearRequestedMipmapLevel();if(map.direction)map.direction.ClearRequestedMipmapLevel();}
        }
    }
}
