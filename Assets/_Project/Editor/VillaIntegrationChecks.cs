using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Rubber.World;
using Rubber.Gameplay.Player;
using Rubber.Gameplay.Ducks;
using Rubber.Gameplay.Interaction;
namespace Rubber.EditorTools
{
    [InitializeOnLoad] public static class VillaIntegrationChecks
    {
        static VillaIntegrationChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VillaIntegrationChecks",false)){SessionState.SetBool("VillaIntegrationChecks",false);UnityEngine.Object.FindAnyObjectByType<PlayerMovement>().StartCoroutine(Check());}};}
        public static void Run(){SessionState.SetBool("VillaIntegrationChecks",true);EditorApplication.isPaused=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;}
        static IEnumerator Check()
        {
            var report=new List<string>();var errors=new List<string>();
            Application.LogCallback log=(m,s,t)=>{if(t==LogType.Exception||t==LogType.Error)errors.Add(m);};Application.logMessageReceived+=log;
            bool background=Application.runInBackground;Application.runInBackground=true;
            void Assert(bool condition,string name){report.Add((condition?"PASS ":"FAIL ")+name);}
            try
            {
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();player.GetComponent<PlayerInputReader>().enabled=false;
                var registry=UnityEngine.Object.FindAnyObjectByType<RubberDuckReturnRegistry>();
                var ducks=UnityEngine.Object.FindObjectsByType<RubberDuckInteractable>().OrderBy(d=>d.Data.Id).ToArray();
                var pools=UnityEngine.Object.FindObjectsByType<VillaPoolWater>();
                yield return new WaitForSecondsRealtime(2);
                Assert(player.IsGrounded,"player grounded at spawn");float y=player.transform.position.y;player.Jump();yield return new WaitForSeconds(.2f);Assert(player.transform.position.y>y+.1f,"player jump");yield return new WaitForSeconds(1);
                Assert(registry.TotalDuckCount==ducks.Length && ducks.Length>0 && registry.ReturnedCount==0,"all unique ducks registered, none returned on land");
                var doors=UnityEngine.Object.FindObjectsByType<VillaDoorInteractable>();
                foreach(var door in doors)Assert(door.TryInteract(player.gameObject),"idle door accepts interaction: "+door.name);
                yield return new WaitForSeconds(3);
                Assert(doors.All(d=>d.GetComponent<VillaSlidingDoor>().Progress>.99f),"all eight doors finish opening");
                foreach(var door in doors)door.TryInteract(player.gameObject);
                var carrier=player.GetComponent<PlayerDuckCarrier>();
                Assert(ducks[0].TryInteract(player.gameObject) && ducks[0].IsHeld,"duck pickup via IInteractable");
                yield return new WaitForFixedUpdate();Assert(!ducks[0].GetComponent<VillaDuckBuoyancy>().IsFloating,"held duck buoyancy suspended");
                Assert(carrier.TryDrop() && !ducks[0].IsHeld,"duck release restores physics");
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                var placements=new List<Vector3>();
                for(int i=0;i<ducks.Length;i++)
                {
                    var pool=pools[i%pools.Length];Vector3 position=default;bool found=false;
                    var center=pool.surface.bounds.center;
                    foreach(int k in Enumerable.Range(0,pool.wetCells.Length).OrderBy(k=>Mathf.Pow(pool.origin.x+(k%pool.columns+.5f)*pool.cellSize-center.x,2)+Mathf.Pow(pool.origin.y+(k/pool.columns+.5f)*pool.cellSize-center.z,2)))
                    {
                        if(!pool.wetCells[k])continue;
                        var p=new Vector3(pool.origin.x+(k%pool.columns+.5f)*pool.cellSize,pool.Height+.5f,pool.origin.y+(k/pool.columns+.5f)*pool.cellSize);
                        if(placements.Any(q=>Vector3.Distance(q,p)<1.2f))continue;
                        if(!Physics.Raycast(new Vector3(p.x,pool.Height+.1f,p.z),Vector3.down,out var floor,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||floor.point.y>pool.Height-.4f)continue;
                        if(!new[]{new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,-.5f),new Vector3(-.5f,0,-.5f)}.All(offset=>pool.Contains(p+offset)))continue;
                        position=p;found=true;break;
                    }
                    Assert(found,"pool interior placement "+i);placements.Add(position);
                    var rb=ducks[i].GetComponent<Rigidbody>();rb.position=position;rb.rotation=Quaternion.identity;rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;
                }
                Assert(registry.ReturnedCount==0,"no collection while still above water");
                yield return new WaitForSeconds(12);
                foreach(var duck in ducks){var b=duck.GetComponent<VillaDuckBuoyancy>();Assert(duck.IsReturned && b.IsFloating,"floating and registered: "+duck.name+" pos="+duck.transform.position+" settled="+b.IsSettled+" returned="+duck.IsReturned);}
                Assert(registry.IsComplete && registry.ReturnedCount==ducks.Length,"collection completes across both pools");
                Assert(!registry.TryRegister(ducks[0]) && registry.ReturnedCount==ducks.Length,"duplicate return rejected");
                Assert(doors.All(d=>d.GetComponent<VillaSlidingDoor>().Progress<.01f),"all doors close");
                Assert(errors.Count==0,"runtime errors="+errors.Count);report.AddRange(errors);
            }
            finally{File.WriteAllLines("Docs/PlayerIntegration/checks.txt",report);Application.logMessageReceived-=log;Application.runInBackground=background;EditorApplication.isPlaying=false;}
        }
    }
}
