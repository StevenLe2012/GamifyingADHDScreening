using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StickOptionSelector : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference moveStick; // Vector2 (rightStick)
    [SerializeField] private InputActionReference submit;    // Button (A / buttonSouth)

    [Header("Selection")]
    [SerializeField] private bool wrap = true;
    [SerializeField] private float horizontalDeadzone = 0.35f;
    [SerializeField] private float repeatDelay = 0.25f;
    [SerializeField] private bool selectFirstOnSync = true;

    private readonly List<Button> _activeButtons = new();
    private int _index = -1;
    private bool _canStep = true;
    private float _repeatTimer = 0f;

    private void OnEnable()
    {
        if (moveStick) moveStick.action.Enable();
        if (submit) submit.action.Enable();
        TryAutoAddFx();
        SyncButtons();
        if (selectFirstOnSync) SelectIndex(0);
    }

    private void OnDisable()
    {
        if (moveStick) moveStick.action.Disable();
        if (submit) submit.action.Disable();
        ClearHover();
        _index = -1;
    }

    public void SyncAndSelectFirst()
    {
        TryAutoAddFx();
        SyncButtons();
        if (selectFirstOnSync) SelectIndex(0);
    }

    public void SyncButtons()
    {
        _activeButtons.Clear();
        GetComponentsInChildren<Button>(false, _activeButtons);
        _activeButtons.RemoveAll(b => !b.gameObject.activeInHierarchy || !b.interactable);
        _index = Mathf.Clamp(_index, _activeButtons.Count > 0 ? 0 : -1, _activeButtons.Count - 1);
        UpdateHover();
    }

    private void Update()
    {
        if (_activeButtons.Count == 0) return;

        var x = moveStick ? moveStick.action.ReadValue<Vector2>().x : 0f;

        if (Mathf.Abs(x) > horizontalDeadzone)
        {
            if (_canStep || _repeatTimer <= 0f)
            {
                Step(x > 0f ? +1 : -1);
                _canStep = false;
                _repeatTimer = repeatDelay;
            }
            else _repeatTimer -= Time.unscaledDeltaTime;
        }
        else
        {
            _canStep = true;
            _repeatTimer = 0f;
        }

        if (submit && submit.action.WasPressedThisFrame())
            ActivateCurrent();
    }

    private void Step(int dir)
    {
        if (_activeButtons.Count == 0) return;
        var newIndex = _index + dir;
        if (wrap)
        {
            if (newIndex < 0) newIndex = _activeButtons.Count - 1;
            else if (newIndex >= _activeButtons.Count) newIndex = 0;
        }
        else newIndex = Mathf.Clamp(newIndex, 0, _activeButtons.Count - 1);
        SelectIndex(newIndex);
    }

    private void SelectIndex(int newIndex)
    {
        if (_activeButtons.Count == 0) { _index = -1; return; }
        _index = Mathf.Clamp(newIndex, 0, _activeButtons.Count - 1);
        UpdateHover();
    }

    private void UpdateHover()
    {
        for (int i = 0; i < _activeButtons.Count; i++)
            _activeButtons[i].GetComponent<HoverableButtonFx>()?.SetHovered(i == _index);
    }

    private void ClearHover()
    {
        foreach (var b in _activeButtons)
            b.GetComponent<HoverableButtonFx>()?.SetHovered(false);
    }

    private void ActivateCurrent()
    {
        if (_index < 0 || _index >= _activeButtons.Count) return;
        _activeButtons[_index].onClick?.Invoke();
    }

    private void TryAutoAddFx()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
            if (!b.TryGetComponent<HoverableButtonFx>(out _))
                b.gameObject.AddComponent<HoverableButtonFx>();
    }
}
