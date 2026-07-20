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

    [Header("Mouse Aim")]
    public Camera mainCamera;

    [Header("Dash Attack")]
    public float dashAttackBurstSpeed = 14f;
    public float dashAttackBurstDuration = 0.15f;

    [Header("Ground Pound")]
    public float slamFallSpeed = 20f; // 내려찍기 낙하 속도 (클수록 빨리 내려감)

    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private bool isGroundedAnim;
    private bool isDashing = false;
    public bool IsDashing => isDashing;
    private bool isDashAttacking = false;
    private float dashAttackTimer = 0f;
    private float dashTimer = 0f;
    private float lastDTapTime = -10f;
    private float lastATapTime = -10f;
    private bool facingRight = true;
    public bool FacingRight => facingRight;
    private float lastMoveMagnitude = 0f;
    private float lastMoveInputTime = -10f;
    private float normalGravity;

    private bool isAiming = false;
    public bool IsAiming => isAiming;

    private bool isSlamming = false;
    public bool IsSlamming => isSlamming;
    public System.Action OnSlamLand; //착지 순간 다른 스크립트(카메라 등)에 알리는 이벤트

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        normalGravity = rb.gravityScale;

        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        CheckGrounded();

        //슬램 착지 감지 
        if (isSlamming && isGrounded)
        {
            isSlamming = false;
            OnSlamLand?.Invoke();
        }
        animator.SetBool("IsSlamming", isSlamming); 

        UpdateFacingDirection();
        isAiming = Input.GetMouseButton(1);

        //슬램 중엔 고정 낙하속도만 유지, 나머지 전부 무시
        if (isSlamming)
        {
            rb.linearVelocity = new Vector2(0f, -slamFallSpeed);
            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        ApplyBetterGravity();

        if (isDashAttacking)
        {
            dashAttackTimer -= Time.deltaTime;
            if (dashAttackTimer <= 0f) isDashAttacking = false;

            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

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
        HandleSlamInput();

        animator.SetBool("Grounded", isGroundedAnim);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
    }

    void UpdateFacingDirection()
    {
        if (mainCamera == null) return;
        if (isDashing) return;
        if (isSlamming) return; 
        if (playerCombat != null && playerCombat.IsAttacking) return;

        float moveInput = Input.GetAxisRaw("Horizontal");
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        bool mouseIsRight = mouseWorldPos.x > transform.position.x;

        if (moveInput > 0.01f && !mouseIsRight)
        {
            facingRight = true;
            spriteRenderer.flipX = false;
            return;
        }
        if (moveInput < -0.01f && mouseIsRight)
        {
            facingRight = false;
            spriteRenderer.flipX = true;
            return;
        }

        facingRight = mouseIsRight;
        spriteRenderer.flipX = !mouseIsRight;
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
            playerCombat?.CancelCombo();
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
        if (playerCombat != null && playerCombat.IsAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetFloat("Speed", 0f);
            return;
        }

        float moveInput = Input.GetAxisRaw("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

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
        playerCombat?.CancelCombo();
    }

    public void StartDashAttackBurst()
    {
        isDashAttacking = true;
        dashAttackTimer = dashAttackBurstDuration;

        float dir = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * dashAttackBurstSpeed, rb.linearVelocity.y);
    }

    // 공중에서 Ctrl 누르면 내려찍기 시작
    void HandleSlamInput()
    {
        if (playerCombat != null && playerCombat.IsAttacking) return;
        if (isGrounded) return; // 공중에서만 발동

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartSlam();
        }
    }

    void StartSlam()
    {
        isSlamming = true;
        rb.linearVelocity = new Vector2(0f, -slamFallSpeed);
        animator.SetTrigger("SlamTrigger");
        playerCombat?.CancelCombo();
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