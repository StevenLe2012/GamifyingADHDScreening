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
        private Cards _card;
        private Report _report;

        private void OnMouseDown()
        {
            // check for what state we are in (whether curCard is active or not and when they click mouse down
            if (_card.curCard.gameObject.CompareTag("Target")) 
            {
                
            }

        }

    }
}

