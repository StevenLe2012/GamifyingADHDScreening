using UnityEditor;
using UnityEngine;

public class Developer
{
    [MenuItem("Developer/Teleport: Main Island")]
    public static void TeleportToMainIsland()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var pos = new Vector3(20, 37.38f, 0f);
        var rot = Quaternion.Euler(0, 0, 0);
        Debug.Log(rot);
        player.transform.SetPositionAndRotation(pos, rot);
    }
    
    [MenuItem("Developer/Teleport: Card Island")]
    public static void TeleportToCardIsland()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var pos = new Vector3(300, 24.5f, 112.5f);
        var rot = Quaternion.Euler(0, 270, 0);
        player.transform.SetPositionAndRotation(pos, rot);
    }
    
    [MenuItem("Developer/Teleport: Desert Island")]
    public static void TeleportToDesertIsland()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var pos = new Vector3(200, 24f, -285);
        var rot = Quaternion.Euler(0, 270, 0);
        player.transform.SetPositionAndRotation(pos, rot);
    }


}
