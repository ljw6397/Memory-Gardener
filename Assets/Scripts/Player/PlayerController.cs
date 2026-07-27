using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth; // ★ 추가

    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float jumpForce = 10f;
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
    public float enemyCheckDistance = 0.15f;
    public LayerMask enemyLayer;
    public float enemyCheckHeight = 3f;
    public float enemyCheckVerticalOffset = 1f;
    public float dashBlockCheckDistance = 0.4f;

    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private bool isGroundedAnim;
    private bool isDashAttacking = false;
    public bool IsDashAttacking => isDashAttacking;
    private float dashAttackTimer = 0f;
    private Transform dashAttackTarget;
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
        playerHealth = GetComponent<PlayerHealth>(); // ★ 추가
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

    bool IsBlockedByEnemy(Vector2 direction, float distance = -1f)
    {
        if (distance < 0f) distance = enemyCheckDistance;

        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * enemyCheckVerticalOffset;
        Vector2 boxSize = new Vector2(0.1f, enemyCheckHeight);
        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, direction, distance, enemyLayer);
        return hit.collider != null;
    }

    // ★ 추가: 피격/사망 시 진행 중이던 대시어택, 내려찍기를 그 즉시 강제로 정지
    public void ForceCancelActions()
    {
        isDashAttacking = false;
        dashAttackTarget = null;

        isSlamming = false;
        slamLanded = false;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetBool("IsSlamming", false);
            animator.SetBool("SlamRecoveryDone", true);
        }
    }

    void Update()
    {
        CheckGrounded();

        // ★ 추가: 매 프레임 히트스턴/사망 여부를 Animator에 전달 (Any State 경합 방지용)
        bool hitStunnedNow = playerHealth != null && (playerHealth.IsDead || playerHealth.IsHitStunned);
        animator.SetBool("HitStunned", hitStunnedNow);

        // ★ 추가: 사망 시 완전히 조작 불가. 좌우 이동만 멈추고 낙하는 자연스럽게 유지
        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        // ★ 추가: 히트스턴 중엔 넉백 속도는 PlayerHealth가 처리하므로 여기서는 아무 것도 안 건드림
        if (playerHealth != null && playerHealth.IsHitStunned)
        {
            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

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
            bool shouldStop = dashAttackTimer <= 0f;

            if (dashAttackTarget != null)
            {
                float dx = dashAttackTarget.position.x - transform.position.x;
                float absDx = Mathf.Abs(dx);

                if (absDx <= dashAttackStopDistance)
                {
                    shouldStop = true;
                }
                else
                {
                    float dir = Mathf.Sign(dx);
                    Vector2 checkDir = dir > 0 ? Vector2.right : Vector2.left;

                    if (IsBlockedByEnemy(checkDir, dashBlockCheckDistance))
                    {
                        shouldStop = true;
                    }
                    else
                    {
                        rb.linearVelocity = new Vector2(dir * dashAttackBurstSpeed, rb.linearVelocity.y);
                        facingRight = dir > 0f;
                        spriteRenderer.flipX = !facingRight;
                    }
                }
            }

            if (shouldStop)
            {
                isDashAttacking = false;
                dashAttackTarget = null;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            animator.SetBool("IsSlamming", false);
            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        HandleMove();
        HandleJump();

        animator.SetBool("IsSlamming", isSlamming);
        animator.SetBool("Grounded", isGroundedAnim);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
    }

    void UpdateFacingDirection()
    {
        if (mainCamera == null) return;
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

        if (moveInput != 0f)
        {
            Vector2 checkDir = moveInput > 0 ? Vector2.right : Vector2.left;

            if (IsBlockedByEnemy(checkDir))
            {
                moveInput = 0f;
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

        Gizmos.color = Color.cyan;
        Vector3 boxCenter = transform.position + Vector3.up * enemyCheckVerticalOffset;
        Vector3 rightCenter = boxCenter + Vector3.right * (enemyCheckDistance / 2f);
        Vector3 leftCenter = boxCenter + Vector3.left * (enemyCheckDistance / 2f);
        Vector3 gizmoSize = new Vector3(enemyCheckDistance, enemyCheckHeight, 0.1f);
        Gizmos.DrawWireCube(rightCenter, gizmoSize);
        Gizmos.DrawWireCube(leftCenter, gizmoSize);
    }
}