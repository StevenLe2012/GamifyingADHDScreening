using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class IslandSelectionUI : MonoBehaviour
{
    public static IslandSelectionUI I { get; private set; }
    void Awake(){ I = this; Hide(); }

    [Header("Wiring")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Highlight")]
    [SerializeField] private Color selectedTint = new Color(1.15f, 1.15f, 1.15f, 1f);
    [SerializeField] private Color normalTint   = Color.white;
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float normalScale   = 1.00f;

    [Header("Grid Navigation")]
    [Tooltip("If GridLayoutGroup has FixedColumnCount/FixedRowCount, those win. Otherwise we use this.")]
    [SerializeField] private int columnsOverride = 4;
    [SerializeField] private bool wrapHorizontal = true;
    [SerializeField] private bool wrapVertical   = false;

    private readonly List<Button> _buttons = new();
    private int _index = 0;

    // === YOUR requested ShowRemaining implementation (+ small additions) ===
    public void ShowRemaining()
    {
        Clear();

        var remaining = IslandProgress.I.Remaining().ToList();
        if (remaining.Count == 0) { Hide(); return; }

        foreach (var island in remaining)
        {
            // parent = buttonParent, keep local transform
            var go = Instantiate(buttonPrefab, buttonParent, false);
            go.name = $"Btn_{island.displayName}";

            // 🔒 Ensure the clone is ON and sane
            if (!go.activeSelf) go.SetActive(true);
            go.transform.localScale = Vector3.one;

            // Optional: if your prefab has a CanvasGroup that’s disabled
            var cg = go.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

            // Hook up visuals
            var icon  = go.GetComponentInChildren<Image>(true);
            if (icon) { icon.enabled = true; icon.sprite = island.icon; }

            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label) { label.enabled = true; label.text = island.displayName; }

            // Button click
            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPick(island));
                _buttons.Add(btn); // <-- track for navigation/highlight
            }
        }

        // ensure a valid selection + visual state
        _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _buttons.Count - 1));
        ApplyHighlight();

        Show();
    }
    // =====================================================================

    void Update()
    {
        if (!panel || !panel.activeInHierarchy) return;
        var kb = Keyboard.current; if (kb == null) return;

        // WASD in grid
        if (kb.wKey.wasPressedThisFrame) MoveGrid(0, -1); // up
        if (kb.sKey.wasPressedThisFrame) MoveGrid(0, +1); // down
        if (kb.aKey.wasPressedThisFrame) MoveGrid(-1, 0); // left
        if (kb.dKey.wasPressedThisFrame) MoveGrid(+1, 0); // right

        // Activate
        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            Activate();
    }

    // ---------- Grid nav core ----------
    private void MoveGrid(int dx, int dy)
    {
        if (_buttons.Count == 0) return;

        (int cols, int rows) = GetGridSize(_buttons.Count);
        (int col, int row) = IndexToColRow(_index, cols);

        int newCol = col + dx;
        int newRow = row + dy;

        // Horizontal wrap/clamp
        if (wrapHorizontal)
            newCol = (newCol % cols + cols) % cols;
        else
            newCol = Mathf.Clamp(newCol, 0, cols - 1);

        // Vertical wrap/clamp
        if (wrapVertical)
            newRow = (newRow % rows + rows) % rows;
        else
            newRow = Mathf.Clamp(newRow, 0, rows - 1);

        int newIndex = ColRowToIndexClamped(newCol, newRow, cols, rows, _buttons.Count);
        _index = newIndex;
        ApplyHighlight();
    }

    private (int cols, int rows) GetGridSize(int count)
    {
        int cols = columnsOverride;
        int rows;

        var grid = buttonParent ? buttonParent.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
                cols = grid.constraintCount;

            if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount && grid.constraintCount > 0)
            {
                rows = grid.constraintCount;
                cols = Mathf.CeilToInt((float)count / rows);
                return (Mathf.Max(1, cols), Mathf.Max(1, rows));
            }
        }

        cols = Mathf.Max(1, cols);
        rows = Mathf.CeilToInt((float)count / cols);
        rows = Mathf.Max(1, rows);
        return (cols, rows);
    }

    private static (int col, int row) IndexToColRow(int index, int cols)
    {
        int row = index / cols;
        int col = index % cols;
        return (col, row);
    }

    private static int ColRowToIndex(int col, int row, int cols) => row * cols + col;

    private static int ColRowToIndexClamped(int col, int row, int cols, int rows, int count)
    {
        int i = ColRowToIndex(col, row, cols);
        if (i < count) return i;

        int lastRow = Mathf.CeilToInt((float)count / cols) - 1;
        row = Mathf.Min(row, lastRow);
        i = ColRowToIndex(col, row, cols);
        if (i < count) return i;

        while (col > 0)
        {
            col--;
            i = ColRowToIndex(col, row, cols);
            if (i < count) return i;
        }
        return count - 1;
    }

    // ---------- Selection / highlight ----------

    private void ApplyHighlight()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            var btn = _buttons[i];
            if (!btn) continue; // <-- guard

            var rootImg = btn.GetComponent<Image>();
            if (rootImg) rootImg.color = (i == _index) ? selectedTint : normalTint;
            else
            {
                foreach (var img in btn.GetComponentsInChildren<Image>(true))
                    if (img) img.color = (i == _index) ? selectedTint : normalTint;
            }

            if (btn.transform) // <-- guard
                btn.transform.localScale = (i == _index) ? Vector3.one * selectedScale : Vector3.one * normalScale;

            var highlight = btn ? btn.transform.Find("Highlight")?.GetComponent<Image>() : null;
            if (highlight) highlight.enabled = (i == _index);
        }
    }

    private void Activate()
    {
        if (_index < 0 || _index >= _buttons.Count) return;
        var btn = _buttons[_index];
        if (btn && btn.interactable)
            btn.onClick.Invoke();
    }

    // ---------- Flow ----------
    void OnPick(IslandData island)
    {
        // Freeze UI immediately (prevents Update / nav / highlight from touching destroyed children)
        var go = panel ? panel : gameObject;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg) { cg.interactable = false; cg.blocksRaycasts = false; }
        enabled = false;

        // Keep it visible just for this frame (avoid destroying while click handler is still running).
        StartCoroutine(DoTravel(island));
    }

    System.Collections.IEnumerator DoTravel(IslandData island)
    {
        yield return null;   // defer to end of frame
        Hide();              // now it’s safe to hide/disable/destroy children
        IslandTravelManager.I.TravelTo(island);
    }

    public void Show()
    {
        var go = panel ? panel : gameObject;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
        go.SetActive(true);
    }

    public void Hide()
    {
        var go = panel ? panel : gameObject;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
        go.SetActive(false);

        // Make absolutely sure no one uses stale references
        _buttons.Clear();
    }

    
    #if UNITY_EDITOR
    void OnValidate()
    {
        if (buttonPrefab != null && buttonPrefab.scene.IsValid())
        {
            Debug.LogError(
                "[IslandSelectionUI] 'buttonPrefab' is a SCENE object. " +
                "Please assign a Project prefab asset (drag IslandCard from Project, not Hierarchy).",
                this
            );
        }
    }
    #endif
        
    void Clear()
    {
        _buttons.Clear();
        for (int i = buttonParent.childCount - 1; i >= 0; i--)
            Destroy(buttonParent.GetChild(i).gameObject);
    }


}
