using UnityEngine;

public class GameStateSetter : MonoBehaviour
{
    public void SetExplore()
    {
        if (GameManager.Instance) GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
    }
    public void SetPrepareCPT()
    {
        if (GameManager.Instance) GameManager.Instance.UpdateGameState(GameManager.GameState.PrepareCPT);
    }
    public void SetCPT()
    {
        if (GameManager.Instance) GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
    }
    public void SetNarrative()
    {
        if (GameManager.Instance) GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);
    }
}