using UnityEngine;
using UnityEngine.InputSystem;

namespace MoxoCPT
{
    [AddComponentMenu("MoxoCPT/Button Press")]
    public class ButtonPress : MonoBehaviour
    {
        // Optional: bind in the Inspector to <Gamepad>/buttonSouth and/or <Keyboard>/space
        [SerializeField] private InputActionReference actionPress;

        [Header("UI Gating")]
        [Tooltip("If a dialogue UI is active (UIInputFocus), ignore presses EXCEPT when Island picker is open.")]
        [SerializeField] private bool respectUIInputFocus = true;

        [Header("State Gating")]
        [Tooltip("If ON, this script only consumes input during CPT.")]
        [SerializeField] private bool onlyDuringCPT = true;

        [Header("Leak Suppression")]
        [Tooltip("Ignore Space/Submit that leaked from closing UI — only applied OUTSIDE CPT.")]
        [SerializeField] private bool respectSuppressSpaceThisFrame = true;

        [Tooltip("Ignore presses for a brief window after CPT FIRST starts (prevents the 'close intro' press from counting).")]
        [SerializeField] private float ignoreSecondsAfterCptEnter = 0.20f;

        [Header("Debounce")]
        [Tooltip("Minimum unscaled seconds between accepted presses.")]
        [SerializeField] private float pressDebounceSeconds = 0.075f;

        [Header("Press Feedback SFX")]
        [Tooltip("Optional sound played when a CPT press is accepted (target or non-target).")]
        [SerializeField] private AudioClip buttonPressSfx;
        [Tooltip("Optional audio source for buttonPressSfx. If empty, one is created on this object.")]
        [SerializeField] private AudioSource buttonPressAudioSource;
        [Range(0f, 1f)]
        [SerializeField] private float buttonPressSfxVolume = 1f;

        [Header("Diagnostics")]
        [Tooltip("Log the reason every time a Space/A press is detected but blocked by a gate.")]
        [SerializeField] private bool logBlockedPresses = true;

        private GameManager.GameState _prevState = GameManager.GameState.Explore;
        private float _cptEnteredUnscaled = -999f;
        private float _debounceUntilUnscaled = 0f;

        private void OnEnable()
        {
            if (actionPress != null && actionPress.action != null)
                actionPress.action.Enable();

            var gm = GameManager.Instance;
            if (gm != null) _prevState = gm.State;
        }

