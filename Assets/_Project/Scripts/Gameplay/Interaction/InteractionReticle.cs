using UnityEngine;
using UnityEngine.UI;

namespace Rubber.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class InteractionReticle : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float size = 6f;
        [SerializeField] private Color color = Color.white;

        private void Awake()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var dot = new GameObject("Interaction Center Dot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            dot.transform.SetParent(transform, false);
            var rect = (RectTransform)dot.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * size;
            var image = dot.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
    }
}
