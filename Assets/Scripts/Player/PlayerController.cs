using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;

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
    public float riseGravityMultiplier = 1.6f;

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

    [Header("Ground Pound - Shockwave (착지 시 광역 데미지)")] 
    public float shockwaveRadius = 4f;     
    public int shockwaveDamage = 8;
    public float shockwaveKnockbackForce = 6f;  
    public float shockwaveKnockbackUpward = 2f; 
    public LayerMask enemyDamageLayer;

    [Header("Enemy Collision")]
    public float enemyCheckDistance = 0.15f;
    public LayerMask enemyLayer;
    public float enemyCheckHeight = 3f;
    public float enemyCheckVerticalOffset = 1f;
    public float dashBlockCheckDistance = 0.4f;
    public float enemyTopCheckDistance = 0.2f; // ★ 추가: 발밑 방향으로 Enemy 몸통을 감지하는 거리
    public float enemyTopSlideSpeed = 4f; // ★ 추가: 미끄러져 내려오는 속도

    [Header("Hit Backdash")]
    public float backDashSpeed = 12f;
    public float backDashDuration = 0.25f;

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

    private bool isBackDashing = false;
    public bool IsBackDashing => isBackDashing;
    private float backDashTimer = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>();
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

    public float heightCheckMaxDistance = 50f;

    public float GetHeightAboveGround()
    {
        if (groundCheck == null) return 999f;

        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, heightCheckMaxDistance, groundLayer);
        if(hit.collider == null)
        {
            return hit.distance;
        }
        return 999f;
    }
    

    bool IsBlockedByEnemy(Vector2 direction, float distance = -1f)
    {
        if (distance < 0f) distance = enemyCheckDistance;

        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * enemyCheckVerticalOffset;
        Vector2 boxSize = new Vector2(0.1f, enemyCheckHeight);
        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, direction, distance, enemyLayer);
        return hit.collider != null;
    }

    // ★ 추가: 발밑 방향으로 Enemy 몸통이 있는지 감지 (Enemy 머리 위에 착지하는 상황 처리용)
    RaycastHit2D CheckEnemyBelow()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, enemyTopCheckDistance, enemyLayer);
    }
    // ★ 추가: Enemy 위에서 자동으로 옆으로 미끄러지게 처리
    void HandleEnemyTopSlide(RaycastHit2D enemyHit)
    {
        Bounds enemyBounds = enemyHit.collider.bounds;
        float enemyTopY = enemyBounds.max.y;
        float enemyCenterX = enemyBounds.center.x;

        // Y는 계속 Enemy 윗면에 고정 (뚫고 들어가지 않게)
        Vector2 pos = rb.position;
        pos.y = enemyTopY + 0.01f;
        rb.position = pos;

        // 플레이어가 Enemy 중심보다 오른쪽이면 오른쪽으로, 왼쪽이면 왼쪽으로 자동으로 밀어줌
        float slideDir = (transform.position.x >= enemyCenterX) ? 1f : -1f;

        rb.linearVelocity = new Vector2(slideDir * enemyTopSlideSpeed, 0f);

        facingRight = slideDir > 0f;
        spriteRenderer.flipX = !facingRight;

        animator.SetFloat("Speed", enemyTopSlideSpeed); // 걷는 애니메이션이 자연스럽게 재생됨
    }

    public void ForceCancelActions()
    {
        isDashAttacking = false;
        dashAttackTarget = null;

        isSlamming = false;
        slamLanded = false;

        isBackDashing = false;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetBool("IsSlamming", false);
            animator.SetBool("SlamRecoveryDone", true);
            animator.SetBool("IsBackDashing", false);
        }
    }

    public void StartBackDash(float direction)
    {
        isBackDashing = true;
        backDashTimer = backDashDuration;

        rb.linearVelocity = new Vector2(direction * backDashSpeed, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetBool("IsBackDashing", true);
            animator.Play("BackDash", 0, 0f);
        }
    }

    void ApplyGroundSlamShockwave()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius, enemyDamageLayer);

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead) continue;

            // 플레이어 기준 왼쪽/오른쪽 판단: 적이 플레이어보다 왼쪽이면 -1(왼쪽으로 날아감), 오른쪽이면 +1(오른쪽으로)
            float dir = (hit.transform.position.x >= transform.position.x) ? 1f : -1f;

            Vector2 knockback = new Vector2(dir * shockwaveKnockbackForce, shockwaveKnockbackUpward);
            enemy.TakeDamage(shockwaveDamage, knockback);
        }
    }

    void Update()
    {
        CheckGrounded();

        bool hitStunnedNow = playerHealth != null && (playerHealth.IsDead || playerHealth.IsHitStunned);
        animator.SetBool("HitStunned", hitStunnedNow);

        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.gravityScale = normalGravity * fallGravityMultiplier;
            return;
        }

        if (playerHealth != null && playerHealth.IsHitStunned)
        {
            return;
        }

        if (isBackDashing)
        {
            backDashTimer -= Time.deltaTime;

            float dashDir = rb.linearVelocity.x >= 0 ? 1f : -1f;
            Vector2 checkDir = dashDir > 0 ? Vector2.right : Vector2.left;

            if (IsBlockedByEnemy(checkDir, dashBlockCheckDistance))
            {
                isBackDashing = false;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            else if (backDashTimer <= 0f)
            {
                isBackDashing = false;
            }
            else
            {
                rb.linearVelocity = new Vector2(dashDir * backDashSpeed, rb.linearVelocity.y);
            }

            if (!isBackDashing) animator.SetBool("IsBackDashing", false);

            animator.SetBool("Grounded", isGroundedAnim);
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            return;
        }

        if (isSlamming && !slamLanded && isGrounded)
        {
            slamLanded = true;
            slamRecoveryTimer = slamRecoveryTime;
            OnSlamLand?.Invoke();
            ApplyGroundSlamShockwave(); // ★ 추가: 카메라 흔들림 트리거와 같은 타이밍에 광역 데미지 적용
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

        if (!isGrounded && rb.linearVelocity.y <= 0f)
        {
            RaycastHit2D enemyHit = CheckEnemyBelow();
            if (enemyHit.collider != null)
            {
                HandleEnemyTopSlide(enemyHit);

                animator.SetBool("IsSlamming", isSlamming);
                animator.SetBool("Grounded", isGroundedAnim);
                animator.SetFloat("VelocityY", rb.linearVelocity.y);
                return; // ★ 중요: 여기서 바로 return해서 아래 HandleMove()가 슬라이드 속도를 덮어쓰지 못하게 함
            }
        }

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
        if (playerCombat != null && playerCombat.IsBusy) return;

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
        if (playerCombat != null && playerCombat.IsBusy) return;

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
        else if (rb.linearVelocity.y > 0)
            rb.gravityScale = normalGravity * riseGravityMultiplier;
        else
            rb.gravityScale = normalGravity;
    }

    void HandleMove()
    {
        if (playerCombat != null && playerCombat.IsBusy)
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

        float targetVelocityX = moveInput * currentSpeed;

        // ★ 추가: 아주 짧은 가감속 (뚝 끊기는 뻣뻣함만 살짝 부드럽게, 두둥실거리지 않을 정도로만)
        float accelRate = 40f; // 클수록 즉각 반응(뻣뻣), 작을수록 부드러움(둥실). 40이면 거의 즉각적임
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, accelRate * Time.deltaTime);

        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

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

    public void FaceAwayFromHit(float knockbackDirX)
    {
        facingRight = knockbackDirX < 0f;
        spriteRenderer.flipX = !facingRight;
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

        // ★ 추가: 슬램 충격파 반경 시각화
        Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}