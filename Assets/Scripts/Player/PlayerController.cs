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
    public float dashAttackLockedMaxDuration = 0.6f;   
    public float dashAttackStopDistance = 1f;          

    [Header("Ground Pound")]
    public float slamFallSpeed = 20f;
    public float slamInitialSpeed = 1f;
    public float slamAcceleration = 40f;
    public float slamRecoveryTime = 0.3f;

    [Header("Enemy Collision")]
    public float enemyCheckDistance = 0.15f; // 이 거리 안에 Enemy가 있으면 그쪽으로 이동 막음
    public LayerMask enemyLayer;

    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private bool isGroundedAnim;
    private bool isDashing = false;
    public bool IsDashing => isDashing;
    private bool isDashAttacking = false;
    public bool IsDashAttacking => isDashAttacking;
    private float dashAttackTimer = 0f;
    private Transform dashAttackTarget; 
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
    private bool slamLanded = false;
    private float slamRecoveryTimer = 0f;
    public System.Action OnSlamLand;
    public System.Action OnSlamRecoveryComplete;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        normalGravity = rb.gravityScale;

        if (mainCamera == null) mainCamera = Camera.main;
    }

   
    public Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return transform.position;
        Vector3 pos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0f;
        return pos;
    }

    void Update()
    {
        CheckGrounded();

        if (isSlamming && !slamLanded && isGrounded)
        {
            slamLanded = true;
            slamRecoveryTimer = slamRecoveryTime;
            OnSlamLand?.Invoke();
        }

        if (isSlamming && slamLanded)
        {
            slamRecoveryTimer -= Time.deltaTime;
            if (slamRecoveryTimer <= 0f)
            {
                isSlamming = false;
                slamLanded = false;
                OnSlamRecoveryComplete?.Invoke();
            }
        }

        UpdateFacingDirection();
        isAiming = Input.GetMouseButton(1);

        if (isSlamming)
        {
            if (!slamLanded)
            {
                float currentFallSpeed = Mathf.Abs(rb.linearVelocity.y);
                currentFallSpeed += slamAcceleration * Time.deltaTime;
                currentFallSpeed = Mathf.Min(currentFallSpeed, slamFallSpeed);
                rb.linearVelocity = new Vector2(0f, -currentFallSpeed);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            animator.SetBool("IsSlamming", true);
            animator.SetBool("SlamRecoveryDone", false);
            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        animator.SetBool("SlamRecoveryDone", true);

        ApplyBetterGravity();

        if (isDashAttacking)
        {
            dashAttackTimer -= Time.deltaTime;
            bool shouldStop = dashAttackTimer <= 0f; // 시간 다 되면 무조건 멈춤(안전장치)

            if (dashAttackTarget != null)
            {
                float dx = dashAttackTarget.position.x - transform.position.x;
                float absDx = Mathf.Abs(dx);

                if (absDx <= dashAttackStopDistance)
                {
                    shouldStop = true; // 타겟한테 충분히 붙었으면 멈춤
                }
                else
                {
                    float dir = Mathf.Sign(dx);
                    rb.linearVelocity = new Vector2(dir * dashAttackBurstSpeed, rb.linearVelocity.y);

                    facingRight = dir > 0f; // 쫓아가는 동안 계속 타겟 쪽을 보게
                    spriteRenderer.flipX = !facingRight;
                }
            }

            if (shouldStop)
            {
                isDashAttacking = false;
                dashAttackTarget = null;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // 붙은 자리에서 딱 멈춤
            }

            animator.SetBool("IsSlamming", false);
            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;

            animator.SetBool("IsSlamming", false);
            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        HandleMove();
        HandleJump();
        HandleDashInput();

        animator.SetBool("IsSlamming", isSlamming);
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

        // ★ 추가: 이동하려는 방향에 Enemy가 바로 붙어있는지 체크
        if (moveInput != 0f)
        {
            Vector2 checkDir = moveInput > 0 ? Vector2.right : Vector2.left;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, checkDir, enemyCheckDistance, enemyLayer);
            if (hit.collider != null)
            {
                moveInput = 0f; // 그 방향은 막힘 → 밀지 못하게 이동 입력 자체를 0으로
            }
        }

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
    public void StartDashAttackBurst(Transform target = null)
    {
        isDashAttacking = true;
        dashAttackTarget = target;
        dashAttackTimer = target != null ? dashAttackLockedMaxDuration : dashAttackBurstDuration;

        if (target != null)
        {
            bool faceRightNow = target.position.x >= transform.position.x;
            facingRight = faceRightNow;
            spriteRenderer.flipX = !faceRightNow;

            float dir = faceRightNow ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * dashAttackBurstSpeed, rb.linearVelocity.y);
        }
        else
        {
            float dir = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * dashAttackBurstSpeed, rb.linearVelocity.y);
        }
    }

    public void StartSlamPhysics()
    {
        isSlamming = true;
        slamLanded = false;
        rb.linearVelocity = new Vector2(0f, -slamInitialSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * (groundCheckDistance + groundAnimAnticipation));

        // ★ 추가: 좌우 Enemy 감지 거리 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * enemyCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * enemyCheckDistance);
    }
}