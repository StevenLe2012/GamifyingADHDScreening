// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TeleportPad : MonoBehaviour
// {
//     [SerializeField] private InputActionReference controllerInput;
//     [SerializeField] private GameManager.GameState _state;
//     [SerializeField] private int code;
//     [SerializeField] private float _triggerAmountNeeded = 0.75f;
    
//     private float _curTrigger;

//     private void OnTriggerStay(Collider collider)
//     {
//         if (collider.gameObject.CompareTag("Player"))
//         {
//             // Hami: --- Controller Input (commented out for now) ---
//             // _curTrigger = controllerInput.action.ReadValue<float>();
//             // if (_curTrigger >= _triggerAmountNeeded)

//             // --- Temporary Keyboard Input (Spacebar) ---
//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 var newPosition = GetTeleportPosition();
//                 GameManager.Instance.UpdateGameState(_state);
//                 collider.gameObject.transform.position = newPosition;
//             }
//         }
//     }
//     private Vector3 GetTeleportPosition()
//     {
//         foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
//         {
//             if (tp.code == code && tp != this)
//             {
//                 var newPosition = tp.transform.position;
//                 newPosition.y += 2;
//                 return newPosition;
//             }
//         }

//         throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
//     }
// }


//Hami: Trigger with spacebar/gamepad stick A

// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TeleportPad : MonoBehaviour
// {
//     [SerializeField] private GameManager.GameState _state;
//     [SerializeField] private int code;

//     [Header("Debounce")]
//     [SerializeField] private float teleportCooldown = 0.2f;

//     private bool _playerInside;
//     private Transform _playerTransform;
//     private float _cooldownTimer;

//     private void Update()
//     {
//         if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
//         if (!_playerInside || _cooldownTimer > 0f) return;

//         // Gamepad A OR Space
//         bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
//         bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

//         if (pressedA || pressedSpace)
//         {
//             var newPosition = GetTeleportPosition();
//             GameManager.Instance.UpdateGameState(_state);
//             _playerTransform.position = newPosition;
//             _cooldownTimer = teleportCooldown;
//         }
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = true;
//             _playerTransform = other.transform;
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = false;
//             if (_playerTransform == other.transform) _playerTransform = null;
//         }
//     }

//     private Vector3 GetTeleportPosition()
//     {
//         foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
//         {
//             if (tp.code == code && tp != this)
//             {
//                 var newPosition = tp.transform.position;
//                 newPosition.y += 2f;
//                 return newPosition;
//             }
//         }
//         throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
//     }
// }


//Hami: Trigger new dialogue state:

// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TeleportPad : MonoBehaviour
// {
//     [Header("Teleport")]
//     [SerializeField] private GameManager.GameState _state; // state to set after teleport (usually Explore)
//     [SerializeField] private int code;                     // match this with the paired pad's code

//     [Header("Debounce")]
//     [SerializeField] private float teleportCooldown = 0.2f;

//     // 🔵 AfterGame handoff (use only on the RETURN pad)
//     [Header("After Game (return pad only)")]
//     [SerializeField] private Dialogue.TestHandler testHandler;    // drag your TestHandler here
//     [SerializeField] private bool triggerAfterGameOnUse = false;  // ✅ tick on the return pad
//     [SerializeField] private string afterGameKey  = "AfterGame";  // keep uniform with TestHandler
//     [SerializeField] private string afterGameText = "Press A to talk to the wizard.";

//     [Header("MOXO Intro (arrival pad only)")]
//     [SerializeField] private IntroScreen moxoIntroScreen;  // drag the same IntroScreen prefab/panel
//     [SerializeField] private bool showMoxoIntroOnUse = false;
//     [SerializeField] [TextArea] private string moxoTitle = "MOXO CPT";
//     [SerializeField] [TextArea] private string moxoBody  =
//     "Go/No-Go Task:\n\n• Press A when you see the TARGET card.\n• Do not press for NON-TARGET cards.\n• Distractors (audio/visual) may appear.\n\nPress A to start.";

    
//     private bool _playerInside;
//     private Transform _playerTransform;
//     private float _cooldownTimer;

//     private void Update()
//     {
//         if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
//         if (!_playerInside || _cooldownTimer > 0f) return;

//         // A or Space to activate
//         bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
//         bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

//         if (pressedA || pressedSpace)
//         {
//             var newPosition = GetTeleportPosition();
//             GameManager.Instance.UpdateGameState(_state);
//             _playerTransform.position = newPosition;
//             _cooldownTimer = teleportCooldown;

//             // 🔵 If this is the RETURN pad, hand off to AfterGame
//             if (triggerAfterGameOnUse && testHandler != null)
//             {
//                 // Shows hint UI ("Press A to talk to the wizard.") and advances dialogue to AfterGame
//                 testHandler.HandoffToAfterGame(afterGameKey, afterGameText);
//             }
//         }
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = true;
//             _playerTransform = other.transform;
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = false;
//             if (_playerTransform == other.transform) _playerTransform = null;
//         }
//     }

//     private Vector3 GetTeleportPosition()
//     {
//         foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
//         {
//             if (tp.code == code && tp != this)
//             {
//                 var newPosition = tp.transform.position;
//                 newPosition.y += 2f;
//                 return newPosition;
//             }
//         }
//         throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
//     }
// }


// // //Hami: Add Instruction for No go

// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TeleportPad : MonoBehaviour
// {
//     [Header("Teleport")]
//     [SerializeField] private GameManager.GameState _state; // state to set after teleport (usually Explore)
//     [SerializeField] private int code;                     // match this with the paired pad's code

//     [Header("Debounce")]
//     [SerializeField] private float teleportCooldown = 0.2f;

//     // 🔵 AfterGame handoff (use only on the RETURN pad)
//     [Header("After Game (return pad only)")]
//     [SerializeField] private Dialogue.TestHandler testHandler;    // drag your TestHandler here
//     [SerializeField] private bool triggerAfterGameOnUse = false;  // ✅ tick on the return pad
//     [SerializeField] private string afterGameKey  = "AfterGame";  // keep uniform with TestHandler
//     [SerializeField] private string afterGameText = "Press A to talk to the wizard.";

