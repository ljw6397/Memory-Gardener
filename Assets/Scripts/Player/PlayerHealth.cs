using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;
    public int CurrentHealth => currentHealth;

    [Header("Hit Reaction")]
    public float hitStunDuration = 0.3f; // 맞은 뒤 조작 불가 시간
    public float hitFlashDuration = 0.1f;

    private float hitStunTimer = 0f;
    private Vector2 currentKnockback;
    private float hitFlashTimer = 0f;
    private Color originalColor;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private bool isDead = false;
    public bool IsDead => isDead;
    public bool IsHitStunned => hitStunTimer > 0f;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    void Update()
    {
        if (isDead) return;

        if (hitStunTimer > 0f)
        {
            hitStunTimer -= Time.deltaTime;

            if (rb != null)
                rb.linearVelocity = new Vector2(currentKnockback.x, rb.linearVelocity.y);

            if (hitStunTimer <= 0f && rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
        hitStunTimer = hitStunDuration;

        if (rb != null)
            rb.linearVelocity = knockback;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            hitFlashTimer = hitFlashDuration;
        }

        if (animator != null) animator.SetTrigger("Hit");

        // 맞는 순간 하던 행동(콤보, 대시어택, 내려찍기)을 즉시 중단
        playerCombat?.CancelCombo();
        playerController?.ForceCancelActions();

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        hitStunTimer = 0f;

        if (animator != null) animator.SetTrigger("Die");

        playerCombat?.CancelCombo();
        playerController?.ForceCancelActions();

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // 좌우 이동만 멈춤, 낙하는 자연스럽게 유지
    }
}