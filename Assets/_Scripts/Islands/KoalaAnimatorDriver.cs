// using UnityEngine;

// [DisallowMultipleComponent]
// public class KoalaAnimatorDriver : MonoBehaviour
// {
//     [SerializeField] private Animator animator;
//     [SerializeField] private string koalaId = "Koala";

//     private bool _warned;

//     private void Reset()
//     {
//         if (!animator)
//             animator = GetComponentInChildren<Animator>(true);
//     }

//     private void Awake()
//     {
//         if (!animator)
//             animator = GetComponentInChildren<Animator>(true);
//     }

//     private bool A()
//     {
//         if (animator) return true;
//         if (!_warned)
//         {
//             Debug.LogWarning($"[KoalaAnimatorDriver] Animator missing on '{name}'.", this);
//             _warned = true;
//         }
//         return false;
//     }

//     // Call this when CPT begins (recommended single entry point)
//     public void OnEnterCPT()
//     {
//         if (!A()) return;

//         animator.SetBool("Talking", false);

//         animator.ResetTrigger("Stand");
//         animator.ResetTrigger("Sit");

//         animator.SetTrigger("Sit");
//         animator.SetBool("CheerOn", true);
//     }

//     public void OnPrepareCPT()
//     {
//         if (!A()) return;

//         animator.ResetTrigger("Stand");
//         animator.ResetTrigger("Sit");
//         animator.SetBool("CheerOn", false);
//         animator.SetBool("Talking", false);
//     }

//     public void OnCountdownStart()
//     {
//         if (!A()) return;

//         animator.ResetTrigger("Stand");
//         animator.SetTrigger("Sit");
//     }

//     public void OnGameBegin()
//     {
//         if (!A()) return;

//         // If you still want OnGameBegin to work, keep it consistent:
//         animator.SetTrigger("Sit");
//         animator.SetBool("CheerOn", true);
//     }

//     public void OnGameEnd()
//     {
//         if (!A()) return;

//         animator.SetBool("CheerOn", false);
//         animator.SetTrigger("Stand");
//     }

//     public void OnResultsContinue()
//     {
//         if (!A()) return;

//         animator.SetBool("Talking", true);
//     }

//     public void StopTalking()
//     {
//         if (!A()) return;

//         animator.SetBool("Talking", false);
//     }

//     public void StandUpAndTalk()
//     {
//         if (!A()) return;

//         animator.ResetTrigger("Sit");
//         animator.SetTrigger("Stand");
//         animator.SetBool("Talking", true);
//     }

//     public void EnsureStandingIdle()
//     {
//         if (!A()) return;

//         animator.ResetTrigger("Sit");
//         animator.SetTrigger("Stand");
//         animator.SetBool("Talking", false);
//         animator.SetBool("CheerOn", false);
//     }

//     public void SitDown()
//     {
//         if (!A()) return;

//         animator.SetBool("Talking", false);
//         animator.ResetTrigger("Stand");
//         animator.SetTrigger("Sit");
//     }

//     public void SetTalking(bool on)
//     {
//         if (!A()) return;
//         animator.SetBool("Talking", on);
//     }

//     public void PlayCheer()
//     {
//         if (!A()) return;

//         animator.SetBool("Talking", false);
//         animator.SetBool("CheerOn", true);

//         animator.ResetTrigger("Sit");
//         animator.ResetTrigger("Stand");
//         animator.SetTrigger("Stand"); // cheer while standing (matches your request)
//     }

//     public void StandAndTalk()
//     {
//         if (!A()) return;

//         animator.SetBool("CheerOn", false);

//         animator.ResetTrigger("Sit");
//         animator.ResetTrigger("Stand");
//         animator.SetTrigger("Stand");
//         animator.SetBool("Talking", true);
//     }
// }

using UnityEngine;

[DisallowMultipleComponent]
public class KoalaAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    [SerializeField] private string triggerStand = "Stand";
    [SerializeField] private string boolTalking  = "Talking";
    [SerializeField] private string boolCheerOn  = "CheerOn";
    [SerializeField] private string triggerHappy  = "Happy";

    private bool _warned;

    private void Reset()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void Awake()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>(true);
    }

    private bool A()
    {
        if (animator) return true;
        if (!_warned)
        {
            Debug.LogWarning($"[KoalaAnimatorDriver] Animator missing on '{name}'.", this);
            _warned = true;
        }
        return false;
    }

    // ---------- Core states ----------
    public void EnsureStandingIdle()
    {
        if (!A()) return;

        animator.SetBool(boolTalking, false);
        animator.SetBool(boolCheerOn, false);
        animator.ResetTrigger(triggerHappy);  // discard any queued Happy trigger
        animator.SetTrigger(triggerStand);
    }

    // ✅ Sustained cheer for entire DP
    public void SetCheer(bool on)
    {
        if (!A()) return;

        animator.SetBool(boolTalking, false);
        animator.SetBool(boolCheerOn, on);

        // Only fire Stand when EXITING cheer. Firing Stand while CheerOn=true
        // causes the trigger to be consumed first and cancel the cheer transition.
        // The caller (ChangeShapes) is responsible for ensuring a standing-idle
        // baseline BEFORE calling SetCheer(true).
        if (!on)
            animator.SetTrigger(triggerStand);
    }

    public void SetTalking(bool on)
    {
        if (!A()) return;

        animator.SetBool(boolTalking, on);

        if (on)
        {
            animator.SetBool(boolCheerOn, false); // talking overrides cheer
        }
        else
        {
            animator.SetTrigger(triggerStand);
        }
    }

    // ---------- Happy reaction ----------
    /// <summary>
    /// Fire the Happy trigger once. The Animator plays the clip and returns to idle
    /// automatically via its exit transition — no manual reset needed.
    /// </summary>
    public void PlayHappy()
    {
        if (!A()) return;
        animator.SetTrigger(triggerHappy);
    }

    // ---------- Back-compat ----------
    public void PlayCheer() => SetCheer(true);  // keep old call working (but now sustained)
    public void StandUpAndTalk() => SetTalking(true);
    public void StandAndTalk() => StandUpAndTalk();
    public void StopTalking() => SetTalking(false);

    public void SitDown() => EnsureStandingIdle();

    public void OnPrepareCPT() => EnsureStandingIdle();
    public void OnCountdownStart() { /* no-op */ }

    public void OnGameBegin() => SetCheer(true);
    public void OnGameEnd() => EnsureStandingIdle();

    public void OnEnterCPT() => EnsureStandingIdle();
    public void OnResultsContinue() => SetTalking(true);
}