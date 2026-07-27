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
    protected PlayerHealth targetHealth; // ★ 추가: 타겟(플레이어) 생사 확인용

    // ★ 변경: 타겟이 죽어있으면 자동으로 false가 됨 → 자식 AI의 "타겟 없으면 배회" 로직이 그대로 재사용됨
    public bool HasTarget => target != null && (targetHealth == null || !targetHealth.IsDead);
    public Transform Target => target;
    public bool FacingRight => spriteRenderer != null && !spriteRenderer.flipX;

    private bool isDead = false;
    public bool IsDead => isDead;

    public bool IsKnockedBack => knockbackTimer > 0f;

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    protected virtual void Update()
    {
        if (isDead) return;

        HandleKnockback();
        HandleHitFlash();
        TryAcquireTarget();
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
            targetHealth = player.GetComponent<PlayerHealth>(); // ★ 추가
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
        knockbackTimer = knockbackDuration;

        if (rb != null)
            rb.linearVelocity = knockback;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            hitFlashTimer = hitFlashDuration;
        }

        if (animator != null) animator.SetTrigger("Hit");

        OnHit();

        if (currentHealth <= 0) Die();
    }

    protected virtual void OnHit() { }

    protected virtual void Die()
    {
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");

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