//     [Header("MOXO Intro (arrival pad only)")]
//     [SerializeField] private IntroScreen moxoIntroScreen;  // drag the same IntroScreen prefab/panel
//     [SerializeField] private bool showMoxoIntroOnUse = false;
//     [SerializeField] [TextArea] private string moxoTitle = "MOXO CPT";
//     [SerializeField] [TextArea] private string moxoBody  =
//     "Go/No-Go Task:\n\n• Press A when you see the TARGET card.\n• Do not press for NON-TARGET cards.\n• Distractors (audio/visual) may appear.\n\nPress A to start.";

    
//     private bool _playerInside;
//     private Transform _playerTransform;
//     private float _cooldownTimer;

//     private void Update()
//     {
//         if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
//         if (!_playerInside || _cooldownTimer > 0f) return;

//         // A or Space to activate
//         bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
//         bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

//         if (pressedA || pressedSpace)
//         {
//             var newPosition = GetTeleportPosition();
//             GameManager.Instance.UpdateGameState(_state);
//             _playerTransform.position = newPosition;
//             _cooldownTimer = teleportCooldown;

//             // 🔵 If this is the RETURN pad, hand off to AfterGame
//             if (triggerAfterGameOnUse && testHandler != null)
//             {
//                 // Shows hint UI ("Press A to talk to the wizard.") and advances dialogue to AfterGame
//                 testHandler.HandoffToAfterGame(afterGameKey, afterGameText);
//             }
//         }
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = true;
//             _playerTransform = other.transform;
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = false;
//             if (_playerTransform == other.transform) _playerTransform = null;
//         }
//     }

//     private Vector3 GetTeleportPosition()
//     {
//         foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
//         {
//             if (tp.code == code && tp != this)
//             {
//                 var newPosition = tp.transform.position;
//                 newPosition.y += 2f;
//                 return newPosition;
//             }
//         }
//         throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
//     }
// }



// //Hami: KEEP!!

// using System;
// using System.Collections; // added for coroutine
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TeleportPad : MonoBehaviour
// {
//     [Header("Teleport")]
//     [SerializeField] private GameManager.GameState _state; // state to set after teleport (usually Explore)
//     [SerializeField] private int code;                     // match this with the paired pad's code

//     [Header("Debounce")]
//     [SerializeField] private float teleportCooldown = 0.2f;


//     // 🔵 AfterGame handoff (use only on the RETURN pad)
//     [Header("After Game (return pad only)")]
//     [SerializeField] private Dialogue.TestHandler testHandler;    // drag your TestHandler here
//     [SerializeField] private bool triggerAfterGameOnUse = false;  // ✅ tick on the return pad
//     [SerializeField] private string afterGameKey  = "AfterGame";  // keep uniform with TestHandler
//     [SerializeField] private string afterGameText = "Press A to talk to the wizard.";

//     [Header("MOXO Intro (arrival pad only)")]
//     [SerializeField] private IntroScreen moxoIntroScreen;  // drag the same IntroScreen prefab/panel
//     [SerializeField] private bool showMoxoIntroOnUse = false;
//     [SerializeField] [TextArea] private string moxoTitle = "MOXO CPT";
//     [SerializeField] [TextArea] private string moxoBody  =
//         "Go/No-Go Task:\n\n• Press A when you see the TARGET card.\n" +
//         "• Do not press for NON-TARGET cards.\n" +
//         "• Distractors (audio/visual) may appear.\n\nPress A to start.";

//     // 🎵 NEW: Music controlled by THIS pad on use (so arrival pad sets destination music, return pad sets original)
//     [Header("Music (this pad triggers on use)")]
//     [SerializeField] private AudioClip destinationMusic;          // assign the music that should play after this teleport
//     [SerializeField] private float musicFadeSeconds = 1.5f;
//     [Range(0f, 1f)] [SerializeField] private float musicTargetVolume = 1f;
//     [SerializeField] private bool musicLoop = true;
//     [SerializeField] private bool playMusicOnUse = true;

//     private bool _playerInside;
//     private Transform _playerTransform;
//     private float _cooldownTimer;

//     private TeleportPad FindPairedPad()
//     {
//         foreach (var tp in FindObjectsOfType<TeleportPad>())
//         {
//             if (tp != this && tp.code == code)
//                 return tp;
//         }
//         return null;
//     }

//     private void Update()
//     {
//         if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
//         if (!_playerInside || _cooldownTimer > 0f) return;

//         // A or Space to activate
//         bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
//         bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

//         if (pressedA || pressedSpace)
//         {
//             // --- original teleport position logic preserved ---
//             var newPosition = GetTeleportPosition();
//             GameManager.Instance.UpdateGameState(_state);
//             _playerTransform.position = newPosition;            
//             _cooldownTimer = teleportCooldown;

//             // 🎵 minimal addition: switch music based on THIS pad's settings
//             if (playMusicOnUse && destinationMusic != null && MusicManager.Instance != null)
//             {
//                 MusicManager.Instance.Switch(destinationMusic, musicFadeSeconds, musicTargetVolume, musicLoop);
//                 Debug.Log($"TeleportPad switching music to: {destinationMusic?.name}");
//             }


//             // 🔵 show MOXO intro on arrival pads (one-frame defer to avoid timing issues)
//             if (showMoxoIntroOnUse && moxoIntroScreen != null)
//             {
//                 StartCoroutine(CoDeferredShowMoxo());
//             }

//             // 🟣 If this is the RETURN pad, hand off to AfterGame (unchanged)
//             if (triggerAfterGameOnUse && testHandler != null)
//             {
//                 // Shows hint UI ("Press A to talk to the wizard.") and advances dialogue to AfterGame
//                 testHandler.HandoffToAfterGame(afterGameKey, afterGameText);
//             }
//         }
//     }

    

