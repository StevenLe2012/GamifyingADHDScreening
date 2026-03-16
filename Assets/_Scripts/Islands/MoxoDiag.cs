using System.Linq;
using UnityEngine;

public class MoxoDiag : MonoBehaviour
{
    [Tooltip("Press F2 in Play mode to dump the full MOXO/Island state to Console.")]
    public KeyCode dumpKey = KeyCode.F2;

    void Awake()
    {
        Debug.unityLogger.logEnabled = true;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        Debug.Log("[Diag] MoxoDiag Awake()");
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
    }

    void Update()
    {
        if (Input.GetKeyDown(dumpKey)) Dump();
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        // If you want to echo logs to a file later, you can hook here.
    }

    public static void Dump()
    {
        var gm = GameManager.Instance;
        Debug.Log($"[Diag] GameState={gm?.State}  ParticipantId={gm?.ParticipantId}");

        // Active Moxo managers
        var allMgrs = Object.FindObjectsOfType<MoxoCPT.MoxoCPTManager>(true);
        var activeMgr = allMgrs.FirstOrDefault(m => m.gameObject.activeInHierarchy);
        Debug.Log($"[Diag] Moxo managers total={allMgrs.Length}, active={(activeMgr?activeMgr.name:"NONE")}, Instance={(MoxoCPT.MoxoCPTManager.Instance? MoxoCPT.MoxoCPTManager.Instance.name : "null")}");

        // ChangeShapes on the active manager’s hierarchy
        var cs = activeMgr ? activeMgr.GetComponentInChildren<MoxoCPT.ChangeShapes>(true) : null;
        Debug.Log($"[Diag] ChangeShapes in active rig={(cs? cs.gameObject.name : "NONE")} enabled={(cs && cs.enabled)}");

        // Intro + Countdown
        var intro = Object.FindObjectsOfType<IntroScreen>(true).FirstOrDefault();
        Debug.Log($"[Diag] IntroScreen={(intro? intro.name : "NONE")} active={(intro && intro.gameObject.activeInHierarchy)}");

        var countdown = Object.FindObjectsOfType<ExploreHintUI>(true).FirstOrDefault();
        Debug.Log($"[Diag] Countdown={(countdown? countdown.name : "NONE")} active={(countdown && countdown.gameObject.activeInHierarchy)}");

        // Travel manager + current island
        var travel = IslandTravelManager.I;
        Debug.Log($"[Diag] TravelMgr={(travel? travel.name : "NONE")} CurrentIsland={(travel? travel.CurrentIsland?.displayName : "null")}");

        // Player root
        var player = travel ? travel.GetType().GetMethod("GetPlayerRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(travel, null) as Transform : null;
        Debug.Log($"[Diag] PlayerRoot={(player? player.name : "NONE")} pos={(player? player.position.ToString() : "n/a")}");
    }
}
