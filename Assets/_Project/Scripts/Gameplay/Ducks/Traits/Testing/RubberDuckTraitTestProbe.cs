using UnityEngine;

namespace Rubber.Gameplay.Ducks.Traits
{
    [DisallowMultipleComponent]
    public sealed class RubberDuckTraitTestProbe : MonoBehaviour
    {
        public int InitializationCount { get; private set; }
        public int InteractionCount { get; private set; }
        public int IntervalCount { get; private set; }
        public GameObject LastInteractor { get; private set; }

        internal void RecordInitialization()
        {
            InitializationCount++;
        }

        internal void RecordInteraction(GameObject interactor)
        {
            InteractionCount++;
            LastInteractor = interactor;
        }

        internal void RecordInterval()
        {
            IntervalCount++;
        }
    }
}