//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = true;
//             _playerTransform = other.transform;
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = false;
//             if (_playerTransform == other.transform) _playerTransform = null;
//         }
//     }

//     private Vector3 GetTeleportPosition()
//     {
//         foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
//         {
//             if (tp.code == code && tp != this)
//             {
//                 var newPosition = tp.transform.position;
//                 newPosition.y += 2f;
//                 return newPosition;
//             }
//         }
//         throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
//     }

//     // 🔵 small helper: defer MOXO panel by a frame so UI always shows after teleport
//     private IEnumerator CoDeferredShowMoxo()
//     {
//         // 1) wait one frame so the teleport finishes
//         yield return null;

//         // 2) wait until A/Space are NOT held anymore (consume the teleport press)
//         while (true)
//         {
//             var gp = UnityEngine.InputSystem.Gamepad.current;
//             var kb = UnityEngine.InputSystem.Keyboard.current;

//             bool holdingA     = gp != null && gp.buttonSouth.isPressed;
//             bool holdingSpace = kb != null && kb.spaceKey.isPressed;

//             if (!holdingA && !holdingSpace)
//                 break; // safe to show UI

//             yield return null; // try again next frame
//         }

//         // 3) (optional) tiny grace delay so the next A press is a fresh tap
//         yield return new WaitForSeconds(0.05f);

//         // 4) show the MOXO intro (uses your existing text/preset & start logic)
//         moxoIntroScreen.ShowMoxo(moxoTitle, moxoBody);
//     }
// }

//Hami: Set the bound
// using System;
// using System.Collections;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.XR.Interaction.Toolkit;   // <-- for TeleportationProvider, TeleportRequest

// public class TeleportPad : MonoBehaviour
// {
//     // ---------- Landing helpers (unchanged) ----------
//     [Header("Landing (ground snap)")]
//     [SerializeField] private LayerMask groundMask = ~0;
//     [SerializeField] private float groundRayHeight = 5f;
//     [SerializeField] private float groundRayMaxDistance = 80f;
//     [SerializeField] private float groundUpOffset = 0.05f;
//     [SerializeField] private float landingInset = 1f;
//     [SerializeField] private Transform landingPointOverride;   // optional precise landing point
//     [SerializeField] private Transform controllerRoot;         // XR Origin / CC root (fallback: player)

//     [Header("Teleport")]
//     [SerializeField] private GameManager.GameState _state;
//     [SerializeField] private int code;

//     [Header("Debounce")]
//     [SerializeField] private float teleportCooldown = 0.2f;

//     // NEW: limiter & destination bounds
//     [Header("Bounds Switching")]
//     [SerializeField] private IslandEdgeLimiter limiter;      // drag your scene limiter here
//     [SerializeField] private BoxCollider destinationBounds;  // drag DESTINATION island bounds here

//     // 🔵 AfterGame handoff (return pad)
//     [Header("After Game (return pad only)")]
//     [SerializeField] private Dialogue.TestHandler testHandler;
//     [SerializeField] private bool triggerAfterGameOnUse = false;
//     [SerializeField] private string afterGameKey  = "AfterGame";
//     [SerializeField] private string afterGameText = "Press A to talk to the wizard.";

//     // MOXO intro on arrival
//     [Header("MOXO Intro (arrival pad only)")]
//     [SerializeField] private IntroScreen moxoIntroScreen;
//     [SerializeField] private bool showMoxoIntroOnUse = false;
//     [SerializeField, TextArea] private string moxoTitle = "MOXO CPT";
//     [SerializeField, TextArea] private string moxoBody =
//         "Go/No-Go Task:\n\n• Press A when you see the TARGET card.\n" +
//         "• Do not press for NON-TARGET cards.\n" +
//         "• Distractors (audio/visual) may appear.\n\nPress A to start.";

//     // Music
//     [Header("Music (this pad triggers on use)")]
//     [SerializeField] private AudioClip destinationMusic;
//     [SerializeField] private float musicFadeSeconds = 1.5f;
//     [Range(0f, 1f)] [SerializeField] private float musicTargetVolume = 1f;
//     [SerializeField] private bool musicLoop = true;
//     [SerializeField] private bool playMusicOnUse = true;

//     // NEW: XR TeleportationProvider hook
//     [Header("XR Teleport (optional)")]
//     [SerializeField] private TeleportationProvider teleportProvider;
//     [SerializeField] private bool useXRProvider = true;   // toggle to fall back to legacy move if needed

//     // --- DEBUG ---
//     [Header("Debug")]
//     [SerializeField] private bool debugLog = true;
//     [SerializeField] private bool debugDraw = true;

//     // MOXO show-once guard
//     [SerializeField] private bool showMoxoOnlyOnce = true;   // keep ON to only ever show once
//     private bool _moxoHasShown = false;                      // runtime flag


//     private void Log(string msg)  { if (debugLog) Debug.Log($"[TPad:{name}] {msg}", this); }
//     private void Warn(string msg) { Debug.LogWarning($"[TPad:{name}] {msg}", this); }

//     private bool _playerInside;
//     private Transform _playerTransform;
//     private float _cooldownTimer;
    

//     private void Update()
//     {
//         if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
//         if (!_playerInside || _cooldownTimer > 0f) return;

//         bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
//         bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

//         if (!pressedA && !pressedSpace) return;

//         Log($"Pressed. State->{_state}, code={code}. Limiter={(limiter?limiter.name:"null")}  DestBounds={(destinationBounds?destinationBounds.name:"null")}  Provider={(useXRProvider && teleportProvider!=null)}");
//         if (limiter == null) Warn("Limiter is NULL – clamping cannot occur.");
//         if (destinationBounds == null) Warn("DestinationBounds is NULL – will not clamp into island.");
//         if (_playerTransform == null)  Warn("Player transform is NULL.");

//         // --- find base destination (paired pad) ---
//         var basePos = GetTeleportPosition();
//         Log($"Paired pad raw pos (+2y in GetTeleportPosition): {basePos:F3}");

