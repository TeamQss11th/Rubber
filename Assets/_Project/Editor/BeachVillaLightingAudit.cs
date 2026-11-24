using System.IO;
using System.Linq;
using System.Text;
using Rubber.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rubber.EditorTools
{
    public static class BeachVillaLightingAudit
    {
        [MenuItem("Rubber/Expansion/8 Validate Lighting Bindings")]
        public static void Run()
        {
            if(Application.isPlaying)throw new System.InvalidOperationException("Exit play first.");
            // Unity's LightingData keeps native texture references after a scene reload. Restore the
            // default generated filenames from the preserved day export; night has independent copies.
            foreach(string source in Directory.GetFiles("Assets/_Project/Lighting/BeachVillaExpanded/Day"))
            {
                string name=Path.GetFileName(source);
                if(!name.StartsWith("Lightmap-") || name.EndsWith(".meta"))continue;
                string target="Assets/Modern Villa/Scenes/Beach Villa/"+name;
                File.Copy(source,target,true);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceUpdate);
            }
            var dayData=AssetDatabase.LoadAssetAtPath<LightingDataAsset>("Assets/_Project/Lighting/BeachVillaExpanded/Day/LightingData.asset");
            Lightmapping.lightingDataAsset=dayData;
            var night=Object.FindAnyObjectByType<BeachVillaDarkNight>();var log=new StringBuilder();int different=0,uv=0;
            log.AppendLine("LightingData="+AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset));
            foreach(var b in night.nightRenderers)if(b.renderer)
            {
                if(b.renderer.lightmapIndex!=b.index)different++;
                if((b.renderer.lightmapScaleOffset-b.scaleOffset).sqrMagnitude>1e-8f)
                {
                    uv++;log.AppendLine($"DIFF {BeachVillaExpansionSurvey.PathOf(b.renderer.transform)} day={b.renderer.lightmapIndex}/{b.renderer.lightmapScaleOffset} night={b.index}/{b.scaleOffset}");
                    // Only differing UV layouts must be excluded from static batching. Other renderers retain batching.
                    var flags=GameObjectUtility.GetStaticEditorFlags(b.renderer.gameObject);GameObjectUtility.SetStaticEditorFlags(b.renderer.gameObject,flags&~StaticEditorFlags.BatchingStatic);
                }
            }
            log.Insert(0,$"Different indices={different}; different UV layouts={uv}; affected renderers removed from static batching.\n");
            foreach(var m in LightmapSettings.lightmaps)log.AppendLine("DAY "+AssetDatabase.GetAssetPath(m.lightmapColor));
            foreach(var m in night.nightMaps)log.AppendLine("NIGHT "+AssetDatabase.GetAssetPath(m.color));
            foreach(var r in GameObject.Find("Guest wing - vendor BuildingC").GetComponentsInChildren<MeshRenderer>())
            {if(r.bounds.center.x<26.4f && r.bounds.size.y>1.8f)log.AppendLine($"WALL {r.name}: index={r.lightmapIndex}; uv={r.lightmapScaleOffset}; mat={string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null"))}");}
            File.WriteAllText("Docs/BeachVillaExpansion/lighting-bindings.txt",log.ToString());EditorSceneManager.MarkSceneDirty(night.gameObject.scene);EditorSceneManager.SaveScene(night.gameObject.scene);
            BeachVillaMovementChecks.Run();
        }
    }
}
