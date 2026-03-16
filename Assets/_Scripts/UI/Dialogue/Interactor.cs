using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [Header("Input (Space only)")]
    [Tooltip("Only SPACE will confirm/interact. Gamepad/Enter are ignored.")]
    [SerializeField] private bool spaceToInteract = true;

    [Header("Range")]
    [SerializeField] private float interactRadius = 2.0f;

    [Header("UI Gating (Block Interact during dialogue UI)")]
    [SerializeField] private GameObject dialogueOptionsRoot;
    [SerializeField] private GameObject dialogueSubtitleRoot;

    [Header("State Gating")]
    [Tooltip("If true, SPACE will only trigger Interact() when GameManager is in EXPLORE state.")]
    [SerializeField] private bool requireExploreStateToInteract = true;

    [Header("Debounce / Suppression")]
    [Tooltip("General debounce between world Interact presses (unscaled).")]
    [SerializeField] private float interactDebounceSeconds = 0.15f;

    private Collider[] _collidersInRange;
    private readonly List<Interactable> _interactablesInRange = new List<Interactable>();
    private Interactable _closestInteractable;

    private float _debounceUntilUnscaled = 0f;

    private void Update()
    {
        // Global block: Narrative (and any other UI that called UIInputFocus.Push) disables world space.
        if (UIInputFocus.IsBlocked || UIInputFocus.SuppressNow) return;

        // Optional: hard gate by state (prevents space during Narrative/PrepareCPT/CPT)
        if (requireExploreStateToInteract && GameManager.Instance != null &&
            GameManager.Instance.State != GameManager.GameState.Explore)
            return;

        // If any dialogue widgets are visible, do not interact with world.
        if ((dialogueOptionsRoot != null && dialogueOptionsRoot.activeInHierarchy) ||
            (dialogueSubtitleRoot != null && dialogueSubtitleRoot.activeInHierarchy))
            return;

        // Input + debounce
        if (!spaceToInteract || Keyboard.current == null) return;
        if (Time.unscaledTime < _debounceUntilUnscaled) return;
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;

        _debounceUntilUnscaled = Time.unscaledTime + interactDebounceSeconds;

        UpdateInteractables();
        Interact();
        Debug.Log("[Interactor] SPACE pressed → Interact()");
    }

    private void Interact()
    {
        if (_closestInteractable == null) return;

        var handler  = _closestInteractable.GetComponentInParent<Dialogue.TestHandler>();
        var dlgState = _closestInteractable.GetComponentInParent<Dialogue.DialogueState>();

        if (handler != null && dlgState != null)
        {
            handler.TryStartConversationFromState(dlgState);
            return;
        }

        _closestInteractable.Interact();
    }

    private void OnTriggerEnter(Collider other) => UpdateInteractables();
    private void OnTriggerExit(Collider other)  => UpdateInteractables();

    private void UpdateInteractables()
    {
        _collidersInRange = Physics.OverlapSphere(transform.position, interactRadius);
        _interactablesInRange.Clear();

        if (_collidersInRange == null || _collidersInRange.Length == 0)
        {
            _closestInteractable = null;
            return;
        }

        foreach (var col in _collidersInRange)
        {
            var interactable = col.GetComponent<Interactable>();
            if (interactable != null)
                _interactablesInRange.Add(interactable);
        }

        _closestInteractable = GetClosestInteractable();
    }

    private Interactable GetClosestInteractable()
    {
        if (_interactablesInRange.Count == 0) return null;

        int closestIndex = 0;
        float closestDist = Mathf.Infinity;

        for (int i = 0; i < _interactablesInRange.Count; i++)
        {
            float d = Vector3.Distance(_interactablesInRange[i].transform.position, transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closestIndex = i;
            }
        }

        return _interactablesInRange[closestIndex];
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.6f, 1f, 0.35f);
        Gizmos.DrawSphere(transform.position, interactRadius);
    }
#endif
}
