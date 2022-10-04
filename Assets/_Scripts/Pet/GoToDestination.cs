using System;
using System.Collections;
using UnityEngine;

namespace Companion
{
    public class GoToDestination : MonoBehaviour
    {
        [SerializeField] private Transform _jumpStart;
        [SerializeField] private Transform _jumpDestination;
        //[SerializeField] private Transform _destination;
        [SerializeField] private float _followSpeedPercent;
        

        private Animator _jumpAnimator;
        private bool _hasJumped;
        
        private void OnEnable()
        {
            _jumpAnimator = GetComponent<Animator>();
            transform.LookAt(_jumpStart);
        }

        private void Start()
        {
            StartCoroutine(GoToJumpLocation());
        }

        private IEnumerator GoToJumpLocation()
        {
            var dist = Vector3.Distance(transform.position, _jumpStart.position);
            while (dist > .5f)
            {
                dist = Vector3.Distance(transform.position, _jumpStart.position);
                transform.position = Vector3.Lerp(transform.position, _jumpStart.position, _followSpeedPercent * Time.time);
                yield return null;
            }
            getPositionAndOrientationForJump();
            yield return null;
            PlayJumpAnimation();
        }

        private void PlayJumpAnimation()
        {

            //_jumpAnimator.applyRootMotion = false;
            //_jumpAnimator.SetTrigger("jumpToChair");
            Debug.Log("jump");
        }

        private void getPositionAndOrientationForJump()
        {
            transform.position = _jumpStart.position;
            Vector3 targetPostition = new Vector3( _jumpDestination.position.x, 
                transform.position.y, 
                _jumpDestination.position.z ) ;
            transform.LookAt( targetPostition ) ;
            transform.Rotate(0f, 180f, 0f);
        }
        
        

    }
}

