/*
 * This script allows the user to press the trigger on the controller
 * to activate the dialogue button that they are currently looking at.
 */
using UnityEngine;
using Tobii.G2OM;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ChooseButtonFromGaze : MonoBehaviour, IGazeFocusable
{
    public InputActionReference controllerInput = null;
    
    [Range(0, 1)]
    [SerializeField] private float _triggerAmountNeeded = 0.75f;
    private float _curTrigger;
    private Button _button;
    private delegate void TriggerHandler();
    private event TriggerHandler Triggered;
    
    public void GazeFocusChanged(bool hasFocus)
    {
        if (hasFocus) Triggered += ButtonActive;
        else Triggered -= ButtonActive;
    }

    private void Start()
    {
        _button = GetComponent<Button>();
    }

    private void Update()
    {
        _curTrigger = controllerInput.action.ReadValue<float>();
        if (_curTrigger >= _triggerAmountNeeded)
        {
            Triggered?.Invoke();
        }
    }

    private void ButtonActive() => _button.onClick.Invoke();
}
