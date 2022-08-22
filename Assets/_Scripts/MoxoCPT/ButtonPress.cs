using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    public class ButtonPress : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Interact._buttonPressed = true;
            }
            
            //TODO: Make dedicated way to turn on game
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                MoxoCPTManager.Instance.OnGameBegin();
            }
        }
    }

}
