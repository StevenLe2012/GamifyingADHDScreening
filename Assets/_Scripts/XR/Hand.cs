using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Hand : MonoBehaviour {

    [SerializeField] private float speed = 10f;
    [SerializeField] private bool useTriggerAndGrip;

    private Animator _animator;
    private SkinnedMeshRenderer _mesh;
    private float _gripCurrent;
    private float _gripTarget;
    private float _triggerCurrent;
    private float _triggerTarget;
    private int _animatorGripID;
    private int _animatorTriggerID;
    private int _animatorFullHandID;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _mesh = GetComponentInChildren<SkinnedMeshRenderer>();

        // hash to prevent string lookups in update (hash is faster!)
        _animatorGripID = Animator.StringToHash("Grip");
        _animatorTriggerID = Animator.StringToHash("Trigger");
        _animatorFullHandID = Animator.StringToHash("FullHand");
    }

    private void Update()
    {
        AnimateHand();
    }

    public void SetGrip(float v)
    {
        _gripTarget = v;
    }

    public void SetTrigger(float v)
    {
        _triggerTarget = v;
    }

    private void AnimateHand()
    {
        if (useTriggerAndGrip)
        {
            // transition the grip fingers slowly towards target per frame
            if (_gripCurrent != _gripTarget)
            {
                _gripCurrent = Mathf.MoveTowards(_gripCurrent, _gripTarget, Time.deltaTime * speed);
                _animator.SetFloat(_animatorGripID, _gripCurrent);
            }

            // transition the trigger fingers slowly towards target per frame
            if (_triggerCurrent != _triggerTarget)
            {
                _triggerCurrent = Mathf.MoveTowards(_triggerCurrent, _triggerTarget, Time.deltaTime * speed);
                _animator.SetFloat(_animatorTriggerID, _triggerCurrent);
            }
        }
        else
        {
            // transitions the full hand (all fingers) slowly towards target per frame
            if (_triggerCurrent != _triggerTarget)
            {
                _triggerCurrent = Mathf.MoveTowards(_triggerCurrent, _triggerTarget, Time.deltaTime * speed);
                _animator.SetFloat(_animatorFullHandID, _triggerCurrent);
            }
         }


    }

    public void ToggleVisibility()
    {
        _mesh.enabled = !_mesh.enabled;
    }
}
