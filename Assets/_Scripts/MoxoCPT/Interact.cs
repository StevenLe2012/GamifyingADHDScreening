using System;
using System.Collections;
using System.Collections.Generic;
using MoxoCPT;
using UnityEditor;
using UnityEngine;

namespace MoxoCPT
{
    public class Interact : MonoBehaviour
    {
        private Cards _cards;
        [HideInInspector] public Report _report;

        private bool _hasPressed;

        private void Awake()
        {
            _cards = GetComponent<Cards>();
            _report = new Report();
        }

        public void ButtonPressed()  // TODO: Make it for when they press button
        {
            var index = _cards.numSeen - 1;
            
            // HyperReactiveness
            if (_hasPressed)
            {
                _report.HyperReactiveness[index] = true;
                _report.HyperReactiveCount++;
            }
            
            // Attentiveness and Timeliness
            if (IsTargetCard() && !_hasPressed)
            {
                if (_cards.isActive) _report.Timelineess[index] = true;
                _report.Attentiveness[index] = true;
                _hasPressed = true;
            }
            
            // Impulsiveness
            if (!IsTargetCard() && !_hasPressed)
            {
                _report.Impulsiveness[index] = true;
                _hasPressed = true;
            }
        }

        private bool IsTargetCard()
        {
            return _cards.curCard.gameObject.CompareTag("Target");
        }

    }
}