//         // --- find base destination (paired pad or LandingPoint) ---
//         var feetTarget = GetTeleportPosition(); // start with pair

//         if (landingPointOverride != null)
//         {
//             // Use LP only if it's inside destination bounds; otherwise ignore.
//             var lp = landingPointOverride.position;
//             bool lpInside = destinationBounds == null || destinationBounds.bounds.Contains(lp);
//             if (!lpInside)
//                 Debug.LogWarning($"[TPad:{name}] LandingPoint '{landingPointOverride.name}' is OUTSIDE destination bounds '{destinationBounds?.name}'. Using paired pad instead.", this);

//             feetTarget = lpInside ? lp : (FindPairedPad() != null
//                 ? FindPairedPad().transform.position + FindPairedPad().transform.forward * landingInset
//                 : feetTarget);
//         }
//         else
//         {
//             var pair = FindPairedPad();
//             if (pair != null) feetTarget = pair.transform.position + pair.transform.forward * landingInset;
//         }
//         // update game state first
//         GameManager.Instance.UpdateGameState(_state);

//         // do the safe move (handles ground, limiter order, provider/legacy)
//         SafeTeleportTo(feetTarget);

//         _cooldownTimer = teleportCooldown;

//         // 🎵 music switch
//         if (playMusicOnUse && destinationMusic != null && MusicManager.Instance != null)
//         {
//             Log($"Music switch -> '{destinationMusic.name}' fade={musicFadeSeconds}s targetVol={musicTargetVolume:0.00} loop={musicLoop}");
//             MusicManager.Instance.Switch(destinationMusic, musicFadeSeconds, musicTargetVolume, musicLoop);
//         }

//         // Bounds switch (clamp once to be safe)
//         if (limiter != null && destinationBounds != null)
//         {
//             if (useXRProvider && teleportProvider != null)
//             {
//                 Log("XR provider path: will clamp after teleport finishes.");
//                 StartCoroutine(CoClampAfterTeleport());  // we'll clamp after XR finishes
//             }
//             else
//             {
//                 Log("Legacy path: limiter.SetBounds() now.");
//                 limiter.SetBounds(destinationBounds);     // legacy path: clamp now
//             }
//         }

//         // Show MOXO exactly once, then never again this session
//         if (showMoxoIntroOnUse &&
//             moxoIntroScreen != null &&
//             (!showMoxoOnlyOnce || !_moxoHasShown) &&
//             !moxoIntroScreen.IsVisible)
//         {
//             _moxoHasShown = true;               // lock it so it won’t pop up again
//             StartCoroutine(CoDeferredShowMoxo());  // waits one frame so your teleport press isn’t consumed
//         }

//         // AfterGame hint on return pad
//         if (triggerAfterGameOnUse && testHandler != null)
//             testHandler.HandoffToAfterGame(afterGameKey, afterGameText);
//     }


//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = true;
//             _playerTransform = other.transform;
//             Log("Player entered pad trigger.");
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             _playerInside = false;
//             if (_playerTransform == other.transform) _playerTransform = null;
//             Log("Player exited pad trigger.");
//         }
//     }

//     private TeleportPad FindPairedPad()
//     {
//         var pads = FindObjectsOfType<TeleportPad>();
//         for (int i = 0; i < pads.Length; i++)
//         {
//             var tp = pads[i];
//             if (tp != this && tp.code == code) return tp;
//         }
//         return null;
//     }

//     private Vector3 GetTeleportPosition()
//     {
//         var pair = FindPairedPad();
//         if (pair != null)
//         {
//             var pos = pair.transform.position;
//             pos.y += 2f;
//             return pos;
//         }
//         throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
//     }

//     // ---------- landing helpers ----------
//     private Vector3 ClampXZIntoBounds(BoxCollider box, Vector3 pos, float inset)
//     {
//         if (!box) return pos;
//         var b = box.bounds;
//         var clamped = new Vector3(
//             Mathf.Clamp(pos.x, b.min.x + inset, b.max.x - inset),
//             pos.y,
//             Mathf.Clamp(pos.z, b.min.z + inset, b.max.z - inset)
//         );
//         return clamped;
//     }

//     private bool TryFindGround(Vector3 seed, out Vector3 result)
//     {
//         if (landingPointOverride) seed = landingPointOverride.position;
//         Vector3 start = seed + Vector3.up * groundRayHeight;

//         RaycastHit hit;
//         if (Physics.SphereCast(start, 0.3f, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
//         {
//             if (debugDraw)
//             {
//                 Debug.DrawLine(start, hit.point, Color.green, 2f);
//                 Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.green, 2f);
//             }
//             Log($"TryFindGround (SphereCast): HIT '{hit.collider.name}' -> y={hit.point.y:F3}");
//             result = new Vector3(seed.x, hit.point.y + groundUpOffset, seed.z);
//             return true;
//         }
//         if (Physics.Raycast(start, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
//         {
//             if (debugDraw)
//             {
//                 Debug.DrawLine(start, hit.point, Color.green, 2f);
//                 Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.green, 2f);
//             }
//             Log($"TryFindGround (Raycast): HIT '{hit.collider.name}' -> y={hit.point.y:F3}");
//             result = new Vector3(seed.x, hit.point.y + groundUpOffset, seed.z);
//             return true;
//         }
//         if (debugDraw) Debug.DrawRay(start, Vector3.down * groundRayMaxDistance, Color.red, 2f);
//         Log($"TryFindGround: NO HIT (seed={seed:F3}, mask={groundMask.value}, start={start:F3}, dist={groundRayMaxDistance})");
//         result = seed;
//         return false;
//     }

//     private Vector3 ResolveLanding(BoxCollider destBounds, Vector3 guess)
//     {
//         // 1) clamp XZ inside island
//         Vector3 xz = ClampXZIntoBounds(destBounds, guess, landingInset);

