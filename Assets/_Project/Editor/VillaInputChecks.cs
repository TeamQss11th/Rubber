using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Rubber.Gameplay.Player;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaInputChecks
    {
        static VillaInputChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillaInputChecks",false)){SessionState.SetBool("VillaInputChecks",false);UnityEngine.Object.FindAnyObjectByType<PlayerMovement>().StartCoroutine(Check());}};}
        public static void Run(){if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");SessionState.SetBool("VillaInputChecks",true);EditorApplication.isPaused=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;}
        static IEnumerator Check()
        {
            var report=new List<string>();Directory.CreateDirectory("Docs/InputDiagnosis");
            Keyboard keyboard=null;Mouse mouse=null;bool background=Application.runInBackground;Application.runInBackground=true;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            try
            {
                var reader=UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>();var type=typeof(PlayerInputReader);
                var actions=(InputActionAsset)type.GetField("runtimeActions",flags).GetValue(reader);
                report.Add("Reader enabled="+reader.enabled+" map enabled="+actions.FindActionMap("Player").enabled);
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
                actions.devices=new InputDevice[]{keyboard,mouse};
                yield return null;
                type.GetMethod("SetCapture",flags).Invoke(reader,new object[]{true});
                Cursor.lockState=CursorLockMode.None;
                type.GetMethod("Update",flags).Invoke(reader,null);
                report.Add("After unexpected unlock: captured="+type.GetField("captured",flags).GetValue(reader)+" cursor="+Cursor.lockState);
                InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});
                yield return null;yield return null;
                report.Add("Click recaptures="+(Cursor.lockState==CursorLockMode.Locked));
                InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
                var player=reader.GetComponent<PlayerMovement>();var start=player.transform.position;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
                yield return new WaitForSeconds(.3f);
                report.Add("W action="+actions.FindAction("Player/Move").ReadValue<Vector2>()+" travel="+Vector3.Distance(start,player.transform.position).ToString("F3"));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
                report.Add("Escape releases="+(Cursor.lockState==CursorLockMode.None));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return null;yield return null;
                report.Add("Click after Escape recaptures="+(Cursor.lockState==CursorLockMode.Locked));
            }
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);
                File.WriteAllLines("Docs/InputDiagnosis/checks.txt",report);Application.runInBackground=background;EditorApplication.isPlaying=false;
            }
        }
    }
}
