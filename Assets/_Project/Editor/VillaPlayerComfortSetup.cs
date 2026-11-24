using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using Rubber.Gameplay.Player;
using UnityEngine.Rendering.Universal;

namespace Rubber.EditorTools
{
    public static class VillaPlayerComfortSetup
    {
        public static void Install()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            EditorSceneManager.SaveOpenScenes();var scene=EditorSceneManager.OpenScene(ModernVillaSetup.ScenePath);
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            var camera=player.GetComponentInChildren<Camera>();
            var flashlight=player.GetComponent<PlayerFlashlight>();if(!flashlight)flashlight=player.gameObject.AddComponent<PlayerFlashlight>();
            var child=camera.transform.Find("Player Flashlight");
            if(!child){child=new GameObject("Player Flashlight",typeof(Light)).transform;child.SetParent(camera.transform,false);}
            child.localPosition=new Vector3(.12f,-.12f,.12f);child.localRotation=Quaternion.identity;
            var beam=child.GetComponent<Light>();beam.type=LightType.Spot;beam.lightmapBakeType=LightmapBakeType.Realtime;
            beam.range=18;beam.spotAngle=55;beam.innerSpotAngle=30;beam.intensity=8;beam.color=new Color(1,.96f,.88f);
            beam.shadows=LightShadows.Soft;beam.shadowCustomResolution=512;beam.shadowBias=.025f;beam.shadowNormalBias=.15f;
            var lightData=beam.GetUniversalAdditionalLightData();lightData.usePipelineSettings=false;
            var serializedLight=new SerializedObject(lightData);
            serializedLight.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            serializedLight.ApplyModifiedPropertiesWithoutUndo();
            beam.cullingMask=~((1<<2)|(1<<5));flashlight.Configure(beam);
            var text=GameObject.Find("Game HUD").transform.Find("Controls").GetComponent<TMP_Text>();
            text.text="WASD  Move    SPACE  Jump    CLICK  Interact    G  Drop    F  Flashlight    ESC  Pause";
            text.rectTransform.sizeDelta=new Vector2(1500,40);text.fontSize=21;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Docs/PlayerComfort");
            File.WriteAllLines("Docs/PlayerComfort/stairs.txt",UnityEngine.Object.FindObjectsByType<Collider>().Where(c=>c.name.ToLowerInvariant().Contains("stair")).Select(c=>$"{ModernVillaSurfaceAudit.PathOf(c.transform)} bounds={c.bounds}"));
        }
    }
}
