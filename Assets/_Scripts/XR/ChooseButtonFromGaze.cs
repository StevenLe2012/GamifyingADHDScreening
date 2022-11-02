/*
 * This script allows the user to press the trigger on the controller
 * to activate the dialogue button that they are currently looking at.
 */

using System;
using System.Threading;
using UnityEngine;
using Tobii.G2OM;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ChooseButtonFromGaze : MonoBehaviour, IGazeFocusable
{
    [SerializeField] private InputActionReference controllerInput = null;
    [Range(0, 1)]
    [SerializeField] private float _triggerAmountNeeded = 0.75f;
    [SerializeField] private float _holdDur = 2f;
    [SerializeField] private Color _targetColor;
    private Color _baseColor;
    
    private float _curTrigger;
    private float _timer;
    private Button _button;
    private Image _image;
    
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
        _image = GetComponent<Image>();
        _baseColor = _image.color;
        _timer = 0f;
    }

    private void Update()
    {
        _curTrigger = controllerInput.action.ReadValue<float>();
        if (Triggered != null)
        {
            if (_curTrigger >= _triggerAmountNeeded)
            {
                _timer += Time.deltaTime;
                print(_timer);
                _image.color = Color.Lerp(_image.color, _targetColor, Time.deltaTime * (_timer / _holdDur));;
                if (_timer >= _holdDur)
                {
                    _timer = float.NegativeInfinity;
                    Triggered.Invoke();
                    Reset();
                }
            }
            else
            {
                Reset();
            }
        }
       
    }

    private void ButtonActive() => _button.onClick.Invoke();

    private void Reset()
    {
        _timer = 0f;
        _image.color = _baseColor;
    }
}
