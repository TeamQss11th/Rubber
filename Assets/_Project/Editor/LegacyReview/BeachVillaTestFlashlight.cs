using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Rubber.World
{
    // Runtime-only test light: never contributes to an editor bake.
    [DisallowMultipleComponent]
    public sealed class BeachVillaTestFlashlight : MonoBehaviour
    {
        public Transform view;
        public Light Beam { get; private set; }
        void OnEnable()
        {
            if (!Application.isPlaying || !view) return;
            var lamp = new GameObject("Test flashlight (runtime)");
            lamp.transform.SetParent(view, false);
            lamp.transform.localPosition = new Vector3(.12f, -.10f, .05f);
            Beam = lamp.AddComponent<Light>();
            Beam.type = LightType.Spot;
            // Runtime-created lights are realtime; lightmapBakeType is editor-only.
            Beam.color = new Color(1, .94f, .82f);
            Beam.intensity = 8;
            Beam.range = 15;
            Beam.spotAngle = 48;
            Beam.innerSpotAngle = 28;
            Beam.shadows = LightShadows.Hard;
            Beam.shadowCustomResolution = 512;
            Beam.shadowNearPlane = .05f;
            Beam.shadowBias = .02f;
            Beam.shadowNormalBias = .1f;
            var data = lamp.AddComponent<UniversalAdditionalLightData>();
            data.usePipelineSettings = false;
            // URP exposes this serialized setting as a read-only property.
            JsonUtility.FromJsonOverwrite("{\"m_AdditionalLightsShadowResolutionTier\":1}", data);
            Beam.enabled = false;
        }
        public void SetOn(bool on) { if (Beam) Beam.enabled = on; }
        void Update()
        {
            if (Application.isFocused && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && Beam)
                SetOn(!Beam.enabled);
        }
        void OnDisable() { if (Beam) Destroy(Beam.gameObject); }
    }
}
