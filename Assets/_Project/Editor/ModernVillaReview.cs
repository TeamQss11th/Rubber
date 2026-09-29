using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Rubber.EditorTools {
public static class ModernVillaReview {
 [MenuItem("Rubber/Modern Review/Reload Saved and Validate %&v")]
 public static void Reload(){
 if(Application.isPlaying || Lightmapping.isRunning || SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save and exit Play/bake first");
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ModernVillaSetup.ScenePath);
 var n=Object.FindAnyObjectByType<Rubber.World.ModernVillaDayNight>();
 if(!n || !n.nightProbesReady || n.nightMaps.Length!=2 || LightmapSettings.lightmaps.Length!=2)throw new System.InvalidOperationException("Saved lighting missing");
 File.WriteAllLines(ModernVillaSetup.Report+"/dependencies.txt",AssetDatabase.GetDependencies(ModernVillaSetup.ScenePath,true));
 File.WriteAllText(ModernVillaSetup.Report+"/reload.txt","Saved scene reloaded: day maps="+LightmapSettings.lightmaps.Length+"; night maps="+n.nightMaps.Length+"; night APV ready="+n.nightProbesReady+"\n");
 }
 [MenuItem("Rubber/Modern Review/Inspect %&m")]
 public static void Inspect(){
 var s=SceneManager.GetActiveScene(); var b=new StringBuilder("Scene="+s.path+" dirty="+s.isDirty+"\n");
 foreach(var g in s.GetRootGameObjects()) b.AppendLine("ROOT "+g.name+" bounds="+BeachVillaExpansionSurvey.BoundsOf(g));
 foreach(var h in Physics.RaycastAll(new Vector3(-2,5.5f,3.3f),Vector3.down,4))b.AppendLine("HIT "+h.collider.name+" p="+h.point+" normal="+h.normal);
  foreach(float x in new[]{-10f,-8,-6,0,8,10})foreach(float z in new[]{-2f,0,2})foreach(var h in Physics.RaycastAll(new Vector3(x,2.5f,z),Vector3.down,4))b.AppendLine("SPAWN "+h.collider.name+" "+h.point);
 foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.bounds.center.x > -8 && r.bounds.center.x < -3 && r.bounds.center.z > 1 && r.bounds.center.z < 5 && r.bounds.center.y<2))b.AppendLine("LEATHER "+r.name+" gi="+r.receiveGI+" lightmap="+r.lightmapIndex+" mat="+string.Join(",",r.sharedMaterials.Select(m=>m?AssetDatabase.GetAssetPath(m)+":"+m.shader.name+" emission="+(m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor").ToString():"none"):"null")));
 foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))b.AppendLine("CAM "+c.name+" enabled="+c.enabled+" pos="+c.transform.position);
 foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))b.AppendLine("LIGHT "+l.name+" type="+l.type+" bake="+l.lightmapBakeType+" power="+l.intensity+" enabled="+l.enabled);
 b.AppendLine("Settings="+EditorJsonUtility.ToJson(Lightmapping.lightingSettings,true));
 b.AppendLine("Pipeline="+EditorJsonUtility.ToJson(QualitySettings.renderPipeline,true));
 Directory.CreateDirectory("Docs/ModernVillaDayNight"); File.WriteAllText("Docs/ModernVillaDayNight/current.txt",b.ToString());
 }
}}




