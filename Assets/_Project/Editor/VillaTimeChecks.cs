using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rubber.EditorTools
{
    [InitializeOnLoad]
    public static class VillaTimeChecks
    {
        static VillaTimeChecks()
        {
            EditorApplication.playModeStateChanged+=Changed;
            EditorApplication.delayCall+=()=>{const string request="Library/VillaTimeChecks.request";if(File.Exists(request)){File.Delete(request);Run();}};
        }
        [MenuItem("Rubber/Time Of Day/Run Playback Checks")]
        public static void Run(){if(Application.isPlaying)return;SessionState.SetBool("VillaTimeChecks",true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange s)
        {
            if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("VillaTimeChecks",false))return;
            SessionState.SetBool("VillaTimeChecks",false);
            var c=UnityEngine.Object.FindAnyObjectByType<VillaTimeOfDay>();c.StartCoroutine(Check(c));
        }
        static IEnumerator Check(VillaTimeOfDay c)
        {
            var report=new StringBuilder("Runtime checks "+DateTime.UtcNow.ToString("O")+"\n");
            var walk=c.GetComponent<ModernVillaWalkthrough>();bool walking=walk.enabled;walk.enabled=false;c.running=false;
            var errors=new List<string>();Application.LogCallback callback=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception)errors.Add(message);};Application.logMessageReceived+=callback;
            try
            {
                yield return new WaitForSecondsRealtime(2);
                report.AppendLine("GPU="+SystemInfo.graphicsDeviceName+" Resolution="+Screen.width+"x"+Screen.height+" APV blending="+new SerializedObject(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline).FindProperty("m_SupportProbeVolumeScenarioBlending").boolValue);
                report.AppendLine("Blend buffers reported bytes="+c.BlendingBufferBytes);
                walk.GoTo(0);walk.view.localRotation=Quaternion.Euler(-5,0,0);
                foreach(float hour in new[]{7f,12,16.5f,18,19.5f,0})
                {
                    c.SetHour(hour);yield return new WaitForSecondsRealtime(2);
                    report.AppendLine($"hour={hour} blend={c.CurrentBlend} APV={ProbeReferenceVolume.instance.lightingScenario} factor={ProbeReferenceVolume.instance.scenarioBlendingFactor:F3} maps={LightmapSettings.lightmaps.Length} sun={c.sun.intensity:F4}");
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/VillaTimeOfDay/"+hour.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                    yield return null;
                }
                // Verify actual GPU lightmap interpolation against source textures at a fixed midpoint.
                c.SetHour(16.5f);yield return new WaitForSecondsRealtime(.5f);
                var a=Read(c,c.noon.maps[0].color);var b=Read(c,c.sunset.maps[0].color);var blended=Read(c,LightmapSettings.lightmaps[0].lightmapColor);
                float max=0;for(int i=0;i<a.Length;i++){var expected=Color.Lerp(a[i],b[i],.5f);max=Mathf.Max(max,Mathf.Abs(blended[i].r-expected.r),Mathf.Abs(blended[i].g-expected.g),Mathf.Abs(blended[i].b-expected.b));}
                report.AppendLine("GPU midpoint max RGB error="+max+" (half precision and sampling)");
                foreach(bool run in new[]{false,true})
                {
                    c.SetHour(16.5f);c.running=run;yield return new WaitForSecondsRealtime(1);
                    var times=new List<double>();int updates=c.LightmapUpdates;double previous=Time.realtimeSinceStartupAsDouble;
                    for(int i=0;i<180;i++){yield return null;double now=Time.realtimeSinceStartupAsDouble;times.Add((now-previous)*1000);previous=now;}
                    times.Sort();report.AppendLine($"Editor running={run}: median={times[90]:F2}ms p95={times[171]:F2}ms lightmap updates={c.LightmapUpdates-updates}");
                }
                c.running=false;c.SetHour(23.9999f);c.running=true;yield return new WaitForSecondsRealtime(.2f);report.AppendLine("Midnight wrap hour="+c.hour);c.running=false;
                report.AppendLine("Runtime errors="+errors.Count);foreach(var e in errors.Distinct())report.AppendLine(e);
                report.AppendLine("No standalone FPS guarantee. Legacy door, jump, flashlight components retained.");
            }
            finally
            {
                Application.logMessageReceived-=callback;File.WriteAllText("Docs/VillaTimeOfDay/playchecks.txt",report.ToString());c.SetHour(7);c.running=true;walk.GoTo(0);walk.enabled=walking;EditorApplication.isPlaying=false;
            }
        }
        static Color[] Read(VillaTimeOfDay cycle,Texture texture)
        {
            var rt=new RenderTexture(16,16,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear){enableRandomWrite=true};rt.Create();var previous=RenderTexture.active;
            int kernel=cycle.blender.FindKernel("SampleBase");cycle.blender.SetTexture(kernel,"SourceA",texture);cycle.blender.SetTexture(kernel,"Result",rt);cycle.blender.Dispatch(kernel,2,2,1);RenderTexture.active=rt;
            var t=new Texture2D(16,16,TextureFormat.RGBAHalf,false,true);t.ReadPixels(new Rect(0,0,16,16),0,0);t.Apply();var result=t.GetPixels();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);return result;
        }
    }
}
