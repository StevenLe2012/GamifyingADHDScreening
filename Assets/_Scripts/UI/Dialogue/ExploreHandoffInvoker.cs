using UnityEngine;

public class ExploreHandoffInvoker : MonoBehaviour
{
    
    [SerializeField] private Dialogue.DialogueHandler handler;
    [SerializeField] private string nextStateKey = "AfterExplore1";
    [SerializeField] private float overrideExploreSeconds = -1f; // -1 = use handler's default

    public void Invoke()
    {
        if (handler == null)
        {
            Debug.LogWarning("[HandoffInvoker] DialogueHandler not set.");
            return;
        }
        Debug.Log("[HandoffInvoker] Invoke() called.");
        handler.HandoffToExploreAndSetState(nextStateKey, overrideExploreSeconds);
    }

}
