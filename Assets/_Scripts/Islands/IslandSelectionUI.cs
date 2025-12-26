using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IslandSelectionUI : MonoBehaviour
{
    public static IslandSelectionUI I { get; private set; }
    void Awake(){ I = this; Hide(); }

    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject buttonPrefab;

    public void ShowRemaining()
    {
        Clear();
        var remaining = IslandProgress.I.Remaining().ToList();
        if (remaining.Count == 0){ Hide(); return; }

        foreach (var island in remaining)
        {
            var go = Instantiate(buttonPrefab, buttonParent);
            go.name = $"Btn_{island.displayName}";

            var icon = go.GetComponentInChildren<Image>(true);
            if (icon) icon.sprite = island.icon;

            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label) label.text = island.displayName;

            go.GetComponent<Button>().onClick.AddListener(() => OnPick(island));
        }

        Show();
    }

    void OnPick(IslandData island)
    {
        Hide();
        IslandTravelManager.I.TravelTo(island);
    }

    public void Show(){ if (panel) panel.SetActive(true); }
    public void Hide(){ if (panel) panel.SetActive(false); }

    void Clear()
    {
        for (int i = buttonParent.childCount - 1; i >= 0; i--)
            Destroy(buttonParent.GetChild(i).gameObject);
    }
}
