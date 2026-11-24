using UnityEngine;
using UnityEngine.InputSystem;

namespace Rubber.World
{
    // Temporary walkthrough controls. Gameplay can call VillaSlidingDoor directly.
    public sealed class VillaDoorTestControls : MonoBehaviour
    {
        public VillaSlidingDoor[] doors;
        public Transform view;
        void Update()
        {
            var k = Keyboard.current;
            if (!Application.isFocused || k == null || !view || doors == null) return;
            if (k.oKey.wasPressedThisFrame) foreach (var d in doors) if (d) d.Open();
            if (k.pKey.wasPressedThisFrame) foreach (var d in doors) if (d) d.Close();
            if (!k.eKey.wasPressedThisFrame) return;
            VillaSlidingDoor nearest = null; float distance = 3;
            foreach (var d in doors)
            {
                if (!d) continue;
                float next = Vector3.Distance(view.position, d.ClosedCenter);
                if (next < distance) { nearest = d; distance = next; }
            }
            if (nearest) nearest.Toggle();
        }
    }
}
