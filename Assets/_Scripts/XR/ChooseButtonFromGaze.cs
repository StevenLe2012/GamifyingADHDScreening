using System;
using UnityEngine;
using Tobii.G2OM;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class ChooseButtonFromGaze : MonoBehaviour, IGazeFocusable
{
    //public ActionBasedController Controller;
    public InputActionReference controllerInput = null;

    [Range(0, 1)]
    [SerializeField] private float _triggerAmountNeeded = 0.75f;
    private float _curTrigger;
    private bool _triggerPressed;
    private Button _button;
    
    public void GazeFocusChanged(bool hasFocus)
    {
        //If this object received focus, fade the object's color to highlight color
        if (hasFocus)
        {
            print(_curTrigger);
            print("found focus");
            // TODO: Make it work for when you press trigger, it selects the button, but first make sure it can detect you pressing trigger.
            if (_curTrigger > _triggerAmountNeeded)
            {
                print("pressed trigger");
                _button.onClick.Invoke();
            }
        }
        //If this object lost focus, fade the object's color to it's original color
        else
        {
            print("lost focus");
        }
    }

    private void Start()
    {
        _button = GetComponent<Button>();
    }

    private void Update()
    {
        _curTrigger = controllerInput.action.ReadValue<float>();
    }
}
