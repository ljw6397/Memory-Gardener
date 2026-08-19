using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;
    private PlayerHealth playerHealth;

    [Header("Combo Settings")]
    public int maxComboStep = 3;
    public float comboGraceWindow = 0.5f;
    public float comboGapDelay = 0.08f;

    [Header("Post-Attack Recovery (현자타임)")]
    public float postAttackRecovery = 0.2f;

    [Header("Hitboxes (펀치마다 손 위치가 달라서 각각 따로)")]
    public PunchHitbox punchHitboxA;
    public PunchHitbox punchHitboxB;
    public PunchHitbox punchHitboxC;

    [Header("Kick (Q)")]
    public PunchHitbox kickHitbox;
    public float kickRecovery = 0.25f;

    [Header("Overhead Smash (E)")]
    public PunchHitbox smashHitbox;   // 내려찍기 전용 히트박스 (PunchHitbox 재사용, Knockback Upward를 음수로 설정해서 씀)
    public float smashRecovery = 0.3f;

    [Header("Dash Attack Targeting")]
    public float dashAttackRange = 8f;

    [Header("Power Punch (조준 중 DD/AA)")]
    public PunchHitbox powerPunchHitbox; // 파워펀치 전용 히트박스 (PunchHitbox 재사용, Is Power Hit 체크)
    public float powerPunchBurstSpeed = 20f; // 기존 대시어택보다 빠르게
    public float powerPunchStopDistance = 1f;
    public float powerPunchKnockbackForce = 14f; // 멀리 날아가는 좌우 힘
    public float powerPunchKnockbackUpward = 6f; // airborneKnockbackThreshold(보통 3)보다 확실히 커야 Knockback으로 처리됨
    public float powerPunchDoubleTapWindow = 0.3f;
    public float powerPunchRecovery = 0.35f;



    private int comboStep = 0;
    private bool isAttacking = false;
    private int queuedAttacks = 0;
    private float comboExpireTime = -10f;
    private int attackStartFrame = -1;
    private bool waitingForNextComboStep = false;
    private float comboGapTimer = 0f;
    private Transform currentLockOnTarget;

    private bool isKicking = false;
    private int kickStartFrame = -1;

    private bool isSmashing = false;
    private int smashStartFrame = -1;

    private bool isRecovering = false;
    private float recoveryTimer = 0f;

    private bool isPowerPunching = false;
    private int powerPunchStartFrame = -1;
    private float lastPowerDTapTime = -10f;
    private float lastPowerATapTime = -10f;

    public bool IsAttacking => isAttacking || isKicking || isSmashing || isPowerPunching;
    public bool IsRecovering => isRecovering;
    public bool IsBusy => isAttacking || isKicking || isSmashing || isPowerPunching || isRecovering;
    public Transform CurrentLockOnTarget => currentLockOnTarget;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();

        if (playerController != null)
            playerController.OnSlamRecoveryComplete += HandleSlamRecoveryComplete;
    }

    void OnDestroy()
    {
        if (playerController != null)
            playerController.OnSlamRecoveryComplete -= HandleSlamRecoveryComplete;
    }

    void Update()
    {
        if (playerHealth != null && (playerHealth.IsDead || playerHealth.IsHitStunned)) return;
        if (playerController != null && playerController.IsBackDashing) return;

        UpdateLockOnTarget();
        HandleAttackInput();
        HandleKickInput();
        HandleSmashInput();
        HandleSlamInput();
        HandlePowerPunchInput(); // ★ 추가
        UpdateComboGap();
        UpdateRecovery();
        SafetyCheck();
        UpdatePowerPunch(); // ★ 추가
    }

    void UpdateRecovery()
    {
        if (!isRecovering) return;

        recoveryTimer -= Time.deltaTime;
        if (recoveryTimer <= 0f)
        {
            isRecovering = false;
        }
    }

    void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (isRecovering) return;
        if (isKicking) return;
        if (isSmashing) return;

        if (playerController != null && playerController.IsAiming && !isAttacking
            && playerController.IsGrounded)
        {
            if (currentLockOnTarget != null)
            {
                TriggerDashAttack(currentLockOnTarget);
                return;
            }
        }

        if (playerController != null && !playerController.IsGrounded) return;

        if (isAttacking)
        {
            int remainingSteps = maxComboStep - comboStep;
            if (queuedAttacks < remainingSteps)
                queuedAttacks++;
        }
        else if (comboStep > 0 && comboStep < maxComboStep && Time.time <= comboExpireTime)
        {
            StartAttack(comboStep + 1);
        }
        else
        {
            StartAttack(1);
        }
    }

    void HandleKickInput()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;
        if (isRecovering) return;
        if (isAttacking) return;
        if (isKicking) return;
        if (isSmashing) return;

        StartKick();
    }

    void StartKick()
    {
        DeactivateAllHitboxes();

        isKicking = true;
        animator.SetTrigger("KickTrigger");
        kickStartFrame = Time.frameCount;
    }

    public void AnimEvent_KickFinished()
    {
        kickHitbox?.Deactivate();
        isKicking = false;

        isRecovering = true;
        recoveryTimer = kickRecovery;
    }

    // E 입력 처리. 공중이어도(적을 띄운 뒤 따라가며) 발동 가능
    void HandleSmashInput()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (isRecovering) return;
        if (isAttacking) return;
        if (isKicking) return;
        if (isSmashing) return;

        StartSmash();
    }

    // ★ 추가: 조준(우클릭) 중 D 또는 A 더블탭 감지
    void HandlePowerPunchInput()
    {
        if (isRecovering || isAttacking || isKicking || isSmashing || isPowerPunching) return;
        if (playerController == null || !playerController.IsAiming) return;
        if (!playerController.IsGrounded) return;
        if (currentLockOnTarget == null) return; // 락온된 타겟이 없으면 발동 안 함

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastPowerDTapTime <= powerPunchDoubleTapWindow)
            {
                StartPowerPunch();
            }
            lastPowerDTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastPowerATapTime <= powerPunchDoubleTapWindow)
            {
                StartPowerPunch();
            }
            lastPowerATapTime = Time.time;
        }
    }

    void StartPowerPunch()
    {
        DeactivateAllHitboxes();

        isPowerPunching = true;

        // ★ 변경: SetTrigger 대신 즉시 컷 전환. 다른 모든 액션(Hit, Knockback, BackDash 등)과
        // 동일한 방식으로 통일해서, "물리는 멈췄는데 애니메이션은 아직 Walk/Idle" 같은
        // 한두 프레임짜리 경합을 원천 차단함.
        animator.Play("PowerPunch", 0, 0f);

        powerPunchStartFrame = Time.frameCount;

        playerController.StartDashAttackBurst(currentLockOnTarget, powerPunchBurstSpeed, powerPunchStopDistance);
    }

    // Animation Event: 파워펀치 손이 닿는 프레임에 연결 → 넉백값을 강하게 세팅한 채로 히트박스 켬
    public void AnimEvent_PowerPunchHitboxOn()
    {
        if (powerPunchHitbox == null) return;
        powerPunchHitbox.knockbackForce = powerPunchKnockbackForce;
        powerPunchHitbox.knockbackUpward = powerPunchKnockbackUpward;
        powerPunchHitbox.Activate();
    }

    public void AnimEvent_PowerPunchHitboxOff()
    {
        powerPunchHitbox?.Deactivate();
    }

    // Animation Event: 파워펀치 클립 마지막 프레임에 연결
    public void AnimEvent_PowerPunchFinished()
    {
        powerPunchHitbox?.Deactivate();
        isPowerPunching = false;

        isRecovering = true;
        recoveryTimer = powerPunchRecovery;
    }

    void UpdatePowerPunch()
    {
        if (!isPowerPunching) return;
        if (Time.frameCount == powerPunchStartFrame) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsTag("Attack") && !animator.IsInTransition(0))
        {
            powerPunchHitbox?.Deactivate();
            isPowerPunching = false;
        }
    }

    void StartSmash()
    {
        DeactivateAllHitboxes();

        isSmashing = true;
        animator.SetTrigger("SmashTrigger");
        smashStartFrame = Time.frameCount;
    }

    // Animation Event: 내려찍기 클립 마지막 프레임에서 호출
    public void AnimEvent_SmashFinished()
    {
        smashHitbox?.Deactivate();
        isSmashing = false;

        isRecovering = true;
        recoveryTimer = smashRecovery;
    }

    void StartAttack(int step)
    {
        DeactivateAllHitboxes();

        comboStep = step;
        isAttacking = true;
        isRecovering = false;
        animator.SetInteger("ComboStep", comboStep);
        animator.SetTrigger("AttackTrigger");
        attackStartFrame = Time.frameCount;
    }

    public void AnimEvent_HitboxOn(string which)
    {
        GetHitbox(which)?.Activate();
    }

    public void AnimEvent_HitboxOff(string which)
    {
        GetHitbox(which)?.Deactivate();
    }

    PunchHitbox GetHitbox(string which)
    {
        switch (which)
        {
            case "A": return punchHitboxA;
            case "B": return punchHitboxB;
            case "C": return punchHitboxC;
            case "K": return kickHitbox;
            case "S": return smashHitbox;
            default: return null;
        }
    }

    void DeactivateAllHitboxes()
    {
        punchHitboxA?.Deactivate();
        punchHitboxB?.Deactivate();
        punchHitboxC?.Deactivate();
        kickHitbox?.Deactivate();
        smashHitbox?.Deactivate();
        powerPunchHitbox?.Deactivate(); // ★ 추가
    }

    void UpdateLockOnTarget()
    {
        bool canDashAttack = playerController != null && playerController.IsAiming
            && !isAttacking && !isRecovering && playerController.IsGrounded;

        currentLockOnTarget = canDashAttack ? FindDashAttackTarget() : null;
    }

    public void AnimEvent_DashAttackFinished()
    {
        StartAttack(1);
    }

    void TriggerDashAttack(Transform target)
    {
        DeactivateAllHitboxes();

        comboStep = 0;
        queuedAttacks = 0;
        isAttacking = true;
        isRecovering = false;
        animator.SetTrigger("DashAttackTrigger");
        attackStartFrame = Time.frameCount;
        playerController.StartDashAttackBurst(target);
    }

    Transform FindDashAttackTarget()
    {
        if (playerController == null) return null;

        Vector3 mouseWorldPos = playerController.GetMouseWorldPosition();
        Vector3 playerPos = transform.position;
        float mouseDir = Mathf.Sign(mouseWorldPos.x - playerPos.x);

        if (currentLockOnTarget != null)
        {
            Enemy currentEnemy = currentLockOnTarget.GetComponent<Enemy>();
            bool stillAlive = currentEnemy != null && !currentEnemy.IsDead;

            if (stillAlive)
            {
                float distToPlayer = Vector2.Distance(playerPos, currentLockOnTarget.position);
                float enemyDir = Mathf.Sign(currentLockOnTarget.position.x - playerPos.x);

                if (distToPlayer <= dashAttackRange && enemyDir == mouseDir)
                {
                    return currentLockOnTarget;
                }
            }
        }

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        Transform best = null;
        float bestPlayerDist = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.IsDead) continue;

            float distToPlayer = Vector2.Distance(playerPos, enemy.transform.position);
            if (distToPlayer > dashAttackRange) continue;

            float enemyDir = Mathf.Sign(enemy.transform.position.x - playerPos.x);
            if (enemyDir != mouseDir) continue;

            if (distToPlayer < bestPlayerDist)
            {
                bestPlayerDist = distToPlayer;
                best = enemy.transform;
            }
        }

        return best;
    }

    void UpdateComboGap()
    {
        if (!waitingForNextComboStep) return;

        comboGapTimer -= Time.deltaTime;
        if (comboGapTimer <= 0f)
        {
            waitingForNextComboStep = false;
            queuedAttacks--;
            StartAttack(comboStep + 1);
        }
    }

    void HandleSlamInput()
    {
        if (isAttacking || isRecovering || isKicking || isSmashing) return;
        if (playerController == null) return;
        if (playerController.IsGrounded) return;
        if (playerController.IsDashAttacking) return;

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            TriggerSlam();
        }
    }

    void TriggerSlam()
    {
        DeactivateAllHitboxes();

        comboStep = 0;
        queuedAttacks = 0;
        isAttacking = true;
        isRecovering = false;
        animator.SetTrigger("SlamTrigger");
        attackStartFrame = Time.frameCount;
        playerController.StartSlamPhysics();
    }

    void HandleSlamRecoveryComplete()
    {
        isAttacking = false;
        comboStep = 0;
        queuedAttacks = 0;
    }

    public void AnimEvent_AttackFinished()
    {
        DeactivateAllHitboxes();

        if (queuedAttacks > 0 && comboStep < maxComboStep)
        {
            waitingForNextComboStep = true;
            comboGapTimer = comboGapDelay;
        }
        else
        {
            isAttacking = false;
            queuedAttacks = 0;
            comboExpireTime = Time.time + comboGraceWindow;

            isRecovering = true;
            recoveryTimer = postAttackRecovery;
        }
    }

    public void CancelCombo()
    {
        DeactivateAllHitboxes();

        comboStep = 0;
        isAttacking = false;
        queuedAttacks = 0;
        comboExpireTime = -10f;
        waitingForNextComboStep = false;
        isRecovering = false;
        recoveryTimer = 0f;

        isKicking = false;
        isSmashing = false;
        isPowerPunching = false; 
    }

    void SafetyCheck()
    {
        if (isAttacking)
        {
            if (Time.frameCount != attackStartFrame)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsTag("Attack") && !animator.IsInTransition(0))
                {
                    DeactivateAllHitboxes();
                    isAttacking = false;
                    comboStep = 0;
                    queuedAttacks = 0;
                    waitingForNextComboStep = false;
                }
            }
        }

        if (isKicking)
        {
            if (Time.frameCount != kickStartFrame)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsTag("Attack") && !animator.IsInTransition(0))
                {
                    kickHitbox?.Deactivate();
                    isKicking = false;
                }
            }
        }

        if (isSmashing)
        {
            if (Time.frameCount != smashStartFrame)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsTag("Attack") && !animator.IsInTransition(0))
                {
                    smashHitbox?.Deactivate();
                    isSmashing = false;
                }
            }
        }
    }
}