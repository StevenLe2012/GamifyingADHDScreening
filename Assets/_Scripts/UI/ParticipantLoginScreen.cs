using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Login form shown before the intro video on WebGL.
/// Collects a 5-digit participant code only — no name or date.
/// Generated ID format: 12345  (exactly as typed, zero-padded to 5 digits)
///
/// Setup in Unity:
///   1. Create a full-screen Canvas in the IntroBoot scene (Sort Order = 10 so it sits on top).
///   2. Add a panel with:
///        - TMP_InputField  → codeField      (placeholder: "Enter 5-digit code", content type: Integer Number)
///        - TMP_Text        → idPreviewLabel (shows the ID live — optional)
///        - TMP_Text        → errorLabel     (validation messages, hidden by default)
///        - Button          → startButton    (label: "Start")
///   3. Assign all references in the Inspector.
///   4. Assign this GameObject to IntroBoot.loginScreen.
/// </summary>
public class ParticipantLoginScreen : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField codeField;

    [Header("Feedback")]
    [SerializeField] private TMP_Text idPreviewLabel;
    [SerializeField] private TMP_Text errorLabel;

    [Header("Button")]
    [SerializeField] private Button startButton;

    /// <summary>Fired when the form is successfully submitted.</summary>
    public event Action OnSubmitted;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        // Restrict to exactly 5 characters.
        if (codeField != null)
        {
            codeField.characterLimit = 5;
            codeField.contentType = TMP_InputField.ContentType.IntegerNumber;
            codeField.onValueChanged.AddListener(_ => RefreshPreview());
        }

        if (errorLabel != null)
            errorLabel.gameObject.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        RefreshPreview();
    }

    // ── Keyboard shortcut ─────────────────────────────────────────────────────

    void Update()
    {
        // Allow Enter or Space to submit when the code field is not focused.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnStartClicked();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool codeFocused = codeField != null && codeField.isFocused;
            if (!codeFocused)
                OnStartClicked();
        }
    }

    // ── Button handler ────────────────────────────────────────────────────────

    void OnStartClicked()
    {
        string code = codeField != null ? codeField.text.Trim() : "";

        // --- Validation ---
        if (string.IsNullOrWhiteSpace(code))
        {
            ShowError("Please enter your 5-digit participant code.");
            return;
        }

        if (!int.TryParse(code, out int parsedCode) || parsedCode < 0)
        {
            ShowError("Participant code must be a number (e.g. 10042).");
            return;
        }

        if (code.Length != 5)
        {
            ShowError($"Code must be exactly 5 digits (e.g. 10042). You entered {code.Length} digit(s).");
            return;
        }

        // --- Save to static holder so GameManager can read it after scene load ---
        ParticipantSession.Number       = code;          // stored as-is (5 digits)
        ParticipantSession.LastName     = "";            // not collected
        ParticipantSession.SessionDate  = "";            // not included in ID
        ParticipantSession.WasSubmitted = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLFullscreen.Request();
#endif

        HideError();
        gameObject.SetActive(false);
        OnSubmitted?.Invoke();
    }

    // ── Preview ───────────────────────────────────────────────────────────────

    void RefreshPreview()
    {
        if (idPreviewLabel == null) return;

        string code = codeField != null ? codeField.text.Trim() : "";
        idPreviewLabel.text = string.IsNullOrEmpty(code)
            ? "Your participant code will appear here"
            : $"Participant ID:  {code}";
    }

    // ── Error helpers ─────────────────────────────────────────────────────────

    void ShowError(string message)
    {
        if (errorLabel == null) return;
        errorLabel.text = message;
        errorLabel.gameObject.SetActive(true);
    }

    void HideError()
    {
        if (errorLabel != null)
            errorLabel.gameObject.SetActive(false);
    }
}
