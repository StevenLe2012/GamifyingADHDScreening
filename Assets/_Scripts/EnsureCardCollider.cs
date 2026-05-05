using UnityEngine;

[DisallowMultipleComponent]
public class EnsureCardCollider : MonoBehaviour
{
    [SerializeField] bool useChildCollider = true;
    [SerializeField] string colliderChildName = "CardCollider";
    [SerializeField] Vector3 size = new Vector3(1f, 0.02f, 1.5f);
    [SerializeField] bool isTrigger = true;

    void Reset() { Setup(); }
    void Awake() { Setup(); }

    void Setup()
    {
        if (useChildCollider)
        {
            var child = transform.Find(colliderChildName);
            if (!child)
            {
                child = new GameObject(colliderChildName).transform;
                child.SetParent(transform, false);
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
            }
            var bc = child.GetComponent<BoxCollider>() ?? child.gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = isTrigger;
            bc.size = size;
        }
        else
        {
            var bc = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = isTrigger;
            // With giant non-uniform scales, you may still need to tweak bc.size
        }
    }
}
