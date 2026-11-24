using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Rubber.World
{
    public sealed class BeachVillaBenchmark : MonoBehaviour
    {
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartRequested()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-beachVillaBenchmark");
            if(i>=0 && i+1<args.Length)new GameObject("Beach Villa benchmark (explicit command line)").AddComponent<BeachVillaBenchmark>().output=args[i+1];
        }
        IEnumerator Start()
        {
            var walk=FindAnyObjectByType<BeachVillaWalkthrough>();if(!walk){Application.Quit(2);yield break;}
            var night=walk.GetComponent<BeachVillaDarkNight>();var flash=walk.GetComponent<BeachVillaTestFlashlight>();
            walk.enabled=false;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Application.runInBackground=true;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            var lines=new List<string>{"Standalone player; wall frame time, not isolated GPU time.",$"GPU={SystemInfo.graphicsDeviceName}; CPU={SystemInfo.processorType}; RAM={SystemInfo.systemMemorySize}MB", "Each viewpoint: 3s warmup, 4s samples. Uncapped, VSync off. Rendering sample; no AI/duck physics workload.","mode,checkpoint,width,height,samples,mean_ms,p95_ms,p99_ms,lightmaps,apv"};
            foreach(int mode in new[]{0,1,2})
            {
                night.SetNight(mode!=0);flash.SetOn(mode==2);
                foreach(int index in new[]{0,2,6,7,8,9,10})
                {
                    walk.GoTo(index);walk.GetComponent<CharacterController>().enabled=false;
                    yield return new WaitForSecondsRealtime(3);
                    var samples=new List<double>();double end=Time.realtimeSinceStartupAsDouble+4;
                    while(Time.realtimeSinceStartupAsDouble<end){yield return null;samples.Add(Time.unscaledDeltaTime*1000d);}
                    samples.Sort();double Percent(float p)=>samples[Mathf.Min(samples.Count-1,Mathf.CeilToInt(samples.Count*p)-1)];
                    lines.Add(FormattableString.Invariant($"{new[]{"day","night","flashlight"}[mode]},{index+1},{Screen.width},{Screen.height},{samples.Count},{samples.Average():F3},{Percent(.95f):F3},{Percent(.99f):F3},{LightmapSettings.lightmaps.Length},{UnityEngine.Rendering.ProbeReferenceVolume.instance.lightingScenario}"));
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));File.WriteAllLines(output,lines);
                }
            }
            lines.Add("COMPLETE");File.WriteAllLines(output,lines);Application.Quit();
        }
    }
}
