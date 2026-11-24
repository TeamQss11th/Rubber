using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Rubber.UI;
using Rubber.Gameplay.Player;

namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaMenuChecks
    {
        static VillaMenuChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillaMenuChecks",false)){SessionState.SetBool("VillaMenuChecks",false);Rubber.Core.SceneManager.instance.StartCoroutine(Check());}};}
        public static void Run()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode.");
            EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(VillaFlowSetup.Menu);
            SessionState.SetBool("VillaMenuChecks",true);
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;
        }
        static IEnumerator Wait(Func<bool> ready)
        {
            float until=Time.realtimeSinceStartup+90;
            while(!ready()){if(Time.realtimeSinceStartup>until)throw new TimeoutException("Menu check timed out.");yield return null;}
        }
        static IEnumerator Check()
        {
            Directory.CreateDirectory("Docs/MenuBackground");var lines=new List<string>();var errors=new List<string>();
            void Assert(bool condition,string label){lines.Add((condition?"PASS ":"FAIL ")+label);File.WriteAllLines("Docs/MenuBackground/checks.txt",lines);}
            Application.LogCallback log=(m,s,t)=>{if(t==LogType.Exception||t==LogType.Error)errors.Add(m);};Application.logMessageReceived+=log;
            bool background=Application.runInBackground;Application.runInBackground=true;
            try
            {
                var bg=UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>();
                yield return null;
                Assert(LoadingScreenUI.IsLoading&&!bg.Ready,"startup covered by loading screen before villa is ready");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/MenuBackground/loading.png"));
                yield return Wait(()=>bg.Ready);yield return new WaitForSecondsRealtime(3);
                Assert(!LoadingScreenUI.IsLoading,"startup loading completes after rendered background");
                Assert(UnityEngine.Object.FindObjectsByType<PlayerInputReader>().Length==1&&!UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>().enabled,"preview player disabled");
                Assert(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1&&Cursor.lockState==CursorLockMode.None,"one menu input system and free cursor");
                float hour=bg.Lighting.hour;var position=bg.menuCamera.transform.position;
                yield return new WaitForSecondsRealtime(2);
                Assert(bg.Lighting.hour>hour&&Vector3.Distance(position,bg.menuCamera.transform.position)>.01f,"time and camera advance");
                Assert(bg.menuCamera.targetTexture.height<=1080&&bg.menuCamera.targetTexture.width<=Screen.width,"background source capped at 1080p");
                var menuUI=UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>();
                var startButton=(UnityEngine.UI.Button)new SerializedObject(menuUI).FindProperty("startButton").objectReferenceValue;
                var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,startButton.transform.position)},hits);
                Assert(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==startButton,"background does not block Start button");
                bg.AnimateCamera=false;bg.Lighting.running=false;
                for(int i=0;i<5;i++)
                {
                    bg.OrbitTime=i*bg.orbitSeconds/4;bg.PositionCamera();bg.Lighting.SetHour(i==3?18:i==4?23:10);
                    yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath($"Docs/MenuBackground/view-{i}.png"));
                }
                UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>().StartGame();
                yield return Wait(()=>!LoadingScreenUI.IsLoading&&!UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>());
                yield return null;
                var flow=UnityEngine.Object.FindAnyObjectByType<Rubber.Core.VillaGameFlow>();
                Assert(flow&&flow.enabled&&flow.input.enabled&&flow.registry.ReturnedCount==0,"fresh gameplay after menu preview");
                Assert(SceneManager.sceneCount==1&&UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"no duplicate villa or event system");
                flow.Pause();flow.ReturnToMenu();
                yield return Wait(()=>{var b=UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>();return b&&b.Ready&&!LoadingScreenUI.IsLoading;});
                Assert(UnityEngine.Object.FindObjectsByType<VillaMenuBackground>().Length==1,"menu return restores one background");
                // A second menu/game round trip must also restore controls.
                UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>().StartGame();
                yield return Wait(()=>!LoadingScreenUI.IsLoading&&!UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>());
                yield return null;
                Assert(UnityEngine.Object.FindAnyObjectByType<Rubber.Core.VillaGameFlow>().input.enabled,"second start restores gameplay input");
                flow=UnityEngine.Object.FindAnyObjectByType<Rubber.Core.VillaGameFlow>();flow.Pause();flow.ReturnToMenu();
                yield return Wait(()=>UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>());
                menuUI=UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>();
                yield return null; // Register the original button listeners before clicking.
                menuUI.StartGame();
                yield return Wait(()=>!LoadingScreenUI.IsLoading);
                Assert(UnityEngine.Object.FindAnyObjectByType<MainMenuTransition>(),"Start during menu return does not start a competing transition");
                menuUI.StartGame();
                yield return Wait(()=>!LoadingScreenUI.IsLoading&&!UnityEngine.Object.FindAnyObjectByType<VillaMenuBackground>());
                yield return null;
                Assert(SceneManager.sceneCount==1&&UnityEngine.Object.FindAnyObjectByType<Rubber.Core.VillaGameFlow>().input.enabled,"Start safely waits for preview cleanup");
                Assert(errors.Count==0,"runtime errors="+errors.Count);
            }
            finally
            {
                lines.AddRange(errors);File.WriteAllLines("Docs/MenuBackground/checks.txt",lines);
                Application.logMessageReceived-=log;Application.runInBackground=background;EditorApplication.isPlaying=false;
            }
        }
    }
}
