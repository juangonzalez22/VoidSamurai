using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Hit shake")]
    [SerializeField] private float hitStrength = 0.08f;
    [SerializeField] private float hitDuration = 0.08f;

    [Header("Clash shake")]
    [SerializeField] private float clashStrength = 0.18f;
    [SerializeField] private float clashDuration = 0.14f;

    private Vector3 baseLocalPosition;
    private float shakeTimeLeft;
    private float shakeDurationTotal;
    private float shakeStrength;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        baseLocalPosition = transform.localPosition;
    }

    public void ShakeHit()
    {
        Shake(hitStrength, hitDuration);
    }

    public void ShakeClash()
    {
        Shake(clashStrength, clashDuration);
    }

    public void Shake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f)
            return;

        if (shakeTimeLeft <= 0f)
            baseLocalPosition = transform.localPosition;

        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeTimeLeft = Mathf.Max(shakeTimeLeft, duration);
        shakeDurationTotal = Mathf.Max(shakeDurationTotal, duration);
    }

    void LateUpdate()
    {
        if (shakeTimeLeft > 0f)
        {
            shakeTimeLeft -= Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(shakeTimeLeft / Mathf.Max(0.0001f, shakeDurationTotal));
            float currentStrength = shakeStrength * normalizedTime;

            Vector2 offset = Random.insideUnitCircle * currentStrength;
            transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);

            if (shakeTimeLeft <= 0f)
            {
                transform.localPosition = baseLocalPosition;
                shakeStrength = 0f;
                shakeDurationTotal = 0f;
            }
        }
    }
}