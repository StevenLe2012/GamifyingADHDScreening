using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowExactly : MonoBehaviour
{
    [SerializeField] private Transform followTransform;

    private Vector3 _origin;
    private Vector3 _followTransformOrigin;

    // Update is called once per frame
    private void Start()
    {
        _origin = transform.position;
        _followTransformOrigin = followTransform.localPosition;
    }
    void Update()
    {
        print(followTransform.localPosition);
        transform.position = followTransform.localPosition;
    }
}
