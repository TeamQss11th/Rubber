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
    [InitializeOnLoad] public static class VillaHuntChecks
    {
        static VillaHuntChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VillaHuntChecks",false)){SessionState.SetBool("VillaHuntChecks",false);UnityEngine.Object.FindAnyObjectByType<PlayerMovement>().StartCoroutine(Check());}};}
        public static void Run(){if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");SessionState.SetBool("VillaHuntChecks",true);EditorApplication.isPaused=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;}
        static IEnumerator Check()
        {
            bool background=Application.runInBackground;Application.runInBackground=true;var report=new List<string>();
            void Assert(bool pass,string name){report.Add((pass?"PASS ":"FAIL ")+name);}
            try
            {
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>();player.enabled=false;
                var ducks=UnityEngine.Object.FindObjectsByType<RubberDuckInteractable>().OrderBy(d=>d.Data.Id).ToArray();var initial=ducks.Select(d=>d.transform.position).ToArray();
                var registry=UnityEngine.Object.FindAnyObjectByType<RubberDuckReturnRegistry>();
                Assert(ducks.Length==25&&ducks.Select(d=>d.Data.Id).Distinct().Count()==25&&registry.TotalDuckCount==25,"25 unique collectibles registered");
                var barriers=GameObject.Find("Villa Boundary - Invisible Colliders").GetComponentsInChildren<BoxCollider>();
                Assert(barriers.Length==4 && barriers.All(b=>!b.isTrigger&&!b.attachedRigidbody&&!b.GetComponent<Renderer>()),"4 static invisible solid barriers");
                foreach(var wall in barriers)
                {
                    var bounds=wall.bounds;bool vertical=bounds.size.x<bounds.size.z;var direction=vertical?Vector3.right:Vector3.forward;
                    int blocked=0,total=0;
                    foreach(float height in new[]{0f,6f,12f})for(int i=0;i<=10;i++)
                    {
                        var center=bounds.center;center.y=height+.9f;
                        if(vertical)center.z=Mathf.Lerp(bounds.min.z+.1f,bounds.max.z-.1f,i/10f);else center.x=Mathf.Lerp(bounds.min.x+.1f,bounds.max.x-.1f,i/10f);
                        center-=direction*1.5f;
                        var hits=Physics.CapsuleCastAll(center-Vector3.up*.6f,center+Vector3.up*.6f,.3f,direction,3,~0,QueryTriggerInteraction.Ignore);
                        total++;if(hits.Any(h=>h.collider==wall))blocked++;
                    }
                    Assert(blocked==total,wall.name+" capsule crossings blocked="+blocked+"/"+total);
                }
                yield return new WaitForSeconds(8);
                for(int i=0;i<ducks.Length;i++)
                {
                    var duck=ducks[i];var body=duck.GetComponent<Rigidbody>();
                    Assert(Vector3.Distance(initial[i],duck.transform.position)<.15f,duck.name+" stable placement; drift="+Vector3.Distance(initial[i],duck.transform.position).ToString("F3"));
                    // Ignore only this duck while finding a standable, unblocked approach.
                    var collider=duck.GetComponent<Collider>();collider.enabled=false;Physics.SyncTransforms();
                    bool reachable=VillaDuckHuntSetup.FindApproach(duck.transform.position,out var approach);
                    collider.enabled=true;Physics.SyncTransforms();
                    var eye=approach+Vector3.up*1.6f;var target=collider.bounds.center;
                    bool hit=reachable&&Physics.Raycast(eye,(target-eye).normalized,out var ray,4.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&ray.collider.GetComponentInParent<RubberDuckInteractable>()==duck;
                    Assert(hit,duck.name+" reachable pickup ray");
                }
                Assert(registry.ReturnedCount==0,"hidden ducks not accidentally inside return pools");
            }
            finally{File.WriteAllLines("Docs/DuckHunt/checks.txt",report);Application.runInBackground=background;EditorApplication.isPlaying=false;}
        }
    }
}
