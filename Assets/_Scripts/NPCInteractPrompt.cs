using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteractPrompt : MonoBehaviour
{
    [SerializeField] private Dialogue.DialogueHandler handler;
    [SerializeField] private Interactor interactor; // the NPC’s Interactor component
    [SerializeField] private float interactDistance = 2.0f;
    [SerializeField] private Transform player;

    private void Update()
    {
        if (player == null || handler == null || interactor == null) return;

        float d = Vector3.Distance(player.position, transform.position);
        bool inRange = d <= interactDistance;

        // (Optional) show/hide “Press Space/A to talk” prompt here based on:
        // inRange && (GameManager.Instance.State == GameManager.GameState.Explore ? handler.IsNextDialogueUnlocked : true)

        bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool aBtn  = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (inRange && (space || aBtn))
        {
            handler.OnInteract(interactor);
        }
    }
}
