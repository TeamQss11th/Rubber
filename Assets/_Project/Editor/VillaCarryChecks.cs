using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Rubber.Gameplay.Player;
using Rubber.Gameplay.Ducks;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaCarryChecks
    {
        static VillaCarryChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillaCarryChecks",false)){SessionState.SetBool("VillaCarryChecks",false);UnityEngine.Object.FindAnyObjectByType<PlayerMovement>().StartCoroutine(Check());}};}
        public static void Run(){if(Application.isPlaying)throw new InvalidOperationException("Exit Play before carry check.");SessionState.SetBool("VillaCarryChecks",true);EditorApplication.isPaused=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;}
        static IEnumerator Check()
        {
            bool background=Application.runInBackground;Application.runInBackground=true;
            var report=new List<string>();Directory.CreateDirectory("Docs/CarryDiagnosis");
            try
            {
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();player.GetComponent<PlayerInputReader>().enabled=false;
                var carrier=player.GetComponent<PlayerDuckCarrier>();var look=player.GetComponent<PlayerCamera>();
                var duck=UnityEngine.Object.FindObjectsByType<RubberDuckInteractable>().OrderBy(d=>d.Data.Id).First();var body=duck.GetComponent<Rigidbody>();
                report.Add("Initial interpolation="+body.interpolation+" collision="+body.collisionDetectionMode);
                yield return new WaitForSeconds(1);
                report.Add("Pickup="+carrier.TryPickUp(duck));
                report.Add("Actual held interpolation="+body.interpolation);
                foreach(var mode in new[]{body.interpolation,RigidbodyInterpolation.Interpolate,RigidbodyInterpolation.None})
                {
                    body.interpolation=mode;duck.transform.localPosition=Vector3.zero;duck.transform.localRotation=Quaternion.identity;
                    float maxPosition=0,maxAngle=0;
                    for(int i=0;i<90;i++)
                    {
                        player.Move(new Vector2(i<45?.5f:-.5f,0));look.Look(new Vector2(i<45?5:-5,Mathf.Sin(i*.2f)*2));
                        yield return new WaitForEndOfFrame();
                        maxPosition=Mathf.Max(maxPosition,duck.transform.localPosition.magnitude);
                        maxAngle=Mathf.Max(maxAngle,Quaternion.Angle(Quaternion.identity,duck.transform.localRotation));
                    }
                    player.ClearInput();report.Add(mode+": max anchor local position drift="+maxPosition.ToString("F6")+" m; rotation drift="+maxAngle.ToString("F4")+" deg");
                }
                report.Add("Drop="+carrier.TryDrop());report.Add("Released interpolation="+body.interpolation);
            }
            finally{File.WriteAllLines("Docs/CarryDiagnosis/checks.txt",report);Application.runInBackground=background;EditorApplication.isPlaying=false;}
        }
    }
}
