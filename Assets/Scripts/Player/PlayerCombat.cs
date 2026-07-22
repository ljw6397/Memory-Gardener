using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;

    [Header("Combo Settings")]
    public int maxComboStep = 3;
    public float comboGraceWindow = 0.5f;
    public float comboGapDelay = 0.08f;

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

    public bool IsAttacking => isAttacking;
    public Transform CurrentLockOnTarget => currentLockOnTarget;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();

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
        UpdateLockOnTarget();
        HandleAttackInput();
        HandleSlamInput();
        UpdateComboGap();
        SafetyCheck();
    }

    void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (playerController != null && playerController.IsAiming && !isAttacking
            && playerController.IsGrounded && !playerController.IsDashing)
        {
            if (currentLockOnTarget != null)
            {
                TriggerDashAttack(currentLockOnTarget);
                return;
            }
        }

        if (playerController != null && (!playerController.IsGrounded || playerController.IsDashing)) return;

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
            && !isAttacking && playerController.IsGrounded && !playerController.IsDashing;

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
        if (isAttacking) return;
        if (playerController == null) return;
        if (playerController.IsGrounded) return;
        if (playerController.IsDashing) return;
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