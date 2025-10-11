using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[Serializable]
public class Interactor : MonoBehaviour
{
    // public InputActionReference toggleReference = null;
    [SerializeField] private InputActionReference controllerInput;
    [SerializeField] private float interactRadius; //floatreference
    //[SerializeField] private inputConfig interactInput;

    private Collider[] _collidersInRange;
    private List<Interactable> _interactablesInRange;
    private Interactable _closestInteractable;

    private void Start()
    {
        _interactablesInRange = new List<Interactable>();
    }

    //Hami: Switch to gamepad stick
    //private void FixedUpdate()
    private void Update()
    {
        //if (controllerInput.action.ReadValue<float>() > 0)
        //if (Input.GetKeyDown(KeyCode.Space))

        UpdateInteractables(); 
        // A (buttonSouth) OR Space (kept for easy switch-back)
        if ( (UnityEngine.InputSystem.Gamepad.current != null 
                && UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame) 
                || Input.GetKeyDown(KeyCode.Space) )
        {
            
            Interact();
            Debug.Log("Interacting!");
        }
    }

    // private void Interact()
    // {
    //     if(_closestInteractable != null)
    //     {
    //         //Debug.Log(_closestInteractable.gameObject.name);
    //         _closestInteractable.Interact();
    //     }

    // }
    //Hami: Update Interact

    private void Interact()
{
    if (_closestInteractable == null) return;

    // Try dialogue first (cleanest route)
    var handler = _closestInteractable.GetComponentInParent<Dialogue.TestHandler>();
    var dlgState = _closestInteractable.GetComponentInParent<Dialogue.DialogueState>();

    if (handler != null && dlgState != null)
    {
        handler.TryStartConversationFromState(dlgState);
        return;
    }

    // Fallback to the legacy interactable behavior
    _closestInteractable.Interact();
}


    //Hami: Change end



    private void OnTriggerEnter(Collider other)
    {
        UpdateInteractables();
    }

    private void OnTriggerExit(Collider other)
    {
        UpdateInteractables();
    }

    private void UpdateInteractables()
    {
        _collidersInRange = Physics.OverlapSphere(transform.position, interactRadius); //interactradius.Value
        _interactablesInRange.Clear();
        if (_collidersInRange == null || _collidersInRange.Length == 0) return;
        foreach (var colliderInRange in _collidersInRange)
        {
            //Debug.Log(colliderInRange);
            var interactable = colliderInRange.GetComponent<Interactable>();
            if (interactable != null)
            {
                _interactablesInRange.Add(interactable);
            }
        }

        _closestInteractable = GetClosestInteractable();
    }

    private Interactable GetClosestInteractable()
    {
        
        if (_interactablesInRange.Count <= 0)
        {
            return null;
        }
            
        int closestInteractableIndex = 0;
        var closestDistance = Mathf.Infinity;
        for (int i = 0; i < _interactablesInRange.Count; i++)
        {
            var distance = Vector3.Distance(_interactablesInRange[i].transform.position, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractableIndex = i;
            }
        }
        return _interactablesInRange[closestInteractableIndex];
    }
}


