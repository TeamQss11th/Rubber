using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rubber.EditorTools {
public static class ModernVillaPlayChecks {
 [MenuItem("Rubber/Modern Review/Test In Play %&t")]
 public static void Test(){if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first");var n=UnityEngine.Object.FindAnyObjectByType<ModernVillaDayNight>();n.StartCoroutine(Run(n));}
 static IEnumerator Run(ModernVillaDayNight n){
 var w=n.GetComponent<ModernVillaWalkthrough>();var b=w.GetComponent<CharacterController>();var f=n.GetComponent<BeachVillaTestFlashlight>();var log=new StringBuilder();w.enabled=false;
 try{
 for(int i=0;i<w.checkpoints.Length;i++){w.GoTo(i);Physics.SyncTransforms();for(int k=0;k<60;k++)w.StepMovement(Vector2.zero,0,false,1f/60);log.AppendLine(w.checkpoints[i].label+" grounded="+b.isGrounded+" feet="+w.transform.position);if(!b.isGrounded)throw new InvalidOperationException("Grounding failed");}
 w.GoTo(0);Physics.SyncTransforms();for(int k=0;k<60;k++)w.StepMovement(Vector2.zero,0,false,1f/60);float start=w.transform.position.y,apex=start;w.StepMovement(Vector2.zero,0,true,1f/60);bool second=w.TryJump();for(int k=0;k<150;k++){w.StepMovement(Vector2.zero,0,false,1f/60);apex=Mathf.Max(apex,w.transform.position.y);}log.AppendLine($"Jump height={apex-start:F3}m; airJumpAllowed={second}; landed={b.isGrounded}; landing drift={w.transform.position.y-start:F3}m");if(apex-start<.8f || second || !b.isGrounded)throw new InvalidOperationException("Jump test failed");
 var startPos=w.transform.position;for(int k=0;k<30;k++)w.StepMovement(Vector2.up,2.6f,false,1f/60);float distance=Vector3.Distance(startPos,w.transform.position);log.AppendLine("Walking distance="+distance);if(distance<1.2f)throw new InvalidOperationException("Start route blocked");
 foreach(int point in new[]{0,1,4}){
 w.GoTo(point);w.view.localRotation=Quaternion.Euler(8,0,0);
 foreach(var mode in new[]{"day","night","flashlight"}){n.SetNight(mode!="day");f.SetOn(mode=="flashlight");yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(ModernVillaSetup.Report+"/"+point+"-"+mode+".png"));log.AppendLine($"{point}-{mode}: scenario={ProbeReferenceVolume.instance.lightingScenario}; lightmaps={LightmapSettings.lightmaps.Length}; flash={f.Beam.enabled}; resolution={Screen.width}x{Screen.height}; cameras={UnityEngine.Object.FindObjectsByType<Camera>().Count(c=>c.isActiveAndEnabled)}");yield return new WaitForSecondsRealtime(1);}
 }
  log.AppendLine("Editor frame sampling; includes editor overhead; no baseline or standalone 60 FPS claim. GPU="+SystemInfo.graphicsDeviceName+" CPU="+SystemInfo.processorType);
 foreach(int point in new[]{0,1,4})foreach(bool night in new[]{false,true}){w.GoTo(point);n.SetNight(night);f.SetOn(night);yield return new WaitForSecondsRealtime(2);var timings=new System.Collections.Generic.List<double>();double previous=Time.realtimeSinceStartupAsDouble;for(int k=0;k<180;k++){yield return null;double now=Time.realtimeSinceStartupAsDouble;timings.Add((now-previous)*1000);previous=now;}timings.Sort();log.AppendLine($"EDITOR {point} night={night} flashlight={night}: median={timings[90]:F2}ms p95={timings[171]:F2}ms mean={timings.Average():F2}ms");}
 log.AppendLine("PASS. Normal Game View captures. Not a standalone FPS benchmark.");
 }finally{File.WriteAllText(ModernVillaSetup.Report+"/playchecks.txt",log.ToString());f.SetOn(false);n.SetNight(false);w.GoTo(0);w.enabled=true;}
 }
}}