        private void OnDisable()
        {
            if (actionPress != null && actionPress.action != null)
                actionPress.action.Disable();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool isCPT = gm.State == GameManager.GameState.CPT;

            // ── Track genuine CPT entry for the startup cooldown ──────────────────────
            // Only set on real first entry (> 2 s gap), not on state-lock bounces
            // (CPT→other→CPT within milliseconds), which would reset the cooldown and
            // silence early presses.
            if (_prevState != gm.State)
            {
                if (isCPT && Time.unscaledTime - _cptEnteredUnscaled > 2f)
                    _cptEnteredUnscaled = Time.unscaledTime;
                _prevState = gm.State;
            }

            // ── Read all inputs NOW (before gates) ────────────────────────────────────
            // Reading wasPressedThisFrame does not consume the event – other scripts
            // can still read it.  We read early so gates can log a blocked press.
            if (actionPress != null && actionPress.action != null && !actionPress.action.enabled)
                actionPress.action.Enable();

            bool pressedAction  = actionPress != null && actionPress.action != null
                                  && actionPress.action.WasPressedThisFrame();
            bool pressedGamepad = Gamepad.current  != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            bool pressedSpace   = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool pressedEnter   = Keyboard.current != null &&
                                  (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
            bool anyPress       = pressedAction || pressedGamepad || pressedSpace || pressedEnter;

            // ── Gate 0 – Suppress leaked UI space (skipped during CPT) ───────────────
            if (!isCPT && respectSuppressSpaceThisFrame && UIInputFocus.SuppressNow)
            {
                if (anyPress && logBlockedPresses)
                    Debug.LogWarning($"[ButtonPress] BLOCKED Gate0_SuppressNow  state={gm.State}");
                return;
            }

            // ── Gate 1 – Hard CPT state gate ─────────────────────────────────────────
            if (onlyDuringCPT && !isCPT)
            {
                if (anyPress && logBlockedPresses)
                    Debug.LogWarning($"[ButtonPress] BLOCKED Gate1_NotCPT  state={gm.State}  isCPT={isCPT}");
                return;
            }

            // ── Gate 1b – MOXO run must be active ────────────────────────────────────
            // Results UI can still be shown while GameState is CPT; in that phase
            // card press SFX should be silent.
            var moxo = MoxoCPTManager.Instance;
            if (isCPT && (moxo == null || moxo.isGameOver))
            {
                if (anyPress && logBlockedPresses)
                    Debug.LogWarning("[ButtonPress] BLOCKED Gate1b_NotActiveRun  CPT but game is over.");
                return;
            }

            // ── Gate 2 – 200 ms startup cooldown (first CPT entry only) ──────────────
            float cooldownRemain = ignoreSecondsAfterCptEnter - (Time.unscaledTime - _cptEnteredUnscaled);
            if (isCPT && cooldownRemain > 0f)
            {
                if (anyPress && logBlockedPresses)
                    Debug.LogWarning($"[ButtonPress] BLOCKED Gate2_Cooldown  remain={cooldownRemain:F3}s");
                return;
            }

            // ── Gate 3 – UI focus block (skipped entirely during CPT) ─────────────────
            if (!isCPT && respectUIInputFocus && UIInputFocus.IsBlocked)
            {
                var picker = IslandSelectionUI.I ?? FindObjectOfType<IslandSelectionUI>(true);
                bool pickerOpen = picker != null && picker.gameObject.activeInHierarchy;
                if (!pickerOpen)
                {
                    if (anyPress && logBlockedPresses)
                        Debug.LogWarning($"[ButtonPress] BLOCKED Gate3_UIFocus  IsBlocked={UIInputFocus.IsBlocked}");
                    return;
                }
            }

            // ── Gate 4 – Debounce ─────────────────────────────────────────────────────
            if (Time.unscaledTime < _debounceUntilUnscaled)
            {
                if (anyPress && logBlockedPresses)
                    Debug.LogWarning($"[ButtonPress] BLOCKED Gate4_Debounce  remain={((_debounceUntilUnscaled - Time.unscaledTime) * 1000f):F0}ms");
                return;
            }

            // ── No press detected ─────────────────────────────────────────────────────
            if (!anyPress) return;

            // ── Press accepted ────────────────────────────────────────────────────────
            char pressedKey = pressedAction ? 'I' : pressedGamepad ? 'A' : (pressedEnter ? 'E' : 'S');
            _debounceUntilUnscaled = Time.unscaledTime + pressDebounceSeconds;

            Debug.Log($"[ButtonPress] ✓ CPT press consumed  key={pressedKey}  pressCount={Interact._pressCount + 1}  state={gm.State}");

            Interact._pressCount++;
            PlayPressSfx();

            var logger = EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);
            if (logger != null && logger.IsLogging)
                logger.RegisterPress(pressedKey.ToString());
        }

        private void PlayPressSfx()
        {
            if (buttonPressSfx == null) return;

            if (buttonPressAudioSource == null)
            {
                buttonPressAudioSource = GetComponent<AudioSource>();
                if (buttonPressAudioSource == null)
                    buttonPressAudioSource = gameObject.AddComponent<AudioSource>();

                buttonPressAudioSource.playOnAwake = false;
                buttonPressAudioSource.spatialBlend = 0f;
            }

            buttonPressAudioSource.PlayOneShot(buttonPressSfx, Mathf.Clamp01(buttonPressSfxVolume));
        }
    }
}
