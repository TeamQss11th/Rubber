using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rubber.EditorTools
{
    public static class BeachVillaExpansionReview
    {
        const string Report=BeachVillaExpansionSurvey.Reports;
        [MenuItem("Rubber/Expansion/3 Refine and Validate Routes")]
        public static void Validate()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit play first.");
            BeachVillaLandscapeFinish.Apply();
            var scene=SceneManager.GetActiveScene();var log=new StringBuilder();
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if(!source.Contains("/Plants/Palm") || PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject)!=t.gameObject)continue;
                foreach(var c in t.GetComponentsInChildren<Collider>())c.enabled=false;
                var trunk=t.GetComponent<CapsuleCollider>();if(!trunk)trunk=t.gameObject.AddComponent<CapsuleCollider>();
                trunk.enabled=true;trunk.radius=.20f;trunk.height=3;trunk.center=new Vector3(0,1.5f,0);
            }
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();var body=walk.GetComponent<CharacterController>();body.enabled=false;Physics.SyncTransforms();
            int mask=~body.excludeLayers.value;const float step=.5f;const int nx=153,nz=139;var valid=new bool[nx,nz];var height=new float[nx,nz];
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
            {
                var p=new Vector3(-38+x*step,0,-21+z*step);
                if(p.x>-14 && p.x< -1 && p.z>0 && p.z<18)continue; // pool is destination, not a shortcut
                if(!Physics.Raycast(p+Vector3.up*2.4f,Vector3.down,out var hit,5,mask,QueryTriggerInteraction.Ignore)||hit.normal.y<.8f)continue;
                var feet=hit.point+Vector3.up*.055f;
                if(Physics.CheckCapsule(feet+Vector3.up*.28f,feet+Vector3.up*1.52f,.28f,mask,QueryTriggerInteraction.Ignore))continue;
                valid[x,z]=true;height[x,z]=feet.y;
            }
            var hide=UnityEngine.Object.FindAnyObjectByType<BeachVillaHideLocations>();
            (int,int) Nearest(Vector3 p,bool[,] grid)
            {
                float best=float.MaxValue;var answer=(-1,-1);
                for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)if(grid[x,z])
                {float d=(new Vector2(-38+x*step,-21+z*step)-new Vector2(p.x,p.z)).sqrMagnitude;if(d<best){best=d;answer=(x,z);}}
                return answer;
            }
            var start=Nearest(hide.poolReturnPoint,valid);var reachable=new bool[nx,nz];var queue=new Queue<(int,int)>();queue.Enqueue(start);reachable[start.Item1,start.Item2]=true;
            int visited=0;while(queue.Count>0)
            {
                var p=queue.Dequeue();visited++;
                foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {
                    int x=p.Item1+d.Item1,z=p.Item2+d.Item2;
                    if(x<0||x>=nx||z<0||z>=nz||!valid[x,z]||reachable[x,z]||Mathf.Abs(height[x,z]-height[p.Item1,p.Item2])>.31f)continue;
                    reachable[x,z]=true;queue.Enqueue((x,z));
                }
            }
            log.AppendLine($"Reachable samples from pool return={visited}; grid=.5m; capsule=height1.8 radius.28; max sampled step=.31; excludes pool water and small-decor player layer.");
            for(int i=0;i<hide.spots.Length;i++)
            {
                var spot=hide.spots[i];var p=Nearest(spot.position,reachable);var a=new Vector3(-38+p.Item1*step,height[p.Item1,p.Item2],-21+p.Item2*step);
                // A 20cm toy must not spawn inside solid furniture. Search a small local pocket with real support.
                if(Physics.CheckSphere(spot.position,.1f,~0,QueryTriggerInteraction.Ignore))
                {
                    bool found=false;
                    for(int ring=1;ring<=6 && !found;ring++)for(int j=0;j<12 && !found;j++)
                    {
                        float angle=j*Mathf.PI/6;var candidate=spot.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*.12f;
                        if(Physics.Raycast(candidate+Vector3.up*.6f,Vector3.down,out var support,1.6f,~0,QueryTriggerInteraction.Ignore))candidate.y=support.point.y+.12f;
                        if(!Physics.CheckSphere(candidate,.1f,~0,QueryTriggerInteraction.Ignore)){spot.position=candidate;found=true;}
                    }
                }
                float horizontal=Vector2.Distance(new Vector2(a.x,a.z),new Vector2(spot.position.x,spot.position.z));
                spot.approach=a;hide.spots[i]=spot;
                log.AppendLine($"HIDE {i+1} {spot.label}: duck={spot.position:F2}; approach={a:F2}; horizontalReach={horizontal:F2}; within2m={horizontal<=2}; clearSphere={ !Physics.CheckSphere(spot.position,.1f,~0,QueryTriggerInteraction.Ignore)}");
            }
            for(int i=0;i<walk.checkpoints.Length;i++)
            {
                if(i>=6){var p=Nearest(walk.checkpoints[i].feet,reachable);var cp=walk.checkpoints[i];cp.feet=new Vector3(-38+p.Item1*step,height[p.Item1,p.Item2],-21+p.Item2*step);walk.checkpoints[i]=cp;}
                log.AppendLine($"CHECKPOINT {i+1} {walk.checkpoints[i].label} feet={walk.checkpoints[i].feet:F3}");
            }
            body.enabled=true;
            File.WriteAllText(Report+"/routes.txt",log.ToString());
            EditorUtility.SetDirty(hide);EditorUtility.SetDirty(walk);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Rubber/Expansion/4 Capture Layout in Play")]
        public static void Capture()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
            var walk=UnityEngine.Object.FindAnyObjectByType<BeachVillaWalkthrough>();walk.StartCoroutine(CaptureRoutine(walk));
        }
        static IEnumerator CaptureRoutine(BeachVillaWalkthrough walk)
        {
            var night=walk.GetComponent<BeachVillaDarkNight>();night.SetNight(false);walk.enabled=false;
            var body=walk.GetComponent<CharacterController>();body.enabled=false;
            var views=new[]{("overview",new Vector3(-52,49,-50),new Vector3(0,0,15)),("west-garden",new Vector3(-20,1.7f,-1),new Vector3(-28,1.3f,7)),("spa",new Vector3(-20,1.7f,21),new Vector3(-28,2,26)),("guest",new Vector3(21,1.7f,16),new Vector3(30,1.3f,19)),("arrival",new Vector3(5,1.7f,44),new Vector3(6,2,15)),("beach",new Vector3(0,1.7f,-20),new Vector3(3,2,5))};
            foreach(var v in views)
            {
                walk.transform.position=v.Item2-Vector3.up*1.65f;walk.transform.rotation=Quaternion.identity;walk.view.rotation=Quaternion.LookRotation(v.Item3-v.Item2);
                yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/"+v.Item1+".png"));yield return new WaitForSecondsRealtime(1);
            }
            var flash=walk.GetComponent<BeachVillaTestFlashlight>();night.SetNight(true);
            foreach(int index in new[]{0,2,6,8})
            {
                walk.GoTo(index);body.enabled=false;
                foreach(bool on in new[]{false,true})
                {
                    flash.SetOn(on);yield return new WaitForSecondsRealtime(3);yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+$"/night-{index+1}-{(on?"flash":"ambient")}.png"));yield return new WaitForSecondsRealtime(1);
                }
            }
            File.WriteAllText(Report+"/runtime-lighting.txt",$"Night ready={night.nightProbesReady}; night atlases={LightmapSettings.lightmaps.Length}; APV={UnityEngine.Rendering.ProbeReferenceVolume.instance.lightingScenario}; renderers={night.nightRenderers.Length}; baked local={night.sceneLights.Count(l=>l&&l.lightmapBakeType==LightmapBakeType.Baked)}; resolution={Screen.width}x{Screen.height}");
            flash.SetOn(false);night.SetNight(false);
            body.enabled=true;walk.enabled=true;walk.GoTo(0);File.WriteAllText(Report+"/capture-complete.txt",DateTime.UtcNow.ToString("O"));
        }
    }
}
