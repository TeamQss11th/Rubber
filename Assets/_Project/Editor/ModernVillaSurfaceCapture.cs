using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using Rubber.World;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class ModernVillaSurfaceCapture
    {
        static ModernVillaSurfaceCapture(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("FloorCapture",false)){SessionState.SetBool("FloorCapture",false);var cycle=Object.FindAnyObjectByType<VillaTimeOfDay>();cycle.StartCoroutine(Capture(cycle));}};}
        public static void Run(){if(Application.isPlaying)return;SessionState.SetBool("FloorCapture",true);EditorApplication.isPlaying=true;}
        static IEnumerator Capture(VillaTimeOfDay cycle)
        {
            yield return new WaitForSecondsRealtime(2);cycle.running=false;cycle.SetHour(12);var walk=Object.FindAnyObjectByType<ModernVillaWalkthrough>();walk.enabled=false;walk.GetComponent<CharacterController>().enabled=false;
            var camera=walk.view;var positions=new[]{new Vector3(1,3,26.5f),new Vector3(-4,0,26.5f),new Vector3(4.8f,1.9f,16.4f)};var targets=new[]{new Vector3(-2,-.6f,26.5f),new Vector3(-.2f,.1f,26.5f),new Vector3(3.75f,1.3f,15)};
            try{for(int i=0;i<positions.Length;i++){camera.position=positions[i];camera.rotation=Quaternion.LookRotation(targets[i]-positions[i]);yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(ModernVillaSurfaceAudit.Folder+"/view-"+i+".png"));yield return new WaitForSecondsRealtime(.3f);}}
            finally{EditorApplication.isPlaying=false;}
        }
    }
}
