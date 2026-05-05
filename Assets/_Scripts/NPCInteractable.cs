using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private Dialogue.DialogueHandler dialogueHandler;

    public void Interact()
    {
        if (dialogueHandler == null)
        {
            Debug.LogWarning($"No DialogueHandler on {name}");
            return;
        }
        var interactor = FindObjectOfType<Interactor>(); // simple lookup
        dialogueHandler.OnInteract(interactor);
    }
}
