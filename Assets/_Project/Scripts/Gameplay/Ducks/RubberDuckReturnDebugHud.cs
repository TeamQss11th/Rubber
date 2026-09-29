using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [DisallowMultipleComponent]
    public sealed class RubberDuckReturnDebugHud : MonoBehaviour
    {
        [SerializeField] private RubberDuckReturnRegistry registry;

        private GUIStyle labelStyle;
        private DuckCollectionProgress displayedProgress;

        public int TotalDuckCount => displayedProgress.TotalCount;
        public DuckCollectionProgress DisplayedProgress => displayedProgress;

        public void Configure(RubberDuckReturnRegistry returnRegistry)
        {
            Unsubscribe();
            registry = returnRegistry;
            Subscribe();
            Refresh();
        }

        private void Awake()
        {
            if (!registry)
                Debug.LogError("RubberDuckReturnDebugHud requires RubberDuckReturnRegistry.", this);
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start() => Refresh();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (!registry) return;
            registry.ProgressChanged -= HandleProgressChanged;
            registry.ProgressChanged += HandleProgressChanged;
        }

        private void Unsubscribe()
        {
            if (registry)
                registry.ProgressChanged -= HandleProgressChanged;
        }

        private void Refresh()
        {
            if (registry)
                displayedProgress = registry.CurrentProgress;
        }

        private void HandleProgressChanged(DuckCollectionProgress progress) =>
            displayedProgress = progress;

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
                $"Ducks Returned: {displayedProgress.ReturnedCount} / {displayedProgress.TotalCount}",
                labelStyle);
        }
    }
}
