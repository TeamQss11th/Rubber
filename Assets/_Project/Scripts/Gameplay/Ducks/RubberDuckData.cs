using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [CreateAssetMenu(fileName = "RubberDuckData", menuName = "Rubber/Ducks/Rubber Duck Data")]
    public sealed class RubberDuckData : ScriptableObject
    {
        [Header("Identity")]
        [Min(0)]
        [SerializeField] private int id;
        [SerializeField] private string displayName = string.Empty;
        [TextArea(2, 5)]
        [SerializeField] private string description = string.Empty;

        [Header("Presentation")]
        [SerializeField] private Sprite collectionIcon;
        [SerializeField] private AudioClip uniqueSound;

        [Header("Held Pose Offset")]
        [SerializeField] private Vector3 heldPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 heldEulerAngleOffset = Vector3.zero;

        public int Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite CollectionIcon => collectionIcon;
        public AudioClip UniqueSound => uniqueSound;
        public Vector3 HeldPositionOffset => heldPositionOffset;
        public Vector3 HeldEulerAngleOffset => heldEulerAngleOffset;
    }
}
