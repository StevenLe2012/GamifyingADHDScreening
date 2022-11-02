// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved
/*
* I modified the script greatly to have it work for Buttons rather than GameObjects
*/

using Tobii.G2OM;
using UnityEngine;
using UnityEngine.UI;

//Monobehaviour which implements the "IGazeFocusable" interface, meaning it will be called on when the object receives focus
public class HighlightAtGazeModified : MonoBehaviour, IGazeFocusable
{
    public Color highlightColor;
    public float animationTime = 0.1f;

    private Button _button;
    private Color _originalColor;
    private Color _targetColor;
    private ColorBlock _colorBlock;

    //The method of the "IGazeFocusable" interface, which will be called when this object receives or loses focus
    public void GazeFocusChanged(bool hasFocus)
    {
        //If this object received focus, fade the object's color to highlight color
        if (hasFocus)
        {
            print("hasFocus");
            _targetColor = highlightColor;
        }
        //If this object lost focus, fade the object's color to it's original color
        else
        {
            print("noFocus");
            _targetColor = _originalColor;
        }
    }

    private void Start()
    {
        _button = GetComponent<Button>();
        _colorBlock = _button.colors;
        _originalColor = _button.colors.normalColor;
        _targetColor = _originalColor;
    }

    private void Update()
    {
        _colorBlock.normalColor = Color.Lerp(_button.colors.normalColor, _targetColor, Time.deltaTime * (1 / animationTime));
        _button.colors = _colorBlock;
    }
}