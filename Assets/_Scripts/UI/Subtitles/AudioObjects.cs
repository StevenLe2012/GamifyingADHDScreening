using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Object", menuName = "ScriptableObjects/New Audio Object")]
public class AudioObjects : ScriptableObject
{
    public AudioClip clip;
    [TextArea(2, 5)]
    public string subtitle;
    public string character; // Hami: Add name "Ekonn", "You", etc.
}