//         // 2) ground it
//         Vector3 landed;
//         if (TryFindGround(xz, out landed))
//         {
//             Log($"ResolveLanding: grounded at {landed:F3}");
//             return landed;
//         }

//         // 3) fallback: bounds center XZ, grounded
//         var centerXZ = new Vector3(destBounds.bounds.center.x, xz.y, destBounds.bounds.center.z);
//         if (TryFindGround(centerXZ, out landed))
//         {
//             Log($"ResolveLanding: fallback center grounded at {landed:F3}");
//             return landed;
//         }

//         // 4) last resort: clamped XZ with original Y
//         Log($"ResolveLanding: fallback to clamped XZ (no ground hit) -> {xz:F3}");
//         return xz;
//     }

//     // Simple helper: snap a point to ground using the same ray settings.
//     private Vector3 SnapToGround(Vector3 target)
//     {
//         Vector3 grounded;
//         if (TryFindGround(target, out grounded))
//         {
//             Log($"SnapToGround -> {grounded:F3}");
//             return grounded;
//         }
//         Log($"SnapToGround: NO HIT, keep {target:F3}");
//         return target; // fall back to the original if no ground hit
//     }

//     // Delay showing MOXO until teleport input is released
//     private IEnumerator CoDeferredShowMoxo()
//     {
//         yield return null;
//         while (true)
//         {
//             var gp = Gamepad.current;
//             var kb = Keyboard.current;
//             bool holdingA     = gp != null && gp.buttonSouth.isPressed;
//             bool holdingSpace = kb != null && kb.spaceKey.isPressed;
//             if (!holdingA && !holdingSpace) break;
//             yield return null;
//         }
//         yield return new WaitForSeconds(0.05f);
//         Log("Showing MOXO intro.");
//         moxoIntroScreen.ShowMoxo(moxoTitle, moxoBody);
//     }

//     private IEnumerator CoClampAfterTeleport()
//     {
//         while (teleportProvider != null &&
//             (teleportProvider.locomotionPhase == LocomotionPhase.Idle ||
//              teleportProvider.locomotionPhase == LocomotionPhase.Started))
//             yield return null;

//         while (teleportProvider != null &&
//             (teleportProvider.locomotionPhase == LocomotionPhase.Moving))
//             yield return null;

//         yield return null;
//         if (limiter != null && destinationBounds != null)
//         {
//             limiter.SetBounds(destinationBounds); // your limiter clamps once on SetBounds
//         }
//     }

//     // Teleport in a safe order (limiter -> ground -> move -> post clamp)
//     private void SafeTeleportTo(Vector3 feetWorldPos)
//     {
//         Log($"SafeTeleportTo (seed feet) {feetWorldPos:F3}");

//         // 0) Clamp inside island XZ (inset keeps you from walls)
//         if (destinationBounds != null)
//             feetWorldPos = ClampXZIntoBounds(destinationBounds, feetWorldPos, Mathf.Max(landingInset, 0.25f));

//         // 1) Snap Y to ground (feet)
//         feetWorldPos = SnapFeetToGround(feetWorldPos);

//         // 2) Switch limiter to destination BEFORE we move so it won't shove us back
//         if (limiter != null && destinationBounds != null)
//         {
//             limiter.SetBounds(destinationBounds);
//         }

//         // 3) Do the move (XR provider or legacy)
//         if (useXRProvider && teleportProvider != null)
//         {
//             var xr = teleportProvider.system?.xrOrigin;
//             var height = xr != null ? xr.CameraInOriginSpaceHeight : 1.6f;
//             var camDest = feetWorldPos + Vector3.up * height;
//             var req = new TeleportRequest
//             {
//                 destinationPosition = camDest,
//                 destinationRotation = Quaternion.LookRotation(transform.forward, Vector3.up),
//                 matchOrientation    = MatchOrientation.WorldSpaceUp
//             };
//             teleportProvider.QueueTeleportRequest(req);
//             StartCoroutine(CoClampAfterTeleport());
//         }
//         else
//         {
//             var root = controllerRoot != null ? controllerRoot : _playerTransform;
//             if (root != null)
//             {
//                 var cc = root.GetComponent<CharacterController>();
//                 var rb = root.GetComponent<Rigidbody>();
//                 if (cc) { cc.enabled = false; }
//                 if (rb) { rb.isKinematic = true; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; Log("LEGACY: RB kinematic & zeroed."); }

//                 root.position = feetWorldPos;

//                 if (rb) { rb.isKinematic = false; }
//                 if (cc) { cc.enabled = true; cc.Move(Vector3.zero); Log("LEGACY: CC re-enabled + Move(0)."); }
//             }

//             if (limiter != null && destinationBounds != null)
//             {
//                 limiter.SetBounds(destinationBounds);
//             }
//         }
//     }

//     // Raycast down to get a solid FEET position
//     private Vector3 SnapFeetToGround(Vector3 seedFeet)
//     {
//         Vector3 start = seedFeet + Vector3.up * groundRayHeight;
//         RaycastHit hit;
//         if (Physics.SphereCast(start, 0.3f, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
//         {
//             if (debugDraw)
//             {
//                 Debug.DrawLine(start, hit.point, Color.cyan, 2f);
//                 Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.cyan, 2f);
//             }
//             var p = new Vector3(seedFeet.x, hit.point.y + groundUpOffset, seedFeet.z);
//             return p;
//         }
//         if (Physics.Raycast(start, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
//         {
//             if (debugDraw)
//             {
//                 Debug.DrawLine(start, hit.point, Color.cyan, 2f);
//                 Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.cyan, 2f);
//             }
//             var p = new Vector3(seedFeet.x, hit.point.y + groundUpOffset, seedFeet.z);
//             return p;
//         }
//         if (debugDraw) Debug.DrawRay(start, Vector3.down * groundRayMaxDistance, Color.red, 2f);
//         return seedFeet; // fallback: keep Y
//     }

//     private void OnDrawGizmosSelected()
//     {
//         if (!debugDraw) return;

