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

    [Header("Airborne Knockback")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.15f;
    public LayerMask groundLayer;
    public float airborneKnockbackThreshold = 3f;

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

    private bool isKnockbackHeld = false; // ★ 추가: 지금 "누워있는" 정지 상태인지

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

    public void AnimEvent_KnockbackHold()
    {
        if (!isAirborneKnockback) return; 

        isKnockbackHeld = true;
        if (animator != null)
        {
            animator.speed = 0f; 
        }
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
        if (isAirborneKnockback) return;

        currentHealth -= amount;

        currentKnockback = knockback;
        knockbackTimer = knockbackDuration;

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
            isAirborneKnockback = true;
            isKnockbackHeld = false; 
            knockbackTimer = 0f;

            if (animator != null)
            {
                animator.speed = 1f; 
                animator.Play("Knockback", 0, 0f);
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
        isKnockbackHeld = false;

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

    public void AnimEvent_KnockbackFinished()
    {
        if (animator != null) animator.Play("Idle", 0, 0f);
    }
}