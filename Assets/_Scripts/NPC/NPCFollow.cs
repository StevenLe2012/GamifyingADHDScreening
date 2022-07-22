using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This works great, but I think this will break once
 * we have obstacles with rigid bodies/collisions in the way.
 * Perhaps make it so the NPC can just go through walls?
 */
public class NPCFollow : MonoBehaviour
{
    public GameObject Player;
    public float minDistance;
    public float maxDistance;
    public float followSpeedPercent;

    private float targetDistance;

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(Player.transform);

        // moves towards player
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hitData;
        if (Physics.Raycast(ray, out hitData))
        {
            targetDistance = hitData.distance;
            if (targetDistance > minDistance)
            {
                //gameObject.GetComponent<Animation>().Play("running");  //TODO: Change to correct name
                transform.position = Vector3.Lerp(transform.position, Player.transform.position, followSpeedPercent);
            }
            else
            {
                //gameObject.GetComponent<Animation>().Play("idle");  //TODO: Change to correct name

            }

            // checks for too far from player
            if (targetDistance > maxDistance)
            {
                transform.position = Player.transform.position;
            }
        }


    }
}
