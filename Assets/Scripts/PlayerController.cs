using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private PlayerCombat playerCombat;

    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float jumpForce = 10f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float doubleTapWindow = 0.3f;
    public float moveInputBuffer = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.05f;
    public float groundAnimAnticipation = 0.15f;
    public LayerMask groundLayer;

    [Header("Jump Feel")]
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private bool isGroundedAnim;
    private bool isDashing = false;
    public bool IsDashing => isDashing; // ★ 추가: PlayerCombat이 참조할 용도
    private float dashTimer = 0f;
    private float lastDTapTime = -10f;
    private float lastATapTime = -10f;
    private bool facingRight = true;
    private float lastMoveMagnitude = 0f;
    private float lastMoveInputTime = -10f;
    private float normalGravity;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        normalGravity = rb.gravityScale;
    }

    void Update()
    {
        CheckGrounded();
        ApplyBetterGravity();

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;

            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        HandleMove();
        HandleJump();
        HandleDashInput();

        animator.SetBool("Grounded", isGroundedAnim);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
    }

    void CheckGrounded()
    {
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        isGroundedAnim = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance + groundAnimAnticipation, groundLayer);
    }

    void HandleJump()
    {
        if (playerCombat != null && playerCombat.IsAttacking) return;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            playerCombat?.CancelCombo(); // ★ 추가: 점프하면 대기 중이던 콤보 초기화
        }
    }

    void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = normalGravity * fallGravityMultiplier;
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
            rb.gravityScale = normalGravity * lowJumpMultiplier;
        else
            rb.gravityScale = normalGravity;
    }

    void HandleMove()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

        if (moveInput > 0) { facingRight = true; spriteRenderer.flipX = false; }
        else if (moveInput < 0) { facingRight = false; spriteRenderer.flipX = true; }

        float rawSpeed = Mathf.Abs(moveInput) * currentSpeed;
        float animSpeed;

        if (rawSpeed > 0.01f)
        {
            animSpeed = rawSpeed;
            lastMoveMagnitude = rawSpeed;
            lastMoveInputTime = Time.time;
        }
        else if (Time.time - lastMoveInputTime < moveInputBuffer)
        {
            animSpeed = lastMoveMagnitude;
        }
        else
        {
            animSpeed = 0f;
        }

        animator.SetFloat("Speed", animSpeed);
    }

    void HandleDashInput()
    {
        if (playerCombat != null && playerCombat.IsAttacking) return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastDTapTime <= doubleTapWindow) StartDash();
            lastDTapTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastATapTime <= doubleTapWindow) StartDash();
            lastATapTime = Time.time;
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        animator.SetTrigger("Dash");
        float dashDirection = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
        playerCombat?.CancelCombo(); // ★ 추가: 대시하면 대기 중이던 콤보 초기화
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * (groundCheckDistance + groundAnimAnticipation));
    }
}