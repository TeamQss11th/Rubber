using UnityEngine;

namespace Rubber.Gameplay.Interaction
{
    public static class InteractionOutlineSelection
    {
        private static Renderer[] selectedRenderers = System.Array.Empty<Renderer>();

        public static Renderer[] SelectedRenderers => selectedRenderers;
        public static bool HasSelection => selectedRenderers.Length > 0;

        public static void SetTarget(Component target)
        {
            selectedRenderers = target
                ? target.GetComponentsInChildren<Renderer>(true)
                : System.Array.Empty<Renderer>();
        }

        public static void Clear() => selectedRenderers = System.Array.Empty<Renderer>();
    }
}
