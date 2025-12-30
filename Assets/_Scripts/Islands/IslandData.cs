// IslandData.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Moxo/Island Data")]
public class IslandData : ScriptableObject
{
    [Header("Identity")]
    public string islandId;                 // e.g. MAIN, CARDS, BREAD, SKULL, POISON
    public string displayName;
    public Sprite icon;

    [Header("Intro / Flow")]
    [TextArea] public string introTitle = "Welcome";
    [TextArea] public string introBody  = "Get ready!";
    [Min(0f)]   public float countdownSeconds = 5f;
    public bool startsMoxoOnArrival = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();
    }
#endif
}
