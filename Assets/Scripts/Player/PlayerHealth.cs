using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health (하트 시스템: 하트 1개 = 2칸, 맞으면 무조건 반 칸씩 깎임)")]
    public int maxHearts = 3;
    private int maxSegments;
    private int currentSegments;
    public int MaxSegments => maxSegments;
    public int CurrentSegments => currentSegments;
    public System.Action<int, int> OnHealthChanged;

    [Header("Hit Reaction")]
    public float hitStunDuration = 0.3f;
    public float hitFlashDuration = 0.1f;

    [Header("Hit Slow-Mo / Backdash Escape")]
    public float slowMoTimeScale = 0.15f;
    public float backdashInputWindow = 1f;
    public float slowMoRecoverySpeed = 1.5f;
    public KeyCode backdashKey = KeyCode.Space;

    [Header("Airborne Knockback")]
    public float airborneKnockbackThreshold = 3f; // ★ 추가: knockback.y가 이 값 이상이면 "떴다"로 판정

    private float hitStunTimer = 0f;
    private Vector2 currentKnockback;
    private float hitFlashTimer = 0f;
    private Color originalColor;

    private bool awaitingBackdash = false;
    private float backdashWindowTimer = 0f;
    private float lastKnockbackDirX = 1f;
    private float baseFixedDeltaTime;

    private bool isAirborneKnockback = false; // ★ 추가

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private bool isDead = false;
    public bool IsDead => isDead;
    public bool IsHitStunned => hitStunTimer > 0f || awaitingBackdash || isAirborneKnockback; // ★ 변경
    public bool IsInSlowMo => Time.timeScale < 1f; // ★ 변경: timeScale이 1 미만이면(아직 복구 안 끝났으면) 전부 슬로우모션 중으로 취급

    void Awake()
    {
        maxSegments = maxHearts * 2;
        currentSegments = maxSegments;

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

    void LateUpdate()
    {
        HandleHitFlash();

        if (isDead) return;

        if (isAirborneKnockback)
        {
            if (rb != null && rb.linearVelocity.y <= 0f && playerController != null && playerController.IsGrounded)
            {
                isAirborneKnockback = false;
                if (animator != null) animator.Play("Idle", 0, 0f);
            }
            return;
        }

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
    }

    void HandleHitFlash()
    {
        if (hitFlashTimer <= 0f) return;

        hitFlashTimer -= Time.deltaTime;
        if (hitFlashTimer <= 0f && spriteRenderer != null)
            spriteRenderer.color = originalColor;
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
            hitStunTimer = hitStunDuration;
            // ★ recoveringTimeScale = true; 삭제 — 이제 복구는 항상 진행 중이라 별도로 켤 필요 없음
        }
    }

    void HandleTimeScaleRecovery()
    {
        // ★ 변경: awaitingBackdash나 다른 상태와 무관하게, timeScale이 1이 아니기만 하면 항상 서서히 복구
        // → 이게 핵심! 맞은 순간부터 곧바로 "천천히 정상 속도로 돌아오는" 슬로우모션 느낌이 시작됨
        if (Time.timeScale >= 1f) return;

        Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, slowMoRecoverySpeed * Time.unscaledDeltaTime);
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;

        if (Time.timeScale >= 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDeltaTime;
        }
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (isDead) return;
        if (awaitingBackdash) return;
        if (isAirborneKnockback) return; // ★ 추가: 공중 넉백 중엔 새 히트 무시 (필요하면 나중에 콤보 확장 가능)

        currentSegments = Mathf.Max(0, currentSegments - 1);
        OnHealthChanged?.Invoke(currentSegments, maxSegments);

        currentKnockback = knockback;

        if (rb != null)
            rb.linearVelocity = knockback;

        playerController?.FaceAwayFromHit(knockback.x);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            hitFlashTimer = hitFlashDuration;
        }

        playerCombat?.CancelCombo();
        playerController?.ForceCancelActions();

        if (currentSegments <= 0)
        {
            Die();
            return;
        }

        // ★ 추가: 위로 띄우는 공격이면 슬로우모션/백대시 대신 Knockback 애니메이션 경로로
        if (knockback.y >= airborneKnockbackThreshold)
        {
            isAirborneKnockback = true;
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.Play("Knockback", 0, 0f);
            }
            return;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.Play("Hit", 0, 0f);
        }

        EnterHitSlowMo(knockback);
    }

    void EnterHitSlowMo(Vector2 knockback)
    {
        lastKnockbackDirX = knockback.x >= 0f ? 1f : -1f;

        awaitingBackdash = true;
        backdashWindowTimer = backdashInputWindow;

        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void TriggerBackdash()
    {
        awaitingBackdash = false;
        hitStunTimer = 0f;

        float backdashDir = lastKnockbackDirX;

        playerController?.StartBackDash(backdashDir);
    }

    void Die()
    {
        isDead = true;
        hitStunTimer = 0f;
        awaitingBackdash = false;
        isAirborneKnockback = false; 

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