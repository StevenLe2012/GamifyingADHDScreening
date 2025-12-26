using UnityEngine;

[CreateAssetMenu(menuName = "Moxo/Island Data")]
public class IslandData : ScriptableObject
{
    public string islandId;                 // e.g. MAIN, CARDS, BREAD, SKULL, POISON
    public string displayName;
    public Sprite icon;
    public bool startsMoxoOnArrival = true;
}
