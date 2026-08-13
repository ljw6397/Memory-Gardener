using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 30;
    protected int currentHealth;

    [Header("Knockback")]
    public float knockbackDuration = 0.15f;
    private float knockbackTimer = 0f;
    private Vector2 currentKnockback;

    [Header("Airborne Knockback (위로 띄우는 공격)")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.15f;
    public LayerMask groundLayer;
    public float airborneKnockbackThreshold = 3f;

    [Header("Player Top Slide (떨어지다 Player 위에 걸리면 미끄러지게)")] // ★ 추가
    public LayerMask playerLayer; // Inspector에서 PlayerBody 레이어 체크
    public float playerTopCheckDistance = 0.2f;
    public float playerTopSlideSpeed = 4f;

    [Header("Slam Down (아래로 내려찍는 공격, 예: 플레이어 E 스킬)")]
    public float slamDownThreshold = 8f;
    public float slamDownAcceleration = 60f;
    public float slamDownMaxSpeed = 30f;
    public float bounceDamping = 0.4f;
    public float bounceMinSpeed = 2f;
    public int maxBounces = 2;

    public static System.Action OnSlamBounce;
    public static System.Action OnSlamSettled;

    [Header("Hit Feedback")]
    public float hitFlashDuration = 0.1f;
    private float hitFlashTimer = 0f;
    private Color originalColor;

    [Header("Targeting (모든 몬스터 공통 — 한번 감지하면 영구 타겟)")]
    public float detectionRadius = 5f;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;

    protected Transform target;
    protected PlayerHealth targetHealth;

    public bool HasTarget => target != null && (targetHealth == null || !targetHealth.IsDead);
    public Transform Target => target;
    public bool FacingRight => spriteRenderer != null && !spriteRenderer.flipX;

    private bool isDead = false;
    public bool IsDead => isDead;

    public bool IsKnockedBack => knockbackTimer > 0f;

    private bool isAirborneKnockback = false;
    public bool IsAirborneKnockback => isAirborneKnockback;

    private bool isSlammedDown = false;
    public bool IsSlammedDown => isSlammedDown;
    private int bounceCount = 0;
    private bool wasGroundedWhenSlamStarted = false;
    private bool isBouncing = false;

    private bool isKnockbackHeld = false;
    private float knockbackTrueStartTime = 0f;

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (groundCheck == null)
        {
            Debug.LogWarning($"[{name}] Enemy의 Ground Check가 비어있습니다! 발밑에 자식 오브젝트를 만들어 연결해주세요.");
        }
    }

    protected virtual void Update()
    {
        HandleHitFlash();

        if (isDead) return;

        if (isSlammedDown)
        {
            UpdateSlamDown();
            return;
        }

        if (isAirborneKnockback)
        {
            // ★ 추가: 떨어지는 중이고 바로 아래에 Player가 있으면, 착지 판정보다 우선해서 미끄러짐 처리
            if (rb.linearVelocity.y <= 0f)
            {
                RaycastHit2D playerHit = CheckPlayerBelow();
                if (playerHit.collider != null)
                {
                    HandleSlideOffPlayerTop(playerHit);
                    return;
                }
            }

            if (isKnockbackHeld && rb.linearVelocity.y <= 0f && IsGroundedCheck())
            {
                isAirborneKnockback = false;
                isKnockbackHeld = false;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                if (animator != null)
                {
                    animator.speed = 1f;
                }
            }
            return;
        }

        HandleKnockback();
        TryAcquireTarget();
    }

    // ★ 추가: 바로 아래에 Player 콜라이더가 있는지 감지
    RaycastHit2D CheckPlayerBelow()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, playerTopCheckDistance, playerLayer);
    }

    // ★ 추가: Player 위에서 자동으로 옆으로 미끄러지게 처리 (PlayerController.HandleEnemyTopSlide의 대칭 버전)
    void HandleSlideOffPlayerTop(RaycastHit2D playerHit)
    {
        Bounds playerBounds = playerHit.collider.bounds;
        float playerTopY = playerBounds.max.y;
        float playerCenterX = playerBounds.center.x;

        // Y는 Player 콜라이더 윗면에 고정 (파고들지 않게)
        Vector2 pos = rb.position;
        pos.y = playerTopY + 0.01f;
        rb.position = pos;

        // Enemy가 Player 중심보다 오른쪽이면 오른쪽으로, 왼쪽이면 왼쪽으로 자동으로 밀려남
        float slideDir = (transform.position.x >= playerCenterX) ? 1f : -1f;

        rb.linearVelocity = new Vector2(slideDir * playerTopSlideSpeed, 0f);
    }

    void UpdateSlamDown()
    {
        if (wasGroundedWhenSlamStarted)
        {
            FinishSlamDown();
            return;
        }

        if (rb.linearVelocity.y < 0f)
        {
            float fallSpeed = Mathf.Abs(rb.linearVelocity.y);
            fallSpeed += slamDownAcceleration * Time.deltaTime;
            fallSpeed = Mathf.Min(fallSpeed, slamDownMaxSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fallSpeed);
        }

        // ★ 추가: E 스킬(Slam)로 떨어지는 도중에도 Player 위에 걸리면 동일하게 미끄러짐
        if (rb.linearVelocity.y <= 0f)
        {
            RaycastHit2D playerHit = CheckPlayerBelow();
            if (playerHit.collider != null)
            {
                HandleSlideOffPlayerTop(playerHit);
                return;
            }
        }

        bool grounded = IsGroundedCheck();

        if (!isBouncing)
        {
            if (rb.linearVelocity.y > 0f)
            {
                isBouncing = true;
            }
            else if (grounded)
            {
                HandleSlamBounce(Mathf.Abs(rb.linearVelocity.y));
            }
        }
        else
        {
            if (rb.linearVelocity.y <= 0f && grounded)
            {
                isBouncing = false;
                HandleSlamBounce(Mathf.Abs(rb.linearVelocity.y));
            }
        }
    }

    void HandleSlamBounce(float impactSpeed)
    {
        bounceCount++;
        float bounceSpeed = impactSpeed * bounceDamping;

        if (bounceSpeed > bounceMinSpeed && bounceCount <= maxBounces)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, bounceSpeed);
            isBouncing = true;
            OnSlamBounce?.Invoke();
        }
        else
        {
            FinishSlamDown();
        }
    }

    void FinishSlamDown()
    {
        isSlammedDown = false;
        isKnockbackHeld = false;
        bounceCount = 0;
        isBouncing = false;
        rb.linearVelocity = new Vector2(0f, 0f);

        if (animator != null)
        {
            animator.speed = 1f;
        }

        OnSlamSettled?.Invoke();
    }

    public void AnimEvent_KnockbackTrueStart()
    {
        if (animator == null) return;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        knockbackTrueStartTime = state.normalizedTime % 1f;
    }

    public void AnimEvent_KnockbackHold()
    {
        if (!isAirborneKnockback && !isSlammedDown) return;

        isKnockbackHeld = true;
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    public void AnimEvent_KnockbackFinished()
    {
        if (animator != null) animator.Play("Idle", 0, 0f);
    }

    bool IsGroundedCheck()
    {
        if (groundCheck == null) return true;
        return Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    void HandleKnockback()
    {
        if (knockbackTimer <= 0f) return;

        knockbackTimer -= Time.deltaTime;

        if (rb != null)
            rb.linearVelocity = new Vector2(currentKnockback.x, rb.linearVelocity.y);

        if (knockbackTimer <= 0f && rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void HandleHitFlash()
    {
        if (hitFlashTimer <= 0f) return;

        hitFlashTimer -= Time.deltaTime;
        if (hitFlashTimer <= 0f && spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    void TryAcquireTarget()
    {
        if (target != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (dist <= detectionRadius)
        {
            target = player.transform;
            targetHealth = player.GetComponent<PlayerHealth>();
            OnTargetAcquired();
        }
    }

    protected virtual void OnTargetAcquired() { }

    protected void FaceTarget()
    {
        if (target == null || spriteRenderer == null) return;
        bool faceRight = target.position.x >= transform.position.x;
        spriteRenderer.flipX = !faceRight;
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (isDead) return;

        currentHealth -= amount;

        currentKnockback = knockback;

        if (rb != null)
            rb.linearVelocity = knockback;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            hitFlashTimer = hitFlashDuration;
        }

        OnHit();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (knockback.y >= airborneKnockbackThreshold)
        {
            isSlammedDown = false;
            bounceCount = 0;
            isAirborneKnockback = true;
            isKnockbackHeld = false;
            knockbackTimer = 0f;

            if (animator != null)
            {
                animator.speed = 1f;
                animator.Play("Knockback", 0, knockbackTrueStartTime);
            }
        }
        else if (knockback.y <= -slamDownThreshold)
        {
            isAirborneKnockback = false;
            isSlammedDown = true;
            isKnockbackHeld = false;
            bounceCount = 0;
            isBouncing = false;
            knockbackTimer = 0f;

            wasGroundedWhenSlamStarted = IsGroundedCheck();

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -Mathf.Abs(knockback.y));
            }

            if (animator != null)
            {
                animator.speed = 1f;
                animator.Play("Knockback", 0, knockbackTrueStartTime);
            }
        }
        else
        {
            if (animator != null) animator.SetTrigger("Hit");
        }
    }

    protected virtual void OnHit() { }

    protected virtual void Die()
    {
        isDead = true;
        isAirborneKnockback = false;
        isSlammedDown = false;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetTrigger("Die");
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 1f);
    }
}