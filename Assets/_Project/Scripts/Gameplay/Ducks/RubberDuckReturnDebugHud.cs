using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [DisallowMultipleComponent]
    public sealed class RubberDuckReturnDebugHud : MonoBehaviour
    {
        [SerializeField] private RubberDuckReturnRegistry registry;
        [SerializeField, Min(1)] private int totalDuckCount = 5;

        private GUIStyle labelStyle;

        public int TotalDuckCount => totalDuckCount;

        public void Configure(RubberDuckReturnRegistry returnRegistry, int totalCount)
        {
            registry = returnRegistry;
            totalDuckCount = Mathf.Max(1, totalCount);
        }

        private void Awake()
        {
            if (!registry)
                Debug.LogError("RubberDuckReturnDebugHud requires RubberDuckReturnRegistry.", this);
        }

        private void OnGUI()
        {
            if (!registry) return;

            labelStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            GUI.Box(new Rect(20f, 20f, 250f, 48f),
                $"Ducks Returned: {registry.ReturnedCount} / {totalDuckCount}", labelStyle);
        }
    }
}