//         if (destinationBounds != null)
//         {
//             Gizmos.color = new Color(0, 1, 1, 0.25f);
//             var b = destinationBounds.bounds;
//             Gizmos.DrawCube(b.center, b.size);
//             Gizmos.color = Color.cyan;
//             Gizmos.DrawWireCube(b.center, b.size);
//         }

//         if (landingPointOverride != null)
//         {
//             Gizmos.color = Color.yellow;
//             Gizmos.DrawSphere(landingPointOverride.position, 0.1f);
//             Gizmos.color = Color.magenta;
//             Gizmos.DrawRay(landingPointOverride.position, landingPointOverride.forward * Mathf.Max(landingInset, 0.25f));
//         }
//     }

    
// }

//Hami: Disable temporarily
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;   // <-- for TeleportationProvider, TeleportRequest

public class TeleportPad : MonoBehaviour
{
    // ---------- Landing helpers (unchanged) ----------
    [Header("Landing (ground snap)")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayHeight = 5f;
    [SerializeField] private float groundRayMaxDistance = 80f;
    [SerializeField] private float groundUpOffset = 0.05f;
    [SerializeField] private float landingInset = 1f;
    [SerializeField] private Transform landingPointOverride;   // optional precise landing point
    [SerializeField] private Transform controllerRoot;         // XR Origin / CC root (fallback: player)

    [Header("Teleport")]
    [SerializeField] private GameManager.GameState _state;
    [SerializeField] private int code;

    [Header("Debounce")]
    [SerializeField] private float teleportCooldown = 0.2f;

    // NEW: limiter & destination bounds
    [Header("Bounds Switching")]
    [SerializeField] private IslandEdgeLimiter limiter;      // drag your scene limiter here
    [SerializeField] private BoxCollider destinationBounds;  // drag DESTINATION island bounds here

    // 🔵 AfterGame handoff (return pad)
    [Header("After Game (return pad only)")]
    [SerializeField] private Dialogue.TestHandler testHandler;
    [SerializeField] private bool triggerAfterGameOnUse = false;
    [SerializeField] private string afterGameKey  = "AfterGame";
    [SerializeField] private string afterGameText = "Press A to talk to the wizard.";

    // MOXO intro on arrival
    [Header("MOXO Intro (arrival pad only)")]
    [SerializeField] private IntroScreen moxoIntroScreen;
    [SerializeField] private bool showMoxoIntroOnUse = false;
    [SerializeField, TextArea] private string moxoTitle = "MOXO CPT";
    [SerializeField, TextArea] private string moxoBody =
        "Go/No-Go Task:\n\n• Press A when you see the TARGET card.\n" +
        "• Do not press for NON-TARGET cards.\n" +
        "• Distractors (audio/visual) may appear.\n\nPress A to start.";

    // Music
    [Header("Music (this pad triggers on use)")]
    [SerializeField] private AudioClip destinationMusic;
    [SerializeField] private float musicFadeSeconds = 1.5f;
    [Range(0f, 1f)] [SerializeField] private float musicTargetVolume = 1f;
    [SerializeField] private bool musicLoop = true;
    [SerializeField] private bool playMusicOnUse = true;

    // XR TeleportationProvider hook
    [Header("XR Teleport (optional)")]
    [SerializeField] private TeleportationProvider teleportProvider;
    [SerializeField] private bool useXRProvider = true;   // toggle to fall back to legacy move if needed

    // --- DEBUG ---
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool debugDraw = true;

    // --- Enable/disable---
    [Header("Auto Enable/Disable")]
    [SerializeField] private bool gateByGameState = true;      // ON: pad toggles itself
    [SerializeField] private bool disableDuringCPT = true;      // usually ON
    [SerializeField] private bool requireAfterGameToEnable = false; // tick ON only on the RETURN pad
    [SerializeField] private Collider padTrigger;               // assign the pad's trigger collider
    [SerializeField] private GameObject padVisual;              // optional: glow/mesh to also hide/show
    [SerializeField] private bool disableInPrepareCPT = true;
    [SerializeField] private bool disableInCPT        = true;

    private void SetPadEnabled(bool enabled)
    {
        if (padTrigger) padTrigger.enabled = enabled;
        if (padVisual)  padVisual.SetActive(enabled);
    }
    

    private GameManager.GameState _lastState;

    private void LateUpdate()
    {
        var gm = GameManager.Instance;
        var state = gm != null ? gm.State : GameManager.GameState.Explore;
        if (state == _lastState) return;
        _lastState = state;

        bool allow =
            !(disableInPrepareCPT && state == GameManager.GameState.PrepareCPT) &&
            !(disableInCPT        && state == GameManager.GameState.CPT);

        SetPadEnabled(allow);
    }    

    // MOXO show-once guard
    [SerializeField] private bool showMoxoOnlyOnce = true;   // keep ON to only ever show once
    private bool _moxoHasShown = false;                      // runtime flag


    private void Log(string msg)  { if (debugLog) Debug.Log($"[TPad:{name}] {msg}", this); }
    private void Warn(string msg) { Debug.LogWarning($"[TPad:{name}] {msg}", this); }

    private bool _playerInside;
    private Transform _playerTransform;
    private float _cooldownTimer;
    

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.State == GameManager.GameState.CPT)
            return;
        
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        if (!_playerInside || _cooldownTimer > 0f) return;

        bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
        bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (!pressedA && !pressedSpace) return;

        Log($"Pressed. State->{_state}, code={code}. Limiter={(limiter?limiter.name:"null")}  DestBounds={(destinationBounds?destinationBounds.name:"null")}  Provider={(useXRProvider && teleportProvider!=null)}");
        if (limiter == null) Warn("Limiter is NULL – clamping cannot occur.");
        if (destinationBounds == null) Warn("DestinationBounds is NULL – will not clamp into island.");
        if (_playerTransform == null)  Warn("Player transform is NULL.");

