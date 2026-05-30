using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FighterCombat : MonoBehaviour
{
    public enum FighterState
    {
        Idle,
        Walking,
        AttackHigh,
        AttackLow,
        BlockHigh,
        BlockLow,
        Hitstun,
        Clash,
        Dead
    }

    [Header("Stats")]
    public int maxHealth = 100;
    public float maxStamina = 100f;

    [SerializeField]
    private int currentHealth;

    [SerializeField]
    private float currentStamina;

    [Header("UI")]
    public Slider healthBar;
    public Slider staminaBar;

    [Header("Stamina Costs")]
    public float moveStaminaDrainPerSecond = 4f;
    public float blockStaminaDrainPerSecond = 6f;
    public float attackStaminaCost = 18f;
    public float staminaRegenPerSecond = 10f;

    [Header("State")]
    public FighterState currentState;

    [Header("Attack Settings")]
    public float attackDuration = 0.3f;

    [Header("Hitstun / Clash")]
    public float hitstunDuration = 0.25f;
    public float clashDuration = 0.25f;

    [Header("Knockback")]
    public float hitKnockback = 5f;
    public float blockedKnockback = 2f;

    [Header("Block Settings")]
    public bool isBlocking;

    [Header("Death")]
    public float deathFadeDuration = 1.25f;

    private FighterController controller;
    private Rigidbody2D rb;
    private Animator animator;

    private FighterCombat opponentCombat;

    private SpriteRenderer[] spriteRenderers;

    private Coroutine deathFadeRoutine;

    private FighterState lastLoggedState;
    private FighterState lastAnimatedState = (FighterState)(-1);

    public int CurrentHealth => currentHealth;
    public float CurrentStamina => currentStamina;

    public bool IsDead => currentState == FighterState.Dead;

    public bool MatchEnded { get; private set; }

    public bool IsAttacking
    {
        get
        {
            return currentState == FighterState.AttackHigh ||
                   currentState == FighterState.AttackLow;
        }
    }

    public bool IsBlocking =>
        currentState == FighterState.BlockHigh ||
        currentState == FighterState.BlockLow;

    void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        controller = GetComponent<FighterController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (controller != null && controller.opponent != null)
        {
            opponentCombat = controller.opponent.GetComponent<FighterCombat>();
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        currentState = FighterState.Idle;
        lastLoggedState = currentState;

        UpdateUI();
    }

    void Update()
    {
        if (!IsDead && !MatchEnded)
        {
            HandleCombatInput();
            DrainBlockStamina();
            RegenerateStamina();
            UpdateState();
        }

        UpdateAnimations();
        UpdateUI();
        LogCurrentState();
    }

    void HandleCombatInput()
    {
        if (currentState == FighterState.Hitstun ||
            currentState == FighterState.Clash)
        {
            return;
        }

        if (gameObject.name == "Player1")
        {
            if (Input.GetKeyDown(KeyCode.F))
                StartHighAttack();

            if (Input.GetKeyDown(KeyCode.G))
                StartLowAttack();

            if (Input.GetKey(KeyCode.R))
            {
                StartHighBlock();
            }
            else if (Input.GetKey(KeyCode.T))
            {
                StartLowBlock();
            }
            else
            {
                StopBlocking();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Keypad1))
                StartHighAttack();

            if (Input.GetKeyDown(KeyCode.Keypad2))
                StartLowAttack();

            if (Input.GetKey(KeyCode.Keypad4))
            {
                StartHighBlock();
            }
            else if (Input.GetKey(KeyCode.Keypad5))
            {
                StartLowBlock();
            }
            else
            {
                StopBlocking();
            }
        }
    }

    void DrainBlockStamina()
    {
        if (!IsBlocking)
            return;

        currentStamina -= blockStaminaDrainPerSecond * Time.deltaTime;

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            StopBlocking();
        }
    }

    void RegenerateStamina()
    {
        if (IsDead || MatchEnded)
            return;

        if (currentState == FighterState.Idle)
        {
            currentStamina = Mathf.Min(
                maxStamina,
                currentStamina + staminaRegenPerSecond * Time.deltaTime
            );
        }
    }

    void UpdateState()
    {
        if (currentState == FighterState.AttackHigh ||
            currentState == FighterState.AttackLow ||
            currentState == FighterState.BlockHigh ||
            currentState == FighterState.BlockLow ||
            currentState == FighterState.Hitstun ||
            currentState == FighterState.Clash ||
            currentState == FighterState.Dead)
        {
            return;
        }

        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            currentState = FighterState.Walking;
        else
            currentState = FighterState.Idle;
    }

    void UpdateAnimations()
    {
        if (animator == null)
            return;

        if (currentState == lastAnimatedState)
            return;

        lastAnimatedState = currentState;

        switch (currentState)
        {
            case FighterState.Idle:
                PlayLoop("Idle");
                break;

            case FighterState.Walking:
                PlayLoop("Run");
                break;

            case FighterState.AttackHigh:
            case FighterState.AttackLow:
                PlayTimed("Attack", attackDuration);
                break;

            case FighterState.BlockHigh:
            case FighterState.BlockLow:
                PlayLoop("Block");
                break;

            case FighterState.Hitstun:
                PlayTimed("Hurt", hitstunDuration);
                break;

            case FighterState.Clash:
                PlayTimed("Hurt", clashDuration);
                break;

            case FighterState.Dead:
                PlayLoop("Hurt");
                break;
        }
    }

    void PlayLoop(string clipName)
    {
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.Play(clipName, 0, 0f);
    }

    void PlayTimed(string clipName, float desiredDuration)
    {
        if (animator == null)
            return;

        float clipLength = GetClipLength(clipName);

        if (clipLength <= 0f)
            clipLength = desiredDuration;

        animator.speed = clipLength / Mathf.Max(0.01f, desiredDuration);
        animator.Play(clipName, 0, 0f);
    }

    float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return -1f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == clipName)
                return clip.length;
        }

        return -1f;
    }

    public bool TryConsumeMovementStamina(float deltaTime)
    {
        if (IsDead || MatchEnded)
            return false;

        float cost = moveStaminaDrainPerSecond * Mathf.Max(0f, deltaTime);

        if (currentStamina <= 0f)
            return false;

        currentStamina -= cost;

        if (currentStamina < 0f)
            currentStamina = 0f;

        return currentStamina > 0f;
    }

    bool TrySpendAttackStamina()
    {
        if (IsDead || MatchEnded)
            return false;

        if (currentStamina < attackStaminaCost)
            return false;

        currentStamina -= attackStaminaCost;
        return true;
    }

    public void StartHighAttack()
    {
        if (IsDead || MatchEnded)
            return;

        if (currentState == FighterState.Hitstun ||
            currentState == FighterState.Clash)
        {
            return;
        }

        if (!TrySpendAttackStamina())
            return;

        StopBlocking();

        currentState = FighterState.AttackHigh;

        CancelInvoke(nameof(EndAttack));
        Invoke(nameof(EndAttack), attackDuration);
    }

    public void StartLowAttack()
    {
        if (IsDead || MatchEnded)
            return;

        if (currentState == FighterState.Hitstun ||
            currentState == FighterState.Clash)
        {
            return;
        }

        if (!TrySpendAttackStamina())
            return;

        StopBlocking();

        currentState = FighterState.AttackLow;

        CancelInvoke(nameof(EndAttack));
        Invoke(nameof(EndAttack), attackDuration);
    }

    void EndAttack()
    {
        if (IsDead || MatchEnded)
            return;

        currentState = FighterState.Idle;
        animator.speed = 1f;
    }

    void StartHighBlock()
    {
        if (IsDead || MatchEnded)
            return;

        if (currentStamina <= 0f)
            return;

        isBlocking = true;
        currentState = FighterState.BlockHigh;
    }

    void StartLowBlock()
    {
        if (IsDead || MatchEnded)
            return;

        if (currentStamina <= 0f)
            return;

        isBlocking = true;
        currentState = FighterState.BlockLow;
    }

    void StopBlocking()
    {
        if (IsDead || MatchEnded)
            return;

        isBlocking = false;

        if (currentState == FighterState.BlockHigh ||
            currentState == FighterState.BlockLow)
        {
            currentState = FighterState.Idle;
        }
    }

    public void ReceiveHit(bool blocked, Transform attacker)
    {
        if (IsDead || MatchEnded)
            return;

        if (blocked)
        {
            currentState = currentState == FighterState.BlockLow
                ? FighterState.BlockLow
                : FighterState.BlockHigh;

            ApplyKnockback(attacker, blockedKnockback);
            return;
        }

        currentHealth -= 10;
        if (currentHealth < 0)
            currentHealth = 0;

        if (currentHealth <= 0)
        {
            Die(attacker);
            return;
        }

        currentState = FighterState.Hitstun;

        ApplyKnockback(attacker, hitKnockback);

        CancelInvoke(nameof(EndHitstun));
        Invoke(nameof(EndHitstun), hitstunDuration);
    }

    public void StartClash(Transform attacker)
    {
        if (IsDead || MatchEnded)
            return;

        currentState = FighterState.Clash;

        ApplyKnockback(attacker, hitKnockback);

        CancelInvoke(nameof(EndClash));
        Invoke(nameof(EndClash), clashDuration);
    }

    void EndHitstun()
    {
        if (IsDead || MatchEnded)
            return;

        currentState = FighterState.Idle;
        animator.speed = 1f;
    }

    void EndClash()
    {
        if (IsDead || MatchEnded)
            return;

        currentState = FighterState.Idle;
        animator.speed = 1f;
    }

    void Die(Transform attacker)
    {
        currentHealth = 0;
        currentStamina = 0f;

        CancelInvoke(nameof(EndAttack));
        CancelInvoke(nameof(EndHitstun));
        CancelInvoke(nameof(EndClash));

        StopBlocking();

        currentState = FighterState.Dead;
        animator.speed = 1f;

        rb.linearVelocity = Vector2.zero;

        if (attacker != null)
        {
            ApplyKnockback(attacker, hitKnockback);
        }

        if (deathFadeRoutine != null)
            StopCoroutine(deathFadeRoutine);

        deathFadeRoutine = StartCoroutine(FadeOutAndLoseColor());

        if (opponentCombat != null)
        {
            opponentCombat.OnMatchWon();
        }
    }

    public void OnMatchWon()
    {
        if (IsDead)
            return;

        MatchEnded = true;

        CancelInvoke(nameof(EndAttack));
        CancelInvoke(nameof(EndHitstun));
        CancelInvoke(nameof(EndClash));

        StopBlocking();

        currentState = FighterState.Idle;
        rb.linearVelocity = Vector2.zero;
        animator.speed = 1f;
    }

    IEnumerator FadeOutAndLoseColor()
    {
        float elapsed = 0f;

        Color[] startColors = new Color[spriteRenderers.Length];
        Color[] targetColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            startColors[i] = spriteRenderers[i] != null
                ? spriteRenderers[i].color
                : Color.white;

            float gray = (startColors[i].r + startColors[i].g + startColors[i].b) / 3f;
            targetColors[i] = new Color(gray, gray, gray, 0f);
        }

        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathFadeDuration);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                    continue;

                spriteRenderers[i].color = Color.Lerp(startColors[i], targetColors[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].color = targetColors[i];
        }
    }

    void ApplyKnockback(Transform attacker, float force)
    {
        if (attacker == null || rb == null)
            return;

        Vector2 direction = (transform.position - attacker.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * force, 0f);
    }

    void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }
    }

    void LogCurrentState()
    {
        if (currentState == lastLoggedState)
            return;

        lastLoggedState = currentState;

        switch (currentState)
        {
            case FighterState.Idle:
                Debug.Log(name + " is idle");
                break;
            case FighterState.Walking:
                Debug.Log(name + " is moving");
                break;
            case FighterState.AttackHigh:
                Debug.Log(name + " is attacking high");
                break;
            case FighterState.AttackLow:
                Debug.Log(name + " is attacking low");
                break;
            case FighterState.BlockHigh:
                Debug.Log(name + " is blocking high");
                break;
            case FighterState.BlockLow:
                Debug.Log(name + " is blocking low");
                break;
            case FighterState.Hitstun:
                Debug.Log(name + " is in hitstun");
                break;
            case FighterState.Clash:
                Debug.Log(name + " is clashing");
                break;
            case FighterState.Dead:
                Debug.Log(name + " is dead");
                break;
        }
    }
}