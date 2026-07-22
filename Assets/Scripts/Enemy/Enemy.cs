using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 30;
    private int currentHealth;

    [Header("Knockback")]
    public float knockbackDuration = 0.15f;
    private float knockbackTimer = 0f;
    private Vector2 currentKnockback;

    [Header("Hit Feedback")]
    public float hitFlashDuration = 0.1f;
    private float hitFlashTimer = 0f;
    private Color originalColor;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool isDead = false;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    void Update()
    {
        if (isDead) return;

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;

            // 넉백 지속시간 동안엔 X(수평)만 계속 밀어주고, Y(수직)는 중력이 자연스럽게 처리하게 냅둠
            if (rb != null)
                rb.linearVelocity = new Vector2(currentKnockback.x, rb.linearVelocity.y);

            if (knockbackTimer <= 0f && rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // X만 멈춤, Y는 계속 중력 따라 자연스럽게
            }
        }

        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f && spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (isDead) return;

        currentHealth -= amount;

        currentKnockback = knockback;
        knockbackTimer = knockbackDuration;

        // 맞는 순간 X, Y 둘 다 한 번에 적용 → 위로 튕기면서 옆으로도 밀려나는 궤적 시작점
        if (rb != null)
            rb.linearVelocity = knockback;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            hitFlashTimer = hitFlashDuration;
        }

        if (animator != null) animator.SetTrigger("Hit");

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 1f);
    }
}