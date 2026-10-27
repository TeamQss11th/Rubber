using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Rubber.Gameplay.Player;

namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaPlayerComfortChecks
    {
        static VillaPlayerComfortChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillaComfort",false)){SessionState.SetBool("VillaComfort",false);Rubber.Core.SceneManager.instance.StartCoroutine(Check());}};}
        public static void Run()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode.");
            EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(ModernVillaSetup.ScenePath);
            SessionState.SetBool("VillaComfort",true);EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;
        }
        static IEnumerator Check()
        {
            Directory.CreateDirectory("Docs/PlayerComfort");var lines=new List<string>();var errors=new List<string>();
            void Assert(bool ok,string label){lines.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines("Docs/PlayerComfort/checks.txt",lines);}
            Application.LogCallback log=(m,s,t)=>{if(t==LogType.Exception||t==LogType.Error)errors.Add(m);};Application.logMessageReceived+=log;
            bool background=Application.runInBackground;Application.runInBackground=true;Keyboard keyboard=null;
            try
            {
                yield return new WaitForSecondsRealtime(2);
                var flow=UnityEngine.Object.FindAnyObjectByType<Rubber.Core.VillaGameFlow>();
                var motor=flow.movement;var flashlight=motor.GetComponent<PlayerFlashlight>();
                keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                Assert(!flashlight.IsOn,"flashlight initially off");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return null;yield return null;
                Assert(flashlight.IsOn,"F enables flashlight");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                flow.Pause();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return null;yield return null;
                Assert(flashlight.IsOn,"F ignored while paused");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;flow.Resume();yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return null;yield return null;
                Assert(!flashlight.IsOn,"F disables flashlight after resume");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                var clock=UnityEngine.Object.FindAnyObjectByType<Rubber.World.VillaTimeOfDay>();clock.running=false;clock.SetHour(0);flashlight.Toggle();
                yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/PlayerComfort/flashlight.png"));
                flashlight.Toggle();clock.SetHour(12);
                flow.enabled=false;flow.input.enabled=false;
                motor.GetComponent<PlayerCamera>().enabled=false;
                var body=motor.GetComponent<Rigidbody>();var camera=motor.GetComponentInChildren<Camera>();
                var starts=new[]{new Vector3(3.75f,.456f,16.5f),new Vector3(7.5f,-.24f,6.2f),new Vector3(4.15f,.25f,8.748f),new Vector3(1.57f,.25f,12.2f),new Vector3(.9f,.25f,26.5f)};
                var ends=new[]{new Vector3(3.75f,3.05f,12.5f),new Vector3(7.5f,.25f,7.7f),new Vector3(2.6f,.75f,8.748f),new Vector3(1.57f,.75f,10.2f),new Vector3(-3.8f,-1.633f,26.5f)};
                string[] names={"annex","pool-front","terrace-side","backyard","pool-basin"};
                for(int route=0;route<starts.Length;route++)for(int reverse=0;reverse<2;reverse++)
                {
                    Vector3 start=reverse==0?starts[route]:ends[route], end=reverse==0?ends[route]:starts[route];
                    motor.ClearInput();body.position=start+Vector3.up*.04f;body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
                    yield return new WaitForSeconds(.3f);
                    int airborne=0;float maxFall=0;
                    for(int tick=0;tick<350;tick++)
                    {
                        var delta=Vector3.ProjectOnPlane(end-body.position,Vector3.up);if(delta.magnitude<.12f)break;
                        camera.transform.rotation=Quaternion.LookRotation(delta);motor.Move(Vector2.up);
                        yield return new WaitForFixedUpdate();
                        if(!motor.IsGrounded)airborne++;maxFall=Mathf.Min(maxFall,body.linearVelocity.y);
                    }
                    motor.ClearInput();yield return new WaitForSeconds(.25f);
                    float error=Vector3.ProjectOnPlane(end-body.position,Vector3.up).magnitude;
                    Assert(error<.35f&&Mathf.Abs(body.position.y-end.y)<.35f,$"{names[route]} direction={reverse} end={body.position:F3} error={error:F3} airborneTicks={airborne} maxFall={maxFall:F2}");
                }
                body.position=new Vector3(0,.8f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForSeconds(.5f);
                float baseY=body.position.y;motor.Jump();yield return new WaitForSeconds(.18f);
                Assert(body.position.y>baseY+.25f,"jump remains free from stair snap");
                yield return new WaitForSeconds(1);
                Assert(motor.IsGrounded,"jump lands normally");Assert(errors.Count==0,"runtime errors="+errors.Count);
            }
            finally{if(keyboard!=null)InputSystem.RemoveDevice(keyboard);lines.AddRange(errors);File.WriteAllLines("Docs/PlayerComfort/checks.txt",lines);Application.logMessageReceived-=log;Application.runInBackground=background;Time.timeScale=1;EditorApplication.isPlaying=false;}
        }
    }
}
