using UnityEngine;
using UnityEngine.InputSystem;

namespace MoxoCPT
{
    public class ButtonPress : MonoBehaviour
    {
        [SerializeField] private InputActionReference controllerInput;
        
        public void Update()
        {
            if (GameManager.Instance.State == GameManager.GameState.CPT && 
                controllerInput.action.triggered)
            {
                Interact._buttonPressed = true;
            }
            
            //TODO: Make dedicated way to turn on game
            if (GameManager.Instance.State == GameManager.GameState.PrepareCPT &&
                controllerInput.action.triggered)
            {
                MoxoCPTManager.Instance.OnGameBegin();
            }
        }
    }

}
