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
    public float postAttackRecovery = 0.2f; // ★ 추가: 마지막 펀치가 끝난 뒤 추가로 조작을 막는 시간

    [Header("Hitboxes (펀치마다 손 위치가 달라서 각각 따로)")]
    public PunchHitbox punchHitboxA;
    public PunchHitbox punchHitboxB;
    public PunchHitbox punchHitboxC;

    [Header("Dash Attack Targeting")]
    public float dashAttackRange = 8f;

    private int comboStep = 0;
    private bool isAttacking = false;
    private int queuedAttacks = 0;
    private float comboExpireTime = -10f;
    private int attackStartFrame = -1;
    private bool waitingForNextComboStep = false;
    private float comboGapTimer = 0f;
    private Transform currentLockOnTarget;

    private bool isRecovering = false; // ★ 추가
    private float recoveryTimer = 0f;  // ★ 추가

    public bool IsAttacking => isAttacking;
    public bool IsRecovering => isRecovering; // ★ 추가
    public bool IsBusy => isAttacking || isRecovering; // ★ 추가: PlayerController가 이동/점프/방향전환 막을 때 이걸로 확인
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
        HandleSlamInput();
        UpdateComboGap();
        UpdateRecovery(); // ★ 추가
        SafetyCheck();
    }

    // ★ 추가: 현자타임 카운트다운
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
        if (isRecovering) return; // ★ 추가: 현자타임 중엔 클릭 자체를 무시

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

    void StartAttack(int step)
    {
        DeactivateAllHitboxes();

        comboStep = step;
        isAttacking = true;
        isRecovering = false; // ★ 추가: 새 공격 시작 시 확실히 꺼둠
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
            default: return null;
        }
    }

    void DeactivateAllHitboxes()
    {
        punchHitboxA?.Deactivate();
        punchHitboxB?.Deactivate();
        punchHitboxC?.Deactivate();
    }

    void UpdateLockOnTarget()
    {
        bool canDashAttack = playerController != null && playerController.IsAiming
            && !isAttacking && !isRecovering && playerController.IsGrounded; // ★ 변경

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
        isRecovering = false; // ★ 추가
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
        if (isAttacking || isRecovering) return; // ★ 변경
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
        isRecovering = false; // ★ 추가
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

            // ★ 추가: 콤보(또는 단발 펀치)가 진짜로 끝나는 순간부터 현자타임 시작
            // 이 시간이 "애니메이션이 실제로 다 끝나기 전 이벤트가 먼저 발동하는 틈"을 덮어주고,
            // 거기에 더해 펀치 후 딜레이(진짜 현자타임)까지 자연스럽게 생김
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
        isRecovering = false; // ★ 추가
        recoveryTimer = 0f;   // ★ 추가
    }

    void SafetyCheck()
    {
        if (!isAttacking) return;
        if (Time.frameCount == attackStartFrame) return;

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