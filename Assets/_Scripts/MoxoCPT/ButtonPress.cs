// using UnityEngine;
// using UnityEngine.InputSystem;

// namespace MoxoCPT
// {
//     public class ButtonPress : MonoBehaviour
//     {
//         // Optional: bind this in the Inspector to <Gamepad>/buttonSouth and (optionally) <Keyboard>/space
//         [SerializeField] private InputActionReference actionPress;

//         private void OnEnable()
//         {
//             if (actionPress != null) actionPress.action.Enable();
//         }

//         private void OnDisable()
//         {
//             if (actionPress != null) actionPress.action.Disable();
//         }

//         private void Update()
//         {
//             // 1) Read input: prefer InputAction if assigned; otherwise poll gamepad/keyboard directly
//             bool pressed = false;

//             if (actionPress != null)
//             {
//                 // Use "WasPressedThisFrame" so holds don’t retrigger
//                 pressed = actionPress.action.WasPressedThisFrame();
//             }
//             else
//             {
//                 var gp = Gamepad.current;
//                 var kb = Keyboard.current;

//                 if (gp != null && gp.buttonSouth.wasPressedThisFrame) pressed = true; // A
//                 else if (kb != null && kb.spaceKey.wasPressedThisFrame) pressed = true; // Space fallback
//             }

//             if (!pressed) return;

//             // 2) Route the press by game state
//             var state = GameManager.Instance.State;

//             if (state == GameManager.GameState.CPT)
//             {
//                 // This is consumed by Interact.StartReport(...)
//                 Interact._buttonPressed = true;
//             }
//             else if (state == GameManager.GameState.PrepareCPT)
//             {
//                 // If you still want A/Space to start the test from a "ready" state
//                 MoxoCPTManager.Instance.OnGameBegin();
//             }
//         }
//     }
// }

//Hami: Don't let game star immediately

// using UnityEngine;
// using UnityEngine.InputSystem;

// namespace MoxoCPT
// {
//     public class ButtonPress : MonoBehaviour
//     {
//         // Optional: bind this in the Inspector to <Gamepad>/buttonSouth and (optionally) <Keyboard>/space
//         [SerializeField] private InputActionReference actionPress;

//         private void OnEnable()
//         {
//             if (actionPress != null) actionPress.action.Enable();
//         }

//         private void OnDisable()
//         {
//             if (actionPress != null) actionPress.action.Disable();
//         }

//         private void Update()
//         {
//             // 1) Read input: prefer InputAction if assigned; otherwise poll gamepad/keyboard directly
//             bool pressed = false;

//             if (actionPress != null)
//             {
//                 pressed = actionPress.action.WasPressedThisFrame();
//             }
//             else
//             {
//                 var gp = Gamepad.current;
//                 var kb = Keyboard.current;

//                 if (gp != null && gp.buttonSouth.wasPressedThisFrame) pressed = true;   // A
//                 else if (kb != null && kb.spaceKey.wasPressedThisFrame) pressed = true; // Space fallback
//             }

//             if (!pressed) return;

//             // --- ROUTE THE PRESS BY GAME STATE ---

//             // During CPT: this is a response press used by Interact.StartReport(...)
//             if (GameManager.Instance.State == GameManager.GameState.CPT)
//             {
//                 Interact._buttonPressed = true;
//                 return;
//             }

//             // ❌ IMPORTANT CHANGE:
//             // Do NOT auto-start the game from PrepareCPT here anymore.
//             // The MOXO Intro UI's Start button should be the only place that calls OnGameBegin().
//             //
//             // Old (remove/comment this block):
//             //
//             // if (GameManager.Instance.State == GameManager.GameState.PrepareCPT)
//             // {
//             //     MoxoCPTManager.Instance.OnGameBegin();
//             // }
//         }
//     }
// }


// ButtonPress.cs
// Requires Unity Input System package.
// Attach to any GameObject; optionally bind "actionPress" in the Inspector to <Gamepad>/buttonSouth and/or <Keyboard>/space

// ButtonPress.cs
// Requires the Input System package. Set Project Settings → Player → Active Input Handling = "Input System Package" (or "Both").

// using UnityEngine;
// using UnityEngine.InputSystem; // Gamepad, Keyboard, InputActionReference

