using UnityEngine;

namespace Rubber.Gameplay.Interaction
{
    /// <summary>Temporary interaction target used only by PlayerTestScene.</summary>
    [DisallowMultipleComponent]
    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock propertyBlock;

        public int InteractionCount { get; private set; }
        public GameObject LastInteractor { get; private set; }

        public bool TryInteract(GameObject interactor)
        {
            if (!interactor) return false;

            InteractionCount++;
            LastInteractor = interactor;
            Renderer targetRenderer = GetComponent<Renderer>();
            if (targetRenderer)
            {
                propertyBlock ??= new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColor,
                    InteractionCount % 2 == 1 ? Color.cyan : Color.yellow);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            Debug.Log($"{name} interacted with by {interactor.name} ({InteractionCount}).", this);
            return true;
        }
    }
}
