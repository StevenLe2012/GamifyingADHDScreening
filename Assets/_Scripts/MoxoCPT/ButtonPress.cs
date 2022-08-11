using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    public class ButtonPress : MonoBehaviour
    {
        public void Pressed()
        {
            Interact._buttonPressed = true;
            Debug.Log("button was PRESSED");
        }
    }

}