// namespace MoxoCPT
// {
//     [AddComponentMenu("MoxoCPT/Button Press")]
//     public class ButtonPress : MonoBehaviour
//     {
//         // Optional: bind this in the Inspector to <Gamepad>/buttonSouth and/or <Keyboard>/space
//         [SerializeField] private InputActionReference actionPress;

//         private void OnEnable()
//         {
//             if (actionPress != null && actionPress.action != null)
//             {
//                 actionPress.action.Enable();
//             }
//         }

//         private void OnDisable()
//         {
//             if (actionPress != null && actionPress.action != null)
//             {
//                 actionPress.action.Disable();
//             }
//         }

//         private void Update()
//         {
//             bool pressed = false;

//             // 1) InputAction path (if assigned & enabled)
//             if (actionPress != null && actionPress.action != null)
//             {
//                 if (!actionPress.action.enabled)
//                     actionPress.action.Enable();

//                 if (actionPress.action.WasPressedThisFrame())
//                     pressed = true;
//             }

//             // 2) Raw gamepad/keyboard backup (ALWAYS check too)
//             var gp = Gamepad.current;
//             var kb = Keyboard.current;

//             if (gp != null && gp.buttonSouth.wasPressedThisFrame) pressed = true; // Gamepad South (A on Xbox)
//             if (kb != null && kb.spaceKey.wasPressedThisFrame)     pressed = true; // Space

//             if (!pressed) return;

//             // --- ROUTE THE PRESS BY GAME STATE ---
//             var state = GameManager.Instance.State;
//             Debug.Log($"[ButtonPress] Press detected in state={state}");

//             if (state == GameManager.GameState.CPT)
//             {
//                 // Consumed by Interact.StartReport(...)
//                 Interact._buttonPressed = true;
//                 return;
//             }

//             // keep this disabled so IntroScreen Start button is the only “begin”
//             // if (state == GameManager.GameState.PrepareCPT)
//             // {
//             //     MoxoCPTManager.Instance.OnGameBegin();
//             // }
//         }
//     }
// }


//Hami: eyetracking

using UnityEngine;
using UnityEngine.InputSystem; // Gamepad, Keyboard, InputActionReference

namespace MoxoCPT
{
    [AddComponentMenu("MoxoCPT/Button Press")]
    public class ButtonPress : MonoBehaviour
    {
        // Optional: bind this in the Inspector to <Gamepad>/buttonSouth and/or <Keyboard>/space
        [SerializeField] private InputActionReference actionPress;

        private void OnEnable()
        {
            if (actionPress != null && actionPress.action != null)
            {
                actionPress.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (actionPress != null && actionPress.action != null)
            {
                actionPress.action.Disable();
            }
        }

        private void Update()
        {
            bool pressed = false;
            char pressedKey = 'P'; // default “Press”

            // 1) InputAction path (if assigned & enabled)
            if (actionPress != null && actionPress.action != null)
            {
                if (!actionPress.action.enabled)
                    actionPress.action.Enable();

                if (actionPress.action.WasPressedThisFrame())
                {
                    pressed = true;
                    pressedKey = 'I'; // generic “InputAction”
                }
            }

            // 2) Raw gamepad/keyboard backup (ALWAYS check too)
            var gp = Gamepad.current;
            var kb = Keyboard.current;

            if (gp != null && gp.buttonSouth.wasPressedThisFrame)
            {
                pressed = true;
                pressedKey = 'A'; // gamepad south (A)
            }

            if (kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                pressed = true;
                pressedKey = 'S'; // Space
            }

            if (!pressed) return;

            // --- ROUTE THE PRESS BY GAME STATE ---
            var state = GameManager.Instance.State;
            Debug.Log($"[ButtonPress] Press detected in state={state} (key={pressedKey})");

            if (state == GameManager.GameState.CPT)
            {
                // 1) Feed the CPT interaction loop
                Interact._buttonPressed = true;

                // 2) Also record into the eye-tracking logger (if running)
                var logger = EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);
                if (logger != null && logger.IsLogging)
                {
                    logger.RegisterPress(pressedKey.ToString());
                }
                return;
            }

            // keep this disabled so IntroScreen Start button is the only “begin”
            // if (state == GameManager.GameState.PrepareCPT)
            // {
            //     MoxoCPTManager.Instance.OnGameBegin();
            // }
        }
    }
}

