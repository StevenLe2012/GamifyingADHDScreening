using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFollowTransform : MonoBehaviour
{
    [SerializeField] private Transform lookAt;
    [SerializeField] private Transform transformToFollow;
    [SerializeField] private float offsetByUnit;
    [SerializeField] private float duration;

    private Transform _thisTransform;
    private void Awake()
    {
        _thisTransform = transform;
    }

    void Update()
    {
        _thisTransform.LookAt(lookAt, Vector3.up);
        _thisTransform.Rotate(0f, 180f, 0f);

        var oldLocation = _thisTransform.position;
        var newLocation = transformToFollow.position;
        _thisTransform.position = Vector3.Lerp(oldLocation, newLocation, Time.deltaTime / duration);


        /*
        Vector3 horizontalForward = new Vector3(transformToFollow.position.x + transformToFollow.transform.forward.x,
                                            transformToFollow.position.y,
                                            transformToFollow.position.z + transformToFollow.forward.z
                                        ).normalized * offsetByUnit;

        transform.position = horizontalForward;

        var oldRotation = _thisTransform.rotation;
        var newRotation = transformToFollow.rotation;
        _thisTransform.rotation = Quaternion.Lerp(oldRotation, newRotation, Time.deltaTime / duration);
        */


    }
}
