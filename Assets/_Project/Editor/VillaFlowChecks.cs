using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rubber.Gameplay.Ducks;
using Rubber.Gameplay.Player;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Flow=Rubber.Core.VillaGameFlow;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaFlowChecks
    {
        static VillaFlowChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillaFlowChecks",false)){SessionState.SetBool("VillaFlowChecks",false);Rubber.Core.SceneManager.instance.StartCoroutine(Check());}};}
        public static void Run()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");
            EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(VillaFlowSetup.Menu);SessionState.SetBool("VillaFlowChecks",true);
            EditorApplication.isPaused=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;
        }
        static IEnumerator WaitScene(string name,Flow old=null)
        {
            float limit=Time.realtimeSinceStartup+60;
            while(Time.realtimeSinceStartup<limit)
            {
                var flow=UnityEngine.Object.FindAnyObjectByType<Flow>();
                if(SceneManager.GetActiveScene().name==name&&!LoadingScreenUI.IsLoading&&(name=="MainMenu"||flow&&flow!=old))yield break;
                yield return null;
            }
            throw new TimeoutException("Scene transition timed out: "+name);
        }
        static IEnumerator Check()
        {
            Directory.CreateDirectory("Docs/GameFlow");var report=new List<string>();var errors=new List<string>();
            void Assert(bool value,string text){report.Add((value?"PASS ":"FAIL ")+text);File.WriteAllLines("Docs/GameFlow/checks.txt",report);}
            bool background=Application.runInBackground;Application.runInBackground=true;
            Keyboard keyboard=null;
            Application.LogCallback callback=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=callback;
            try
            {
                yield return new WaitForSecondsRealtime(2);
                var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>();Assert(menu!=null,"MainMenu ready");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/GameFlow/menu.png"));
                menu.StartGame();menu.StartGame();yield return WaitScene("Modern Villa");yield return null;
                var flow=UnityEngine.Object.FindAnyObjectByType<Flow>();Assert(flow&&flow.registry.TotalDuckCount==25&&flow.registry.ReturnedCount==0,"menu starts 25-duck game");
                Assert(flow.input.enabled&&flow.counter.text=="00 / 25"&&Time.timeScale==1,"HUD/input/time ready after loading");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/GameFlow/gameplay.png"));
                keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));
                for(int sample=0;sample<4;sample++){yield return null;report.Add($"Escape sample {sample}: phase={flow.CurrentPhase} frame={Time.frameCount} current={Keyboard.current.deviceId} virtual={keyboard.deviceId} pressed={keyboard.escapeKey.wasPressedThisFrame} held={keyboard.escapeKey.isPressed}");}
                Assert(!flow.input.enabled&&Time.timeScale==0&&flow.pausePanel.activeSelf,"Escape pauses and blocks gameplay input/physics");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                float pausedTime=flow.ElapsedSeconds;yield return new WaitForSecondsRealtime(.2f);Assert(Mathf.Abs(flow.ElapsedSeconds-pausedTime)<.001f,"session timer excludes pause");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/GameFlow/pause.png"));
                flow.OpenSettings();Assert(flow.settings.IsSettingsOpen&&Time.timeScale==0,"shared settings while paused");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/GameFlow/settings.png"));
                flow.settings.ToggleControlKeyBackground();flow.settings.HandleBack();Assert(flow.settings.IsSettingsOpen,"back closes control help before settings");
                flow.settings.CloseSettings();Assert(flow.pausePanel.activeSelf,"settings close returns to pause");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
                Assert(flow.input.enabled&&Time.timeScale==1&&Cursor.lockState==CursorLockMode.Locked,"Escape resume restores gameplay and cursor");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                var ducks=UnityEngine.Object.FindObjectsByType<RubberDuckInteractable>();foreach(var duck in ducks)Assert(flow.registry.TryRegister(duck),"progress registration "+duck.Data.Id);
                Assert(flow.CurrentPhase==Flow.Phase.Complete&&flow.completePanel.activeSelf&&!flow.input.enabled&&flow.counter.text=="25 / 25","25 returns produce completion and block gameplay");
                Assert(!flow.registry.TryRegister(ducks[0]),"duplicate does not count");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/GameFlow/complete.png"));
                var old=flow;flow.Restart();yield return WaitScene("Modern Villa",old);yield return null;
                flow=UnityEngine.Object.FindAnyObjectByType<Flow>();Assert(flow.registry.ReturnedCount==0&&flow.registry.TotalDuckCount==25&&flow.input.enabled&&Time.timeScale==1,"restart resets collection/input/time");
                flow.Pause();flow.ReturnToMenu();yield return WaitScene("MainMenu");yield return new WaitForSecondsRealtime(.3f);
                Assert(UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>()&&Time.timeScale==1&&Cursor.lockState==CursorLockMode.None,"return to menu resets time/cursor");
                Assert(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1&&UnityEngine.Object.FindObjectsByType<Rubber.Core.SceneManager>().Length==1,"no duplicate EventSystem or scene manager");
                Assert(errors.Count==0,"runtime errors="+errors.Count);report.AddRange(errors);
            }
            finally{if(keyboard!=null)InputSystem.RemoveDevice(keyboard);File.WriteAllLines("Docs/GameFlow/checks.txt",report);Application.logMessageReceived-=callback;Application.runInBackground=background;Time.timeScale=1;EditorApplication.isPlaying=false;}
        }
    }
}
