using System;
using System.Collections;
using UnityEngine;

namespace Companion
{
    public class GoToDestination : MonoBehaviour
    {
        [SerializeField] private Transform _jumpStart;
        [SerializeField] private Transform _jumpDestination;
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
            GetPositionAndOrientationForJump();
            yield return null;
            PlayJumpAnimation();
            // this is HARDCODED to make sure Kola ends up in right position
            yield return new WaitForSeconds(1.5f);
            SetEndingPositionAndOrientation();

        }

        private void PlayJumpAnimation()
        {
            _jumpAnimator.SetTrigger("jumpToChair");
        }

        private void GetPositionAndOrientationForJump()
        {
            transform.position = _jumpStart.position;
            Vector3 targetPostition = new Vector3( _jumpDestination.position.x, 
                transform.position.y, 
                _jumpDestination.position.z ) ;
            transform.LookAt( targetPostition ) ;
            transform.Rotate(0f, 180f, 0f);
        }

        private void SetEndingPositionAndOrientation()
        {
            var newLocation = _jumpDestination.position;
            newLocation.y += 0.45f;
            transform.position = newLocation;
            transform.rotation = _jumpDestination.rotation;
        }
        
        

    }
}

