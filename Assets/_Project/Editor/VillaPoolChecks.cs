using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Rubber.World;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaPoolChecks
    {
        static VillaPoolChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillaPoolChecks",false)){SessionState.SetBool("VillaPoolChecks",false);var test=UnityEngine.Object.FindAnyObjectByType<VillaPoolTestControls>();test.StartCoroutine(Check(test));}};}
        public static void Run(){if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");EditorApplication.isPaused=false;var gameView=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));gameView.Show();gameView.Focus();SessionState.SetBool("VillaPoolChecks",true);EditorApplication.isPlaying=true;}
        static IEnumerator Check(VillaPoolTestControls test)
        {
            bool background=Application.runInBackground;Application.runInBackground=true;
            File.WriteAllText("Docs/VillaPool/progress.txt","Started "+DateTime.Now+" paused="+EditorApplication.isPaused);
            var report=new List<string>();var errors=new List<string>();Application.LogCallback callback=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=callback;
            var cycle=UnityEngine.Object.FindAnyObjectByType<VillaTimeOfDay>();cycle.running=false;cycle.SetHour(12);test.walkthrough.GoTo(test.poolCheckpoint);test.walkthrough.enabled=false;var cam=test.walkthrough.view;cam.position=new Vector3(-1.2f,1.65f,27.5f);cam.rotation=Quaternion.LookRotation(new Vector3(-5.1f,.12f,26.5f)-cam.position);
            try
            {
                var s=test.water.surface;report.Add($"Surface {s.bounds} enabled={s.enabled} active={s.gameObject.activeInHierarchy} shader={s.sharedMaterial.shader.name} supported={s.sharedMaterial.shader.isSupported} passes={s.sharedMaterial.passCount}");foreach(var message in ShaderUtil.GetShaderMessages(s.sharedMaterial.shader))report.Add("SHADER "+message.message);
                yield return new WaitForSecondsRealtime(12);
                File.AppendAllText("Docs/VillaPool/progress.txt","\nPhysics wait complete "+DateTime.Now);
                foreach(var d in test.ducks){var rb=d.GetComponent<Rigidbody>();report.Add($"{d.name}: position={rb.position:F3}, floating={d.IsFloating}, settled={d.IsSettled}, speed={rb.linearVelocity.magnitude:F4}, up={Vector3.Dot(d.transform.up,Vector3.up):F3}");}
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/VillaPool/day.png"));
                test.ducks[0].GetComponent<Rigidbody>().AddForce(Vector3.left*.1f,ForceMode.Impulse);yield return new WaitForSecondsRealtime(3);report.Add("External push: "+test.ducks[0].transform.position.ToString("F3"));
                cycle.SetHour(0);yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/VillaPool/night.png"));
                var body=test.ducks[0].GetComponent<Rigidbody>();body.position=new Vector3(-7,.02f,29.5f);body.linearVelocity=Vector3.forward*3;yield return new WaitForSecondsRealtime(2);report.Add($"Wall stop: position={body.position:F3}, within wall={body.position.z<30.01f}");
                body.position=new Vector3(2,.8f,26.5f);body.linearVelocity=Vector3.zero;yield return new WaitForSecondsRealtime(2);report.Add($"Outside water: floating={test.ducks[0].IsFloating}, position={body.position:F3}");
                test.ResetDucks();yield return new WaitForSecondsRealtime(8);int settled=0;foreach(var d in test.ducks)if(d.IsSettled)settled++;report.Add($"Reset: settled={settled}/{test.ducks.Length}");
                report.Add("Errors="+errors.Count);report.AddRange(errors);File.WriteAllLines("Docs/VillaPool/checks.txt",report);
            }
            finally{Application.logMessageReceived-=callback;Application.runInBackground=background;EditorApplication.isPlaying=false;}
        }
    }
}

