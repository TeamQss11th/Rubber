// using System;
using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private UnityEvent onInteract;

    public void Interact()
    {
        onInteract.Invoke();
    }
}
