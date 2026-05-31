using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FootstepSound : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip footstepClip;

    [Header("Settings")]
    public float stepInterval = 0.35f;
    public float moveThreshold = 0.1f;

    [Header("References")]
    public FighterCombat combat;

    private Rigidbody2D rb;
    private float stepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (combat != null && (combat.IsDead || combat.MatchEnded))
        {
            stepTimer = 0f;
            return;
        }

        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > moveThreshold;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClip == null)
            return;

        audioSource.PlayOneShot(footstepClip);
    }
}