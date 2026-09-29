using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer = ~0;

    private InteractableObject currentTarget;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || interactionUI == null)
        {
            Debug.LogError("PlayerInteraction의 Camera 또는 Interaction UI가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        interactionUI.SetActive(false);
    }

    private void Update()
    {
        FindInteractionTarget();

        if (currentTarget != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            currentTarget.Interact();
    }

    private void FindInteractionTarget()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayer))
            currentTarget = hit.collider.GetComponentInParent<InteractableObject>();
        else
            currentTarget = null;

        interactionUI.SetActive(currentTarget != null);
    }
}
