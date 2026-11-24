using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

namespace Rubber.World
{
    // Test-scene lighting only. No baked sunlight or realtime reflection updates.
    public sealed class VillaLightingPreview : MonoBehaviour
    {
        public Light sun;
        public Light[] practicalLights;
        public Material skyMaterial;
        [Range(0, 1)] public float nightBlend;
        public bool animate;
        private float nextUpdate;
        private float phase;
        private Material sourceSky;

        public void Apply(float blend)
        {
            nightBlend = Mathf.Clamp01(blend);
            var twilight = Mathf.Sin(nightBlend * Mathf.PI);
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(48, 165, nightBlend), -35, 0);
            // Reuse the main light as moonlight after dusk, keeping it above the horizon.
            if (nightBlend > .65f)
                sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(12, 42, (nightBlend - .65f) / .35f), 145, 0);
            sun.color = Color.Lerp(new Color(1, .95f, .83f), new Color(.48f, .64f, 1), nightBlend);
            sun.color = Color.Lerp(sun.color, new Color(1, .58f, .30f), twilight * .55f);
            sun.intensity = Mathf.Lerp(1.25f, .12f, nightBlend);
            // Fade at the sun/moon orientation hand-off, avoiding a visible shadow jump.
            sun.intensity *= Mathf.Clamp01(Mathf.Abs(nightBlend - .65f) / .1f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(.46f, .58f, .68f), new Color(.16f, .22f, .34f), nightBlend);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.36f, .36f, .31f), new Color(.14f, .16f, .21f), nightBlend);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(.19f, .17f, .13f), new Color(.075f, .085f, .11f), nightBlend);
            RenderSettings.reflectionIntensity = Mathf.Lerp(.65f, .1f, nightBlend);
            RenderSettings.fogColor = Color.Lerp(new Color(.64f, .76f, .78f), new Color(.035f, .055f, .10f), nightBlend);
            if (skyMaterial != null)
            {
                skyMaterial.SetFloat("_Exposure", Mathf.Lerp(1.15f, .12f, nightBlend));
                skyMaterial.SetColor("_SkyTint", Color.Lerp(new Color(.48f, .58f, .62f), new Color(.18f, .23f, .38f), nightBlend));
            }
            foreach (var light in practicalLights)
            {
                light.enabled = nightBlend > .12f;
                light.intensity = Mathf.SmoothStep(0, 3.5f, nightBlend);
            }
        }

        private void Start()
        {
            sourceSky = skyMaterial;
            skyMaterial = new Material(sourceSky);
            RenderSettings.skybox = skyMaterial;
            Apply(nightBlend);
        }
        private void OnDestroy()
        {
            if (sourceSky == null) return;
            RenderSettings.skybox = sourceSky;
            Destroy(skyMaterial);
            skyMaterial = sourceSky;
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame) { animate = false; Apply(0); }
                if (keyboard.f2Key.wasPressedThisFrame) { animate = false; Apply(.5f); }
                if (keyboard.f3Key.wasPressedThisFrame) { animate = false; Apply(1); }
                if (keyboard.f4Key.wasPressedThisFrame) animate = !animate;
            }
            if (!animate || Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + .1f;
            phase += .1f / 60f;
            Apply((1 - Mathf.Cos(phase * Mathf.PI * 2)) * .5f);
        }
    }
}
