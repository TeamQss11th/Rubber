using UnityEngine;

namespace Rubber.Gameplay.Player
{
    public sealed class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField] private Light beam;
        public bool IsOn => beam && beam.enabled;
        public void Configure(Light light) { beam = light; beam.enabled = false; }
        private void Awake() { if (beam) beam.enabled = false; }
        public void Toggle() { if (isActiveAndEnabled && beam) beam.enabled = !beam.enabled; }
        private void OnDisable() { if (beam) beam.enabled = false; }
    }
}
