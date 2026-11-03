using UnityEngine;
using UnityEngine.AI;

public class AnimacionGuardia : MonoBehaviour
{
    public Animator animator;
    public NavMeshAgent navMeshAgent;

    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.SetFloat("MotionSpeed", 1f);
        animator.SetBool("Grounded", true);
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }
    }
}
