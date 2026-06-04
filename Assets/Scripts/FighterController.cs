using UnityEngine;

public class FighterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stickDeadzone = 0.2f;

    [Header("References")]
    public Transform opponent;

    private Rigidbody2D rb;
    private FighterCombat combat;

    private float moveInput;
    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        combat = GetComponent<FighterCombat>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        HandleMovementInput();
        FaceOpponent();
    }

    void FixedUpdate()
    {
        Move();
    }

    void HandleMovementInput()
    {
        moveInput = 0f;

        if (combat != null && (combat.IsDead || combat.MatchEnded))
            return;

        if (gameObject.name == "Player1")
        {
            // JoystickButton4 = LB (izquierda), JoystickButton5 = RB (derecha)
            // Cambia estos keycodes según los botones de tu joystick que quieras usar
            if (Input.GetKey(KeyCode.JoystickButton4))
                moveInput = -1f;
            else if (Input.GetKey(KeyCode.JoystickButton5))
                moveInput = 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                moveInput = -1f;
            else if (Input.GetKey(KeyCode.RightArrow))
                moveInput = 1f;
        }
    }

    void Move()
    {
        if (combat == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (combat.IsDead || combat.MatchEnded)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (combat.currentState == FighterCombat.FighterState.AttackHigh ||
            combat.currentState == FighterCombat.FighterState.AttackLow ||
            combat.currentState == FighterCombat.FighterState.BlockHigh ||
            combat.currentState == FighterCombat.FighterState.BlockLow)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (combat.currentState == FighterCombat.FighterState.Hitstun ||
            combat.currentState == FighterCombat.FighterState.Clash)
        {
            return;
        }

        if (Mathf.Abs(moveInput) > 0f)
        {
            bool hasStamina = combat.TryConsumeMovementStamina(Time.fixedDeltaTime);
            if (!hasStamina)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void FaceOpponent()
    {
        if (opponent == null)
            return;

        if (combat != null && (combat.IsDead || combat.MatchEnded))
            return;

        if (transform.position.x < opponent.position.x)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }
        else
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }
    }
}