        // --- find base destination (paired pad) ---
        var basePos = GetTeleportPosition();
        Log($"Paired pad raw pos (+2y in GetTeleportPosition): {basePos:F3}");

        // --- find base destination (paired pad or LandingPoint) ---
        var feetTarget = GetTeleportPosition(); // start with pair

        if (landingPointOverride != null)
        {
            // Use LP only if it's inside destination bounds; otherwise ignore.
            var lp = landingPointOverride.position;
            bool lpInside = destinationBounds == null || destinationBounds.bounds.Contains(lp);
            if (!lpInside)
                Debug.LogWarning($"[TPad:{name}] LandingPoint '{landingPointOverride.name}' is OUTSIDE destination bounds '{destinationBounds?.name}'. Using paired pad instead.", this);

            feetTarget = lpInside ? lp : (FindPairedPad() != null
                ? FindPairedPad().transform.position + FindPairedPad().transform.forward * landingInset
                : feetTarget);
        }
        else
        {
            var pair = FindPairedPad();
            if (pair != null) feetTarget = pair.transform.position + pair.transform.forward * landingInset;
        }
        // update game state first
        GameManager.Instance.UpdateGameState(_state);

        // do the safe move (handles ground, limiter order, provider/legacy)
        SafeTeleportTo(feetTarget);

        _cooldownTimer = teleportCooldown;

        if (gateByGameState)
        {
            var st = GameManager.Instance ? GameManager.Instance.State : GameManager.GameState.Explore;

            // Default: allowed
            bool allow = true;

            // 1) Block during CPT so response presses can't trigger the pad
            if (disableDuringCPT && st == GameManager.GameState.CPT)
                allow = false;

            // 2) Optional: make this pad only usable when you're back in Explore
            //    (use this on the RETURN pad if you want to be explicit)
            if (requireAfterGameToEnable && st != GameManager.GameState.Explore)
                allow = false;

            SetPadEnabled(allow);
            if (!allow) return; // stop processing the pad this frame
        }

        // 🎵 music switch
        if (playMusicOnUse && destinationMusic != null && MusicManager.Instance != null)
        {
            Log($"Music switch -> '{destinationMusic.name}' fade={musicFadeSeconds}s targetVol={musicTargetVolume:0.00} loop={musicLoop}");
            MusicManager.Instance.Switch(destinationMusic, musicFadeSeconds, musicTargetVolume, musicLoop);
        }

        // Bounds switch (clamp once to be safe)
        if (limiter != null && destinationBounds != null)
        {
            if (useXRProvider && teleportProvider != null)
            {
                Log("XR provider path: will clamp after teleport finishes.");
                StartCoroutine(CoClampAfterTeleport());  // we'll clamp after XR finishes
            }
            else
            {
                Log("Legacy path: limiter.SetBounds() now.");
                limiter.SetBounds(destinationBounds);     // legacy path: clamp now
            }
        }

        // Show MOXO exactly once, then never again this session
        if (showMoxoIntroOnUse &&
            moxoIntroScreen != null &&
            (!showMoxoOnlyOnce || !_moxoHasShown) &&
            !moxoIntroScreen.IsVisible)
        {
            _moxoHasShown = true;               // lock it so it won’t pop up again
            StartCoroutine(CoDeferredShowMoxo());  // waits one frame so your teleport press isn’t consumed
        }

