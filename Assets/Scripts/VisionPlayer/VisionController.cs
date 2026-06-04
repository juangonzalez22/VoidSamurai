using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(VisionFighter))]
[RequireComponent(typeof(InputManager))]

public class VisionController : MonoBehaviour
{
    // =========================================================================
    // Inspector
    // =========================================================================

    [Header("Movement")]
    public float moveSpeed    = 20f;
    public float stepDuration = 2f;

    [Header("References")]
    public Transform opponent;

    // =========================================================================
    // Private
    // =========================================================================

    private Rigidbody2D   _rb;
    private VisionFighter _combat;
    private InputManager  _input;

    private float   _moveInput;
    private float   _visionImpulseTimer;
    private float   _visionDirection; 
    private Vector3 _originalScale;

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    private void Start()
    {
        _rb            = GetComponent<Rigidbody2D>();
        _combat        = GetComponent<VisionFighter>();
        _input         = GetComponent<InputManager>();
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        ResolveMovementInput();
        FaceOpponent();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    // =========================================================================
    // Input resolution
    // =========================================================================

    private void ResolveMovementInput()
    {
        _moveInput = 0f;

        if (_combat != null && (_combat.IsDead || _combat.MatchEnded))
            return;

        // Impulso de visión
        if (_input.MoveForward)
        {
            _visionDirection    =  1f;
            _visionImpulseTimer = stepDuration;
        }
        else if (_input.MoveBackward)
        {
            _visionDirection    = -1f;
            _visionImpulseTimer = stepDuration;
        }

        if (_visionImpulseTimer > 0f)
        {
            _moveInput           = _visionDirection;
            _visionImpulseTimer -= Time.deltaTime;
        }
        else
        {
            _visionImpulseTimer = 0f;

            // Fallback de teclado
            if (gameObject.name == "Player1")
            {
                if (Input.GetKey(KeyCode.A)) _moveInput = -1f;
                if (Input.GetKey(KeyCode.D)) _moveInput =  1f;
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftArrow))  _moveInput = -1f;
                if (Input.GetKey(KeyCode.RightArrow)) _moveInput =  1f;
            }
        }
    }

    // =========================================================================
    // Movement application
    // =========================================================================

    private void ApplyMovement()
    {
        if (_combat == null || _combat.IsDead || _combat.MatchEnded)
        {
            ZeroHorizontal();
            return;
        }

        switch (_combat.currentState)
        {
            case VisionFighter.FighterState.AttackHigh:
            case VisionFighter.FighterState.AttackLow:
            case VisionFighter.FighterState.BlockHigh:
            case VisionFighter.FighterState.BlockLow:
                ZeroHorizontal();
                return;
        }

        if (_combat.currentState == VisionFighter.FighterState.Hitstun ||
            _combat.currentState == VisionFighter.FighterState.Clash)
            return;

        if (Mathf.Abs(_moveInput) > 0f)
        {
            bool hasStamina = _combat.TryConsumeMovementStamina(Time.fixedDeltaTime);
            if (!hasStamina) { ZeroHorizontal(); return; }
        }

        _rb.linearVelocity = new Vector2(_moveInput * moveSpeed, _rb.linearVelocity.y);
    }

    // =========================================================================
    // Orientation
    // =========================================================================

    private void FaceOpponent()
    {
        if (opponent == null) return;
        if (_combat != null && (_combat.IsDead || _combat.MatchEnded)) return;

        float sx = transform.position.x < opponent.position.x
            ?  Mathf.Abs(_originalScale.x)
            : -Mathf.Abs(_originalScale.x);

        transform.localScale = new Vector3(sx, _originalScale.y, _originalScale.z);
    }

    private void ZeroHorizontal() =>
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
}