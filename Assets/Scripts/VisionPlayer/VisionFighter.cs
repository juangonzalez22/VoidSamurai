using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VisionFighter : MonoBehaviour
{
    // =========================================================================
    // State enum
    // =========================================================================

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

    // =========================================================================
    // Inspector
    // =========================================================================

    [Header("Stats")]
    public int   maxHealth  = 100;
    public float maxStamina = 100f;

    [SerializeField] private int   currentHealth;
    [SerializeField] private float currentStamina;

    [Header("UI")]
    public Slider healthBar;
    public Slider staminaBar;

    [Header("Stamina Costs")]
    public float moveStaminaDrainPerSecond  = 4f;
    public float blockStaminaDrainPerSecond = 6f;
    public float attackStaminaCost          = 18f;
    public float staminaRegenPerSecond      = 10f;

    [Header("State")]
    public FighterState currentState;

    [Header("Attack Settings")]
    public float attackDuration = 0.3f;

    [Header("Hitstun / Clash")]
    public float hitstunDuration = 0.25f;
    public float clashDuration   = 0.25f;

    [Header("Knockback")]
    public float hitKnockback     = 5f;
    public float blockedKnockback = 2f;

    [Header("Block Settings")]
    public bool isBlocking;

    [Header("Death")]
    public float deathFadeDuration = 1.25f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   swordClip;
    public AudioClip   damageClip;
    public AudioClip   clashClip;
    public AudioClip   deathClip;

    // =========================================================================
    // Public properties
    // =========================================================================

    public int   CurrentHealth  => currentHealth;
    public float CurrentStamina => currentStamina;
    public bool  IsDead         => currentState == FighterState.Dead;
    public bool  MatchEnded     { get; private set; }

    public bool IsAttacking =>
        currentState == FighterState.AttackHigh ||
        currentState == FighterState.AttackLow;

    public bool IsBlocking =>
        currentState == FighterState.BlockHigh ||
        currentState == FighterState.BlockLow;

    // =========================================================================
    // Private fields
    // =========================================================================

    private InputManager      _input;
    private FighterController _controller;
    private Rigidbody2D       _rb;
    private Animator          _animator;
    private VisionFighter     _opponentCombat;
    private SpriteRenderer[]  _spriteRenderers;
    private Coroutine         _deathFadeRoutine;

    private FighterState _lastLoggedState;
    private FighterState _lastAnimatedState = (FighterState)(-1);

    // Evita que dos llamadas StartClash en el mismo frame disparen dos shakes
    private static int _lastClashShakeFrame = -1;

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    private void Start()
    {
        currentHealth  = maxHealth;
        currentStamina = maxStamina;

        _input      = GetComponent<InputManager>();
        _controller = GetComponent<FighterController>();
        _rb         = GetComponent<Rigidbody2D>();
        _animator   = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (_controller != null && _controller.opponent != null)
            _opponentCombat = _controller.opponent.GetComponent<VisionFighter>();

        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        currentState     = FighterState.Idle;
        _lastLoggedState = currentState;

        UpdateUI();
    }

    private void Update()
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

    // =========================================================================
    // Combat input
    // =========================================================================

    private void HandleCombatInput()
    {
        if (currentState == FighterState.Hitstun ||
            currentState == FighterState.Clash)
            return;

        bool visionHandled = HandleVisionCombatInput();
        if (!visionHandled)
            HandleKeyboardCombatInput();
    }

    private bool HandleVisionCombatInput()
    {
        if (_input == null) return false;

        bool consumed = false;

        // AttackRight → HighAttack
        if (_input.AttackRight)
        {
            _input.ConsumeAttackRight();   
            StartHighAttack();
            consumed = true;
        }

        // AttackLeft → LowAttack
        if (_input.AttackLeft)
        {
            _input.ConsumeAttackLeft();  
            StartLowAttack();
            consumed = true;
        }

        return consumed;
    }

    private void HandleKeyboardCombatInput()
    {
        if (gameObject.name == "Player1")
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton2)) StartHighAttack(); // X
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) StartLowAttack();  // A

            if      (Input.GetKey(KeyCode.JoystickButton3)) StartHighBlock(); // Y
            else if (Input.GetKey(KeyCode.JoystickButton1)) StartLowBlock();  // B
            else                                            StopBlocking();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Keypad1)) StartHighAttack();
            if (Input.GetKeyDown(KeyCode.Keypad2)) StartLowAttack();

            if      (Input.GetKey(KeyCode.Keypad4)) StartHighBlock();
            else if (Input.GetKey(KeyCode.Keypad5)) StartLowBlock();
            else                                    StopBlocking();
        }
    }

    // =========================================================================
    // Stamina
    // =========================================================================

    private void DrainBlockStamina()
    {
        if (!IsBlocking) return;

        currentStamina -= blockStaminaDrainPerSecond * Time.deltaTime;
        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            StopBlocking();
        }
    }

    private void RegenerateStamina()
    {
        if (IsDead || MatchEnded) return;

        if (currentState == FighterState.Idle)
        {
            currentStamina = Mathf.Min(
                maxStamina,
                currentStamina + staminaRegenPerSecond * Time.deltaTime);
        }
    }

    public bool TryConsumeMovementStamina(float deltaTime)
    {
        if (IsDead || MatchEnded) return false;

        float cost = moveStaminaDrainPerSecond * Mathf.Max(0f, deltaTime);
        if (currentStamina <= 0f) return false;

        currentStamina -= cost;
        if (currentStamina < 0f) currentStamina = 0f;
        return currentStamina > 0f;
    }

    private bool TrySpendAttackStamina()
    {
        if (IsDead || MatchEnded) return false;
        if (currentStamina < attackStaminaCost) return false;

        currentStamina -= attackStaminaCost;
        return true;
    }

    // =========================================================================
    // State transitions
    // =========================================================================

    private void UpdateState()
    {
        switch (currentState)
        {
            case FighterState.AttackHigh:
            case FighterState.AttackLow:
            case FighterState.BlockHigh:
            case FighterState.BlockLow:
            case FighterState.Hitstun:
            case FighterState.Clash:
            case FighterState.Dead:
                return;
        }

        currentState = Mathf.Abs(_rb.linearVelocity.x) > 0.1f
            ? FighterState.Walking
            : FighterState.Idle;
    }

    // =========================================================================
    // Attack / Block
    // =========================================================================

    public void StartHighAttack()
    {
        if (IsDead || MatchEnded) return;
        if (currentState == FighterState.Hitstun || currentState == FighterState.Clash) return;
        if (!TrySpendAttackStamina()) return;

        StopBlocking();
        currentState = FighterState.AttackHigh;
        PlayClip(swordClip);

        CancelInvoke(nameof(EndAttack));
        Invoke(nameof(EndAttack), attackDuration);

        Debug.Log($"[VisionFighter] {name} → HIGH ATTACK");
    }

    public void StartLowAttack()
    {
        if (IsDead || MatchEnded) return;
        if (currentState == FighterState.Hitstun || currentState == FighterState.Clash) return;
        if (!TrySpendAttackStamina()) return;

        StopBlocking();
        currentState = FighterState.AttackLow;
        PlayClip(swordClip);

        CancelInvoke(nameof(EndAttack));
        Invoke(nameof(EndAttack), attackDuration);

        Debug.Log($"[VisionFighter] {name} → LOW ATTACK");
    }

    private void EndAttack()
    {
        if (IsDead || MatchEnded) return;
        currentState    = FighterState.Idle;
        _animator.speed = 1f;
    }

    private void StartHighBlock()
    {
        if (IsDead || MatchEnded) return;
        if (currentStamina <= 0f) return;

        isBlocking   = true;
        currentState = FighterState.BlockHigh;
    }

    private void StartLowBlock()
    {
        if (IsDead || MatchEnded) return;
        if (currentStamina <= 0f) return;

        isBlocking   = true;
        currentState = FighterState.BlockLow;
    }

    private void StopBlocking()
    {
        if (IsDead || MatchEnded) return;

        isBlocking = false;
        if (currentState == FighterState.BlockHigh ||
            currentState == FighterState.BlockLow)
        {
            currentState = FighterState.Idle;
        }
    }

    // =========================================================================
    // Hit resolution
    // =========================================================================

    public void ReceiveHit(bool blocked, Transform attacker)
    {
        if (IsDead || MatchEnded) return;

        if (blocked)
        {
            currentState = currentState == FighterState.BlockLow
                ? FighterState.BlockLow
                : FighterState.BlockHigh;

            ApplyKnockback(attacker, blockedKnockback);
            return;
        }

        PlayClip(damageClip);
        CameraShake.Instance?.ShakeHit();  

        currentHealth -= 10;
        if (currentHealth < 0) currentHealth = 0;

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
        if (IsDead || MatchEnded) return;
        if (currentState == FighterState.Clash) return;  

        PlayClip(clashClip);
        currentState = FighterState.Clash;
        ApplyKnockback(attacker, hitKnockback);

        if (_lastClashShakeFrame != Time.frameCount)
        {
            _lastClashShakeFrame = Time.frameCount;
            CameraShake.Instance?.ShakeClash();
        }

        CancelInvoke(nameof(EndClash));
        Invoke(nameof(EndClash), clashDuration);
    }

    private void EndHitstun()
    {
        if (IsDead || MatchEnded) return;
        currentState    = FighterState.Idle;
        _animator.speed = 1f;
    }

    private void EndClash()
    {
        if (IsDead || MatchEnded) return;
        currentState    = FighterState.Idle;
        _animator.speed = 1f;
    }

    // =========================================================================
    // Death / match end
    // =========================================================================

    private void Die(Transform attacker)
    {
        currentHealth  = 0;
        currentStamina = 0f;

        CancelInvoke(nameof(EndAttack));
        CancelInvoke(nameof(EndHitstun));
        CancelInvoke(nameof(EndClash));
        StopBlocking();

        currentState       = FighterState.Dead;
        _animator.speed    = 1f;
        _rb.linearVelocity = Vector2.zero;

        PlayClip(deathClip);
        if (attacker != null) ApplyKnockback(attacker, hitKnockback);

        if (_deathFadeRoutine != null) StopCoroutine(_deathFadeRoutine);
        _deathFadeRoutine = StartCoroutine(FadeOutAndLoseColor());

        _opponentCombat?.OnMatchWon();
    }

    public void OnMatchWon()
    {
        if (IsDead) return;

        MatchEnded = true;

        CancelInvoke(nameof(EndAttack));
        CancelInvoke(nameof(EndHitstun));
        CancelInvoke(nameof(EndClash));
        StopBlocking();

        currentState       = FighterState.Idle;
        _rb.linearVelocity = Vector2.zero;
        _animator.speed    = 1f;
    }

    // =========================================================================
    // Animations
    // =========================================================================

    private void UpdateAnimations()
    {
        if (_animator == null) return;
        if (currentState == _lastAnimatedState) return;

        _lastAnimatedState = currentState;

        switch (currentState)
        {
            case FighterState.Idle:
                PlayLoop(HasState("Idle 2") ? "Idle 2" : "Idle");
                break;

            case FighterState.Walking:
                PlayLoop(HasState("Run 2") ? "Run 2" : "Run");
                break;

            case FighterState.AttackHigh:
            case FighterState.AttackLow:
                PlayTimed(HasState("Attack 2") ? "Attack 2" : "Attack", attackDuration);
                break;

            case FighterState.BlockHigh:
            case FighterState.BlockLow:
                PlayLoop(HasState("Block 2") ? "Block 2" : "Block");
                break;

            case FighterState.Hitstun:
                PlayTimed(HasState("Hurt 2") ? "Hurt 2" : "Hurt", hitstunDuration);
                break;

            case FighterState.Clash:
                PlayTimed(HasState("Hurt 2") ? "Hurt 2" : "Hurt", clashDuration);
                break;

            case FighterState.Dead:
                PlayLoop(HasState("Hurt 2") ? "Hurt 2" : "Hurt");
                break;
        }
    }

    private bool HasState(string stateName) =>
        _animator != null &&
        _animator.HasState(0, Animator.StringToHash(stateName));

    private void PlayLoop(string clipName)
    {
        if (_animator == null) return;
        _animator.speed = 1f;
        _animator.Play(clipName, 0, 0f);
    }

    private void PlayTimed(string clipName, float desiredDuration)
    {
        if (_animator == null) return;

        float clipLength = GetClipLength(clipName);
        if (clipLength <= 0f) clipLength = desiredDuration;

        _animator.speed = clipLength / Mathf.Max(0.01f, desiredDuration);
        _animator.Play(clipName, 0, 0f);
    }

    private float GetClipLength(string clipName)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return -1f;

        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name == clipName) return clip.length;

        return -1f;
    }

    // =========================================================================
    // Audio / Physics / UI helpers
    // =========================================================================

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void ApplyKnockback(Transform attacker, float force)
    {
        if (attacker == null || _rb == null) return;
        Vector2 dir = (transform.position - attacker.position).normalized;
        _rb.linearVelocity = new Vector2(dir.x * force, 0f);
    }

    private void UpdateUI()
    {
        if (healthBar != null)  { healthBar.maxValue  = maxHealth;  healthBar.value  = currentHealth;  }
        if (staminaBar != null) { staminaBar.maxValue = maxStamina; staminaBar.value = currentStamina; }
    }

    // =========================================================================
    // Death visual
    // =========================================================================

    private IEnumerator FadeOutAndLoseColor()
    {
        float   elapsed      = 0f;
        Color[] startColors  = new Color[_spriteRenderers.Length];
        Color[] targetColors = new Color[_spriteRenderers.Length];

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            startColors[i] = _spriteRenderers[i] != null ? _spriteRenderers[i].color : Color.white;
            float gray      = (startColors[i].r + startColors[i].g + startColors[i].b) / 3f;
            targetColors[i] = new Color(gray, gray, gray, 0f);
        }

        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / deathFadeDuration);

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] == null) continue;
                _spriteRenderers[i].color = Color.Lerp(startColors[i], targetColors[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] == null) continue;
            _spriteRenderers[i].color = targetColors[i];
        }
    }

    // =========================================================================
    // Debug logging
    // =========================================================================

    private void LogCurrentState()
    {
        if (currentState == _lastLoggedState) return;
        _lastLoggedState = currentState;

        string label = currentState switch
        {
            FighterState.Idle       => "idle",
            FighterState.Walking    => "walking",
            FighterState.AttackHigh => "attacking high",
            FighterState.AttackLow  => "attacking low",
            FighterState.BlockHigh  => "blocking high",
            FighterState.BlockLow   => "blocking low",
            FighterState.Hitstun    => "in hitstun",
            FighterState.Clash      => "clashing",
            FighterState.Dead       => "dead",
            _                       => currentState.ToString()
        };

        Debug.Log($"[VisionFighter] {name} is {label}");
    }
}