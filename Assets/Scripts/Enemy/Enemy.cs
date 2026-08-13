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

    [Header("Slam Down (아래로 내려찍는 공격, 예: 플레이어 E 스킬)")]
    public float slamDownThreshold = 8f;
    public float slamDownAcceleration = 60f;
    public float slamDownMaxSpeed = 30f;
    public float bounceDamping = 0.4f;
    public float bounceMinSpeed = 2f;
    public int maxBounces = 2; // ★ 변경: 말씀하신 "두 번 튕기다가"에 맞춰 기본값 2로

    public static System.Action OnSlamBounce;
    public static System.Action OnSlamSettled;

    [Header("Hit Feedback")]
    public float hitFlashDuration = 0.1f;
    private float hitFlashTimer = 0f;
    private Color originalColor;
    private float knockbackTrueStartTime = 0f;

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
    private bool wasGroundedWhenSlamStarted = false; // ★ 추가: 슬램 시작 시점에 이미 땅 위였는지 (=바운스 없이 즉시 박히는 케이스)
    private bool isBouncing = false; // ★ 추가: 지금 튕겨서 공중에 뜬 상태인지 (착지 판정을 허용할지 결정)

    private bool isKnockbackHeld = false;

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

    void UpdateSlamDown()
    {
        // ★ 케이스 1: 슬램 시작부터 이미 땅 위였던 적 → 튕기지 않고 즉시 정착
        if (wasGroundedWhenSlamStarted)
        {
            FinishSlamDown();
            return;
        }

        // 낙하/가속 처리 (아래로 향하는 동안만)
        if (rb.linearVelocity.y < 0f)
        {
            float fallSpeed = Mathf.Abs(rb.linearVelocity.y);
            fallSpeed += slamDownAcceleration * Time.deltaTime;
            fallSpeed = Mathf.Min(fallSpeed, slamDownMaxSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fallSpeed);
        }

        bool grounded = IsGroundedCheck();

        if (!isBouncing)
        {
            // ★ 핵심: velocity.y가 양수(막 튕겨서 올라가는 중)인 동안은 "공중에 떠서 튕기는 중"으로 표시해두고
            // 착지 판정 자체를 하지 않음 → 이게 "튕겼는데 바로 또 착지로 오판되는" 문제를 막아줌
            if (rb.linearVelocity.y > 0f)
            {
                isBouncing = true;
            }
            else if (grounded)
            {
                // 아직 한 번도 안 튕겼고, 계속 땅에 붙어있는 상태 → 첫 착지
                HandleSlamBounce(Mathf.Abs(rb.linearVelocity.y));
            }
        }
        else
        {
            // 튕겨서 공중에 뜬 상태 → velocity.y가 다시 음수로 바뀌고(정점 찍고 하강 시작) + 실제로 땅에 닿아야만 착지로 인정
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
            isBouncing = true; // ★ 추가: 튕겨 올린 직후 즉시 "튕기는 중" 상태로 표시
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

            // ★ 핵심: 슬램이 시작되는 이 순간, 이미 땅에 붙어있는지(=원래 땅 위였는지) 미리 기록해둠
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



    //애니메이션 이벤트들
    public void AnimEvent_KnockbackTrueStart()
    {
        if (animator == null) return;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        knockbackTrueStartTime = state.normalizedTime % 1f; // 0~1 범위로 정규화해서 저장
    }
}