        // AfterGame hint on return pad
        if (triggerAfterGameOnUse && testHandler != null)
            testHandler.HandoffToAfterGame(afterGameKey, afterGameText);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
            _playerTransform = other.transform;
            Log("Player entered pad trigger.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
            if (_playerTransform == other.transform) _playerTransform = null;
            Log("Player exited pad trigger.");
        }
    }

    private TeleportPad FindPairedPad()
    {
        var pads = FindObjectsOfType<TeleportPad>();
        for (int i = 0; i < pads.Length; i++)
        {
            var tp = pads[i];
            if (tp != this && tp.code == code) return tp;
        }
        return null;
    }

    private Vector3 GetTeleportPosition()
    {
        var pair = FindPairedPad();
        if (pair != null)
        {
            var pos = pair.transform.position;
            pos.y += 2f;
            return pos;
        }
        throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
    }

    // ---------- landing helpers ----------
    private Vector3 ClampXZIntoBounds(BoxCollider box, Vector3 pos, float inset)
    {
        if (!box) return pos;
        var b = box.bounds;
        var clamped = new Vector3(
            Mathf.Clamp(pos.x, b.min.x + inset, b.max.x - inset),
            pos.y,
            Mathf.Clamp(pos.z, b.min.z + inset, b.max.z - inset)
        );
        return clamped;
    }

    private bool TryFindGround(Vector3 seed, out Vector3 result)
    {
        if (landingPointOverride) seed = landingPointOverride.position;
        Vector3 start = seed + Vector3.up * groundRayHeight;

        RaycastHit hit;
        if (Physics.SphereCast(start, 0.3f, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw)
            {
                Debug.DrawLine(start, hit.point, Color.green, 2f);
                Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.green, 2f);
            }
            Log($"TryFindGround (SphereCast): HIT '{hit.collider.name}' -> y={hit.point.y:F3}");
            result = new Vector3(seed.x, hit.point.y + groundUpOffset, seed.z);
            return true;
        }
        if (Physics.Raycast(start, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw)
            {
                Debug.DrawLine(start, hit.point, Color.green, 2f);
                Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.green, 2f);
            }
            Log($"TryFindGround (Raycast): HIT '{hit.collider.name}' -> y={hit.point.y:F3}");
            result = new Vector3(seed.x, hit.point.y + groundUpOffset, seed.z);
            return true;
        }
        if (debugDraw) Debug.DrawRay(start, Vector3.down * groundRayMaxDistance, Color.red, 2f);
        Log($"TryFindGround: NO HIT (seed={seed:F3}, mask={groundMask.value}, start={start:F3}, dist={groundRayMaxDistance})");
        result = seed;
        return false;
    }

    private Vector3 ResolveLanding(BoxCollider destBounds, Vector3 guess)
    {
        // 1) clamp XZ inside island
        Vector3 xz = ClampXZIntoBounds(destBounds, guess, landingInset);

        // 2) ground it
        Vector3 landed;
        if (TryFindGround(xz, out landed))
        {
            Log($"ResolveLanding: grounded at {landed:F3}");
            return landed;
        }

        // 3) fallback: bounds center XZ, grounded
        var centerXZ = new Vector3(destBounds.bounds.center.x, xz.y, destBounds.bounds.center.z);
        if (TryFindGround(centerXZ, out landed))
        {
            Log($"ResolveLanding: fallback center grounded at {landed:F3}");
            return landed;
        }

        // 4) last resort: clamped XZ with original Y
        Log($"ResolveLanding: fallback to clamped XZ (no ground hit) -> {xz:F3}");
        return xz;
    }

    // Simple helper: snap a point to ground using the same ray settings.
    private Vector3 SnapToGround(Vector3 target)
    {
        Vector3 grounded;
        if (TryFindGround(target, out grounded))
        {
            Log($"SnapToGround -> {grounded:F3}");
            return grounded;
        }
        Log($"SnapToGround: NO HIT, keep {target:F3}");
        return target; // fall back to the original if no ground hit
    }

    // Delay showing MOXO until teleport input is released
    private IEnumerator CoDeferredShowMoxo()
    {
        yield return null;
        while (true)
        {
            var gp = Gamepad.current;
            var kb = Keyboard.current;
            bool holdingA     = gp != null && gp.buttonSouth.isPressed;
            bool holdingSpace = kb != null && kb.spaceKey.isPressed;
            if (!holdingA && !holdingSpace) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.05f);
        Log("Showing MOXO intro.");
        moxoIntroScreen.ShowMoxo(moxoTitle, moxoBody);
    }

    private IEnumerator CoClampAfterTeleport()
    {
        while (teleportProvider != null &&
            (teleportProvider.locomotionPhase == LocomotionPhase.Idle ||
             teleportProvider.locomotionPhase == LocomotionPhase.Started))
            yield return null;

        while (teleportProvider != null &&
            (teleportProvider.locomotionPhase == LocomotionPhase.Moving))
            yield return null;

        yield return null;
        if (limiter != null && destinationBounds != null)
        {
            limiter.SetBounds(destinationBounds); // your limiter clamps once on SetBounds
        }
    }

    // Teleport in a safe order (limiter -> ground -> move -> post clamp)
    private void SafeTeleportTo(Vector3 feetWorldPos)
    {
        Log($"SafeTeleportTo (seed feet) {feetWorldPos:F3}");

        // 0) Clamp inside island XZ (inset keeps you from walls)
        if (destinationBounds != null)
            feetWorldPos = ClampXZIntoBounds(destinationBounds, feetWorldPos, Mathf.Max(landingInset, 0.25f));

        // 1) Snap Y to ground (feet)
        feetWorldPos = SnapFeetToGround(feetWorldPos);

        // 2) Switch limiter to destination BEFORE we move so it won't shove us back
        if (limiter != null && destinationBounds != null)
        {
            limiter.SetBounds(destinationBounds);
        }

        // 3) Do the move (XR provider or legacy)
        if (useXRProvider && teleportProvider != null)
        {
            var xr = teleportProvider.system?.xrOrigin;
            var height = xr != null ? xr.CameraInOriginSpaceHeight : 1.6f;
            var camDest = feetWorldPos + Vector3.up * height;
            var req = new TeleportRequest
            {
                destinationPosition = camDest,
                destinationRotation = Quaternion.LookRotation(transform.forward, Vector3.up),
                matchOrientation    = MatchOrientation.WorldSpaceUp
            };
            teleportProvider.QueueTeleportRequest(req);
            StartCoroutine(CoClampAfterTeleport());
        }
        else
        {
            var root = controllerRoot != null ? controllerRoot : _playerTransform;
            if (root != null)
            {
                var cc = root.GetComponent<CharacterController>();
                var rb = root.GetComponent<Rigidbody>();
                if (cc) { cc.enabled = false; }
                if (rb) { rb.isKinematic = true; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; Log("LEGACY: RB kinematic & zeroed."); }

                root.position = feetWorldPos;

                if (rb) { rb.isKinematic = false; }
                if (cc) { cc.enabled = true; cc.Move(Vector3.zero); Log("LEGACY: CC re-enabled + Move(0)."); }
            }

            if (limiter != null && destinationBounds != null)
            {
                limiter.SetBounds(destinationBounds);
            }
        }
    }

    // Raycast down to get a solid FEET position
    private Vector3 SnapFeetToGround(Vector3 seedFeet)
    {
        Vector3 start = seedFeet + Vector3.up * groundRayHeight;
        RaycastHit hit;
        if (Physics.SphereCast(start, 0.3f, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw)
            {
                Debug.DrawLine(start, hit.point, Color.cyan, 2f);
                Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.cyan, 2f);
            }
            var p = new Vector3(seedFeet.x, hit.point.y + groundUpOffset, seedFeet.z);
            return p;
        }
        if (Physics.Raycast(start, Vector3.down, out hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw)
            {
                Debug.DrawLine(start, hit.point, Color.cyan, 2f);
                Debug.DrawRay(hit.point, Vector3.up * 0.3f, Color.cyan, 2f);
            }
            var p = new Vector3(seedFeet.x, hit.point.y + groundUpOffset, seedFeet.z);
            return p;
        }
        if (debugDraw) Debug.DrawRay(start, Vector3.down * groundRayMaxDistance, Color.red, 2f);
        return seedFeet; // fallback: keep Y
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw) return;

        if (destinationBounds != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.25f);
            var b = destinationBounds.bounds;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        if (landingPointOverride != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(landingPointOverride.position, 0.1f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(landingPointOverride.position, landingPointOverride.forward * Mathf.Max(landingInset, 0.25f));
        }
    }

    
}

