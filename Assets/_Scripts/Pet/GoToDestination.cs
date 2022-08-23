using System;
using UnityEngine;

namespace Companion
{
    public class GoToDestination : MonoBehaviour
    {
        [SerializeField] private Transform _jumpStart;
        //[SerializeField] private Transform _destination;
        [SerializeField] private float _followSpeedPercent;
        

        private Animator _jumpAnimator;
        private bool _hasJumped;
        
        private void OnEnable()
        {
            _jumpAnimator = GetComponent<Animator>();
            transform.LookAt(_jumpStart);
        }

        private void Update()
        {
            var dist = Vector3.Distance(transform.position, _jumpStart.position);
            if (dist > 1) GoToJumpLocation();
            //transform.position = Vector3.Lerp(transform.position, _jumpStart.position, _followSpeedPercent);
            else if (!_hasJumped)
            {
                _hasJumped = true;
                PlayJumpAnimation();
            }
        }

        private void GoToJumpLocation()
        {
            transform.position = Vector3.Lerp(transform.position, _jumpStart.position, _followSpeedPercent * Time.time);
        }

        private void PlayJumpAnimation()
        {
            _jumpAnimator.applyRootMotion = false;
            _jumpAnimator.SetTrigger("jumpToChair");
        }
        
        

    }
}

