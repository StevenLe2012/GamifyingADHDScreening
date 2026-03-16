using UnityEngine;

/// <summary>
/// Bridge so Dialogue options (in ScriptableObject assets) can trigger
/// scene logic for the Dragon ending cutscenes.
/// Hook these methods directly from DialogueOption.actionToTrigger.
/// </summary>
[CreateAssetMenu(menuName = "Moxo/End Game Option Bridge")]
public class EndGameOptionBridge : ScriptableObject
{
    public void PlayEnding1()
    {
        var dragon = FindObjectOfType<DragonLordDialogueHandler>(true);
        if (dragon != null && dragon.IsTalking)
        {
            dragon.StopDialogue();
        }

        var cutscene = FindObjectOfType<EndGameCutscene>(true);
        if (cutscene != null)
        {
            cutscene.PlayEnding1();
        }
        else
        {
            Debug.LogWarning("[EndGameOptionBridge] EndGameCutscene not found in scene.");
        }
    }

    public void PlayEnding2()
    {
        var dragon = FindObjectOfType<DragonLordDialogueHandler>(true);
        if (dragon != null && dragon.IsTalking)
        {
            dragon.StopDialogue();
        }

        var cutscene = FindObjectOfType<EndGameCutscene>(true);
        if (cutscene != null)
        {
            cutscene.PlayEnding2();
        }
        else
        {
            Debug.LogWarning("[EndGameOptionBridge] EndGameCutscene not found in scene.");
        }
    }
}

