using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Rubber.World;

namespace Rubber.EditorTools
{
    [InitializeOnLoad]
    public static class VillaTimeLifecycleChecks
    {
        const string Request="Library/VillaTimeLifecycleChecks.request";
        const string Result="Library/VillaTimeLifecycleChecks.txt";
        static VillaTimeOfDay cycle;
        static readonly List<string> lines=new List<string>();
        static readonly List<string> errors=new List<string>();
        static int stage=-1;
        static double next;
        static int afterFrame;
        static float originalIntensity;
        static VillaTimeLifecycleChecks(){EditorApplication.update+=Tick;}
        [MenuItem("Rubber/Time Of Day/Check Destroyed Sun Lifecycle")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run from Edit mode.");
            SessionState.SetBool("VillaTimeLifecycleChecks",true);EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists(Request)&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete(Request);Run();return;}
            if(stage<0&&Application.isPlaying&&SessionState.GetBool("VillaTimeLifecycleChecks",false))
            {
                SessionState.SetBool("VillaTimeLifecycleChecks",false);cycle=UnityEngine.Object.FindAnyObjectByType<VillaTimeOfDay>();
                lines.Clear();errors.Clear();Application.logMessageReceived+=Log;stage=0;next=EditorApplication.timeSinceStartup+2;afterFrame=Time.frameCount+2;
            }
            if(stage>=0&&Application.isPlaying)EditorApplication.QueuePlayerLoopUpdate();
            if(stage<0||EditorApplication.timeSinceStartup<next)return;
            if(Time.frameCount<afterFrame)return;
            try
            {
                switch(stage)
                {
                    case 0:
                        if(!cycle||!cycle.sun||cycle.BlendingBufferBytes==0)throw new Exception("Expected initialized Modern Villa cycle.");
                        // Disable restores saved light state; re-enable must continue working.
                        cycle.enabled=false;originalIntensity=cycle.sun.intensity;
                        lines.Add("Normal disable restored surviving sun; intensity="+originalIntensity);
                        stage=1;break;
                    case 1:
                        if(cycle.BlendingBufferBytes!=0)throw new Exception("Normal disable leaked blend buffers.");
                        cycle.enabled=true;cycle.SetHour(18);
                        if(cycle.BlendingBufferBytes==0)throw new Exception("Re-enable did not allocate buffers.");
                        UnityEngine.Object.Destroy(cycle.sun.gameObject);stage=2;break;
                    case 2:
                        if(cycle.sun)throw new Exception("Test sun destruction has not completed.");
                        cycle.SetHour(12);lines.Add("PASS: Apply and Update tolerate a destroyed Unity Light.");
                        cycle.enabled=false;stage=3;break;
                    case 3:
                        if(cycle.BlendingBufferBytes!=0)throw new Exception("Destroyed-sun disable leaked blend buffers.");
                        lines.Add("PASS: OnDisable with destroyed sun completes and releases all blend buffers.");
                        lines.Add("Errors="+errors.Count);lines.AddRange(errors);File.WriteAllLines(Result,lines);
                        Application.logMessageReceived-=Log;stage=-1;EditorApplication.isPlaying=false;return;
                }
                next=EditorApplication.timeSinceStartup+.5;
                afterFrame=Time.frameCount+2;
            }
            catch(Exception e)
            {
                lines.Add("FAIL: "+e);lines.AddRange(errors);File.WriteAllLines(Result,lines);Application.logMessageReceived-=Log;stage=-1;EditorApplication.isPlaying=false;
            }
        }
    }
}
