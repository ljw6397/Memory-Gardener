using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;

    [Header("Combo Settings")]
    public int maxComboStep = 3;
    public float comboGraceWindow = 0.5f;

    [Header("Hitboxes (펀치마다 손 위치가 달라서 각각 따로)")]
    public PunchHitbox punchHitboxA;
    public PunchHitbox punchHitboxB;
    public PunchHitbox punchHitboxC;

    [Header("Dash Attack Targeting")]
    public float dashAttackLockOnRadius = 6f; // ★ 추가: 마우스 커서 기준 이 반경 안의 적만 타겟으로 잡음

    private int comboStep = 0;
    private bool isAttacking = false;
    private int queuedAttacks = 0;
    private float comboExpireTime = -10f;
    private int attackStartFrame = -1;

    public bool IsAttacking => isAttacking;

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
        HandleAttackInput();
        HandleSlamInput();
        SafetyCheck();
    }

    void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (playerController != null && playerController.IsAiming && !isAttacking
            && playerController.IsGrounded && !playerController.IsDashing)
        {
            TriggerDashAttack();
            return;
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

    public void AnimEvent_DashAttackFinished()
    {
        StartAttack(1);
    }

    void TriggerDashAttack()
    {
        comboStep = 0;
        queuedAttacks = 0;
        isAttacking = true;
        animator.SetTrigger("DashAttackTrigger");
        attackStartFrame = Time.frameCount;

        Transform target = FindDashAttackTarget(); // ★ 추가
        playerController.StartDashAttackBurst(target); // ★ 변경: 타겟 전달
    }

    Transform FindDashAttackTarget()
    {
        if (playerController == null) return null;

        Vector3 mouseWorldPos = playerController.GetMouseWorldPosition();
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        Transform closest = null;
        float closestDist = dashAttackLockOnRadius;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.IsDead) continue;

            float dist = Vector2.Distance(mouseWorldPos, enemy.transform.position);
            if (dist <= closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        return closest;
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
        isAttacking = false;

        if (queuedAttacks > 0 && comboStep < maxComboStep)
        {
            queuedAttacks--;
            StartAttack(comboStep + 1);
        }
        else
        {
            queuedAttacks = 0;
            comboExpireTime = Time.time + comboGraceWindow;
        }
    }

    public void CancelCombo()
    {
        comboStep = 0;
        isAttacking = false;
        queuedAttacks = 0;
        comboExpireTime = -10f;
    }

    void SafetyCheck()
    {
        if (!isAttacking) return;
        if (Time.frameCount == attackStartFrame) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (!state.IsTag("Attack") && !animator.IsInTransition(0))
        {
            isAttacking = false;
            comboStep = 0;
            queuedAttacks = 0;
        }
    }

    
}