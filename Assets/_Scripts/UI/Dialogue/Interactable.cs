using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class Interactable : MonoBehaviour
{
    [SerializeField] private UnityEvent onInteract;

    public void Interact()
    {
        onInteract.Invoke();
    }
}
