using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("What happens when this is used")]
    [SerializeField] private UnityEvent onInteract;

    [Header("Gating (recommended)")]
    [Tooltip("If ON, this Interactable only fires while GameManager is in EXPLORE.")]
    [SerializeField] private bool requireExploreStateToFire = true;

    [Tooltip("If ON, block while any UI owns input or when global Space-suppression is active.")]
    [SerializeField] private bool respectUIInputFocus = true;

    [Header("Debounce")]
    [Tooltip("Minimum unscaled seconds between invokes.")]
    [SerializeField] private float cooldownSeconds = 0.20f;

    private float _cooldownUntilUnscaled = 0f;

    /// <summary>
    /// Call this from your Interactor or animation events.
    /// Now safely ignores presses during Narrative/Dialogue and right after UI closes.
    /// </summary>
    public void Interact()
    {
        // 0) Global UI focus/suppression
        if (respectUIInputFocus && (UIInputFocus.IsBlocked || UIInputFocus.SuppressNow))
            return;

        // 1) State gate
        if (requireExploreStateToFire && GameManager.Instance != null &&
            GameManager.Instance.State != GameManager.GameState.Explore)
            return;

        // 2) Debounce
        if (Time.unscaledTime < _cooldownUntilUnscaled)
            return;

        _cooldownUntilUnscaled = Time.unscaledTime + cooldownSeconds;

        // 3) Invoke
        onInteract?.Invoke();
    }
}
