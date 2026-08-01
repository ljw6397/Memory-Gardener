using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;
    public int CurrentHealth => currentHealth;

    [Header("Hit Reaction")]
    public float hitStunDuration = 0.3f;
    public float hitFlashDuration = 0.1f;

    [Header("Hit Slow-Mo / Backdash Escape")]
    public float slowMoTimeScale = 0.05f;
    public float backdashInputWindow = 1f;
    public float slowMoRecoverySpeed = 1.5f;
    public KeyCode backdashKey = KeyCode.Space;

    private float hitStunTimer = 0f;
    private Vector2 currentKnockback;
    private float hitFlashTimer = 0f;
    private Color originalColor;

    private bool awaitingBackdash = false;
    private float backdashWindowTimer = 0f;
    private float lastKnockbackDirX = 1f;
    private bool recoveringTimeScale = false;
    private float baseFixedDeltaTime;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private bool isDead = false;
    public bool IsDead => isDead;

    // ★ 변경: awaitingBackdash 동안만 "조작 불가"로 취급.
    // hitStunTimer는 이제 "실패 후 슬라이드"용 용도로만 쓰이고, PlayerController가 참조하는 건 이 프로퍼티 하나뿐.
    public bool IsHitStunned => hitStunTimer > 0f || awaitingBackdash;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
    }

    // ★ 핵심 변경: LateUpdate로 옮김.
    // Unity는 모든 오브젝트의 Update()가 다 끝난 뒤에 LateUpdate()를 실행하는 걸 보장하기 때문에,
    // "PlayerController.Update()가 먼저 도는지 나중에 도는지" 경합 자체가 사라짐.
    // 이 프레임에 스페이스를 눌러 BackDash로 전환했다면, PlayerController가 이미 자기 Update를
    // 실행한 뒤이므로 그 프레임의 애니메이션 판단을 건드릴 일이 없고, 다음 프레임부터는
    // PlayerController가 isBackDashing=true를 보고 정상적으로 그 블록을 타게 됨.
    void LateUpdate()
    {
        if (isDead) return;

        HandleBackdashWindow();
        HandleTimeScaleRecovery();

        if (!awaitingBackdash && hitStunTimer > 0f)
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

    void HandleBackdashWindow()
    {
        if (!awaitingBackdash) return;

        backdashWindowTimer -= Time.unscaledDeltaTime;

        if (Input.GetKeyDown(backdashKey))
        {
            TriggerBackdash();
        }
        else if (backdashWindowTimer <= 0f)
        {
            awaitingBackdash = false;
            recoveringTimeScale = true;
            hitStunTimer = hitStunDuration;
        }
    }

    void HandleTimeScaleRecovery()
    {
        if (!recoveringTimeScale) return;

        Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, slowMoRecoverySpeed * Time.unscaledDeltaTime);
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;

        if (Time.timeScale >= 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDeltaTime;
            recoveringTimeScale = false;
        }
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (isDead) return;
        if (awaitingBackdash) return;

        currentHealth -= amount;

        currentKnockback = knockback;

        if (rb != null)
            rb.linearVelocity = knockback;

        playerController?.FaceAwayFromHit(knockback.x);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            hitFlashTimer = hitFlashDuration;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.Play("Hit", 0, 0f);
        }

        playerCombat?.CancelCombo();
        playerController?.ForceCancelActions();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        EnterHitSlowMo(knockback);
    }

    void EnterHitSlowMo(Vector2 knockback)
    {
        lastKnockbackDirX = knockback.x >= 0f ? 1f : -1f;

        awaitingBackdash = true;
        recoveringTimeScale = false;
        backdashWindowTimer = backdashInputWindow;

        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void TriggerBackdash()
    {
        awaitingBackdash = false;
        recoveringTimeScale = true;
        hitStunTimer = 0f;

        float backdashDir = lastKnockbackDirX;

        playerController?.StartBackDash(backdashDir);
    }

    void Die()
    {
        isDead = true;
        hitStunTimer = 0f;
        awaitingBackdash = false;

        recoveringTimeScale = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;

        if (animator != null)
        {
            animator.Play("Die", 0, 0f);
        }

        playerCombat?.CancelCombo();
        playerController?.ForceCancelActions();

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
}