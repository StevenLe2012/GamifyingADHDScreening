using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Login form shown before the intro video on WebGL.
/// Collects participant number and last name. Session date is set automatically
/// to today — participants do not need to enter it.
/// Generated ID format: 001_LASTNAME_YYYY-MM-DD
///
/// Setup in Unity:
///   1. Create a full-screen Canvas in the IntroBoot scene (Sort Order = 10 so it sits on top).
///   2. Add a panel with:
///        - TMP_InputField  → numberField    (placeholder: "Participant number", content type: Integer Number)
///        - TMP_InputField  → lastNameField  (placeholder: "Last name")
///        - TMP_Text        → idPreviewLabel (shows the generated ID live)
///        - TMP_Text        → errorLabel     (validation messages, hidden by default)
///        - Button          → startButton    (label: "Start")
///   3. Assign all references in the Inspector.
///   4. Assign this GameObject to IntroBoot.loginScreen.
/// </summary>
public class ParticipantLoginScreen : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField numberField;
    [SerializeField] private TMP_InputField lastNameField;

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
        // Restrict number field to 3 characters (001–999).
        if (numberField != null) numberField.characterLimit = 3;

        if (errorLabel != null)
            errorLabel.gameObject.SetActive(false);

        // Live preview updates as the user types.
        if (numberField   != null) numberField.onValueChanged.AddListener(_   => RefreshPreview());
        if (lastNameField != null) lastNameField.onValueChanged.AddListener(_ => RefreshPreview());

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        RefreshPreview();
    }

    // ── Keyboard shortcut ─────────────────────────────────────────────────────

    void Update()
    {
        // Allow Space to submit when neither input field is actively focused,
        // so typing a space inside the last-name field still works normally.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool numberFocused   = numberField   != null && numberField.isFocused;
            bool lastNameFocused = lastNameField != null && lastNameField.isFocused;

            if (!numberFocused && !lastNameFocused)
                OnStartClicked();
        }
    }

    // ── Button handler ────────────────────────────────────────────────────────

    void OnStartClicked()
    {
        string number   = numberField   != null ? numberField.text.Trim()   : "";
        string lastName = lastNameField != null ? lastNameField.text.Trim() : "";

        // --- Validation ---
        if (string.IsNullOrWhiteSpace(number))
        {
            ShowError("Please enter the participant number.");
            return;
        }

        if (!int.TryParse(number, out int parsedNum) || parsedNum < 1 || parsedNum > 999)
        {
            ShowError("Participant number must be 1–999 (e.g. 001, 042).");
            return;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            ShowError("Please enter the participant's last name.");
            return;
        }

        // Session date is always today — no participant input needed.
        string today = DateTime.Today.ToString("yyyy-MM-dd");

        // --- Save to static holder so GameManager can read it after scene load ---
        ParticipantSession.Number       = number;
        ParticipantSession.LastName     = lastName;
        ParticipantSession.SessionDate  = today;
        ParticipantSession.WasSubmitted = true;

        HideError();
        gameObject.SetActive(false);
        OnSubmitted?.Invoke();
    }

    // ── Preview ───────────────────────────────────────────────────────────────

    void RefreshPreview()
    {
        if (idPreviewLabel == null) return;

        string id = BuildPreviewId();
        idPreviewLabel.text = string.IsNullOrEmpty(id)
            ? "Your ID will appear here"
            : $"Your ID:  {id}";
    }

    string BuildPreviewId()
    {
        string num  = numberField   != null ? numberField.text.Trim()   : "";
        string last = Sanitize(lastNameField != null ? lastNameField.text : "");
        // Date is always today — shown in preview so participant can verify.
        string date = Sanitize(DateTime.Today.ToString("yyyy-MM-dd"));

        // Mirror GameManager.PadNumber: pad numeric strings to 3 digits.
        if (int.TryParse(num, out int n))
            num = n.ToString("D3");
        num = Sanitize(num);

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(num))  parts.Add(num);
        if (!string.IsNullOrEmpty(last)) parts.Add(last);
        if (!string.IsNullOrEmpty(date)) parts.Add(date);
        return string.Join("_", parts);
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors GameManager.Sanitize — keeps letters, digits, hyphens, underscores,
    /// uppercases everything. Ensures the preview matches the actual stored ID.
    /// </summary>
    static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var upper = s.Trim().ToUpperInvariant();
        var sb = new StringBuilder(upper.Length);
        foreach (char c in upper)
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                sb.Append(c);
        return sb.ToString();
    }
}
