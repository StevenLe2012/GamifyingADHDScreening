using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EkonnAnimDriver : MonoBehaviour
{
    [SerializeField] private Animator anim;

    private void Reset()  { anim = GetComponent<Animator>(); }
    private void Awake()  { if (!anim) anim = GetComponent<Animator>(); }
    private void Start()  { EnsureStandingIdle(); } // DEFAULT: stand idle at boot

    // Conversation entry: stand and start talking
    public void StandUpAndTalk()
    {
        if (!anim) return;
        anim.ResetTrigger("SitDown");
        anim.SetTrigger("StandUp");
        anim.SetBool("Talking", true);
    }

    // While it’s Ekonn’s turn but no audio/line yet: stand idle
    public void EnsureStandingIdle()
    {
        if (!anim) return;
        anim.SetBool("Talking", false);
        anim.ResetTrigger("SitDown");
        anim.SetTrigger("StandUp"); // harmless if already standing
    }

    // Player’s turn or conversation ended
    public void SitDown()
    {
        if (!anim) return;
        anim.SetBool("Talking", false);
        anim.ResetTrigger("StandUp");
        anim.SetTrigger("SitDown");
    }

    // Optional: explicitly toggle talking without changing posture
    public void SetTalking(bool on)
    {
        if (!anim) return;
        anim.SetBool("Talking", on);
    }
}
