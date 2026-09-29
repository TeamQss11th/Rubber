using UnityEngine;

namespace Rubber.World
{
    // Keeps the expanded estate's decorative lights local to the player.
    [DefaultExecutionOrder(100)]
    public sealed class VillaLightBudget : MonoBehaviour
    {
        public VillaLightingPreview lighting;
        public Transform viewer;
        public Light[] localLights;
        public int maxActive = 8;
        public int maxShadowed = 2;
        public float activeDistance = 22;
        private float nextTick;
        private bool[] picked;
        private void Awake() { picked = new bool[localLights.Length]; }
        private void LateUpdate()
        {
            if (Time.unscaledTime < nextTick || viewer == null) return;
            nextTick = Time.unscaledTime + .2f;
            Evaluate(viewer.position);
        }
        public void Evaluate(Vector3 position)
        {
            if (picked == null || picked.Length != localLights.Length) picked = new bool[localLights.Length];
            for (int i = 0; i < localLights.Length; i++)
            {
                picked[i] = false;
                localLights[i].enabled = false;
                localLights[i].shadows = LightShadows.None;
            }
            if (lighting.nightBlend <= .12f) return;
            int shadowed = 0;
            for (int slot = 0; slot < maxActive; slot++)
            {
                int nearest = -1;
                float best = activeDistance * activeDistance;
                for (int i = 0; i < localLights.Length; i++)
                {
                    if (picked[i]) continue;
                    float distance = (localLights[i].transform.position - position).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = i; }
                }
                if (nearest < 0) break;
                picked[nearest] = true;
                var light = localLights[nearest];
                light.enabled = true;
                float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(activeDistance - 5, activeDistance, Mathf.Sqrt(best)));
                light.intensity = Mathf.SmoothStep(0, 4, lighting.nightBlend) * fade;
                if (light.type == LightType.Spot && shadowed < maxShadowed)
                { light.shadows = LightShadows.Soft; shadowed++; }
            }
        }
    }
}
