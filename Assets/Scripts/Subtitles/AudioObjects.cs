using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Object", menuName = "ScriptableObjects/New Audio Object")]
public class AudioObjects : ScriptableObject
{
    public AudioClip clip;
    public string subtitle;
}
