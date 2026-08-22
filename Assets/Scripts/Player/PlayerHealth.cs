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

    [Header("Airborne Hit (공중에서 맞으면 무조건 강한 넉백으로 날아감)")]
    public float minAirborneKnockbackHeight = 1.3f;
    public float airborneHitKnockbackForce = 12f;
    public float airborneHitKnockbackUpward = 4f;
    public float knockbackSlowMoTimeScale = 0.5f; // ★ 추가: Knockback 전용, Hit보다 훨씬 살짝만 느려지게
    public float knockbackSlowMoRecoverySpeed = 4f;

    private float hitStunTimer = 0f;
    private Vector2 currentKnockback;
    private float hitFlashTimer = 0f;
    private Color originalColor;

    private bool awaitingBackdash = false;
    private float backdashWindowTimer = 0f;
    private float lastKnockbackDirX = 1f;
    private float baseFixedDeltaTime;

    private bool isAirborneKnockback = false;
    private bool isKnockbackHeld = false; // ★ 추가: Enemy와 동일한 "홀드" 상태 플래그

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private float knockbackTrueStartTime = 0f;
    private bool isDead = false;
    public bool IsDead => isDead;
    public bool IsHitStunned => hitStunTimer > 0f || awaitingBackdash || isAirborneKnockback;
    public bool IsInSlowMo => Time.timeScale < 1f;

    public static System.Action OnPlayerTookDamage;

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
        HandleTimeScaleRecovery(); // ★ 위치 이동: 어떤 상태든 상관없이 항상 먼저 실행되게

        if (isDead)
        {
            if (animator != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName("Die") && !animator.IsInTransition(0))
                {
                    animator.Play("Die", 0, 0f);
                }
            }
            return;
        }

        if (isAirborneKnockback)
        {
            if (isKnockbackHeld && rb != null && rb.linearVelocity.y <= 0f
                && playerController != null && playerController.IsGrounded)
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

        HandleBackdashWindow();

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
        }
    }

    void HandleTimeScaleRecovery()
    {
        if (Time.timeScale >= 1f) return;

        float recoverySpeed = isAirborneKnockback ? knockbackSlowMoRecoverySpeed : slowMoRecoverySpeed;

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
        if (isAirborneKnockback) return;

        currentSegments = Mathf.Max(0, currentSegments - 1);
        OnHealthChanged?.Invoke(currentSegments, maxSegments);

        OnPlayerTookDamage?.Invoke(); // ★ 추가

        // ★ 변경: 힘의 크기가 아니라 "지금 얼마나 공중에 떠 있는지"로 판정
        bool playerIsAirborne = playerController != null && !playerController.IsGrounded;
        bool shouldKnockback = false;

        if (playerIsAirborne)
        {
            float heightAboveGround = playerController.GetHeightAboveGround();
            if (heightAboveGround > minAirborneKnockbackHeight)
            {
                shouldKnockback = true;
                float dirX = knockback.x >= 0f ? 1f : -1f;
                knockback = new Vector2(dirX * airborneHitKnockbackForce, airborneHitKnockbackUpward);
            }
            // else: 너무 낮으면 knockback을 안 건드림 → 아래에서 그냥 평범한 Hit 경로로 흘러감
        }

        currentKnockback = knockback;

        playerController?.FaceAwayFromHit(knockback.x);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            hitFlashTimer = hitFlashDuration;
        }

        playerCombat?.CancelCombo();
        playerController?.ForceCancelActions(); // 이 안에서 velocity.x를 0으로 초기화함

        // ★ 버그 수정: 넉백 속도 적용을 ForceCancelActions() "이후"로 옮김
        // (이전엔 먼저 적용하고 ForceCancelActions가 곧바로 X를 0으로 지워버려서
        //  좌우로 날아가는 힘이 종종 씹히고 있었음)
        if (rb != null)
            rb.linearVelocity = knockback;

        if (currentSegments <= 0)
        {
            Die();
            return;
        }

        if (shouldKnockback)
        {
            isAirborneKnockback = true;
            isKnockbackHeld = false;

            if (animator != null)
            {
                animator.SetBool("HitStunned", true);
                animator.SetFloat("VelocityY", 0f);
                animator.SetBool("Grounded", true);
                animator.SetFloat("Speed", 0f);
                animator.Play("Knockback", 0, knockbackTrueStartTime);
            }

            EnterKnockbackSlowMo(); // ★ 추가: 백대시 없이 순수 슬로우모션 연출만

            return;
        }

        if (animator != null)
        {
            // ★ 추가: 동일한 이유로 안전하게 고정 (땅 근처 낮은 높이에서 점프 중 맞는 경우 대비)
            animator.SetFloat("VelocityY", 0f);
            animator.SetBool("Grounded", true);

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

    void EnterKnockbackSlowMo()
    {
        Time.timeScale = knockbackSlowMoTimeScale;
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
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
        isKnockbackHeld = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;

        playerCombat?.CancelCombo();
        playerController?.ForceCancelActions();

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.speed = 1f; // ★ 추가: 혹시 Knockback 홀드(speed=0) 상태였다면 Die도 얼어붙는 것 방지
            animator.SetFloat("VelocityY", 0f);
            animator.SetBool("Grounded", true);
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsSlamming", false);
            animator.SetBool("IsBackDashing", false);

            animator.Play("Die", 0, 0f);
        }
    }



    //애니메이션 이벤트들
    public void AnimEvent_KnockbackTrueStart()
    {
        if (animator == null) return;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        knockbackTrueStartTime = state.normalizedTime % 1f; // 0~1 범위로 정규화해서 저장
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
    public void AnimEvent_KnockbackFinished()
    {
        if (animator != null) animator.Play("Idle", 0, 0f);
    }
}