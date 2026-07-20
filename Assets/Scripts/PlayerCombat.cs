using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;

    [Header("Combo Settings")]
    public int maxComboStep = 3;
    public float comboGraceWindow = 0.5f;

    private int comboStep = 0;
    private bool isAttacking = false;
    private int queuedAttacks = 0;
    private float comboExpireTime = -10f;
    private int attackStartFrame = -1; // ★ 추가: 공격을 시작한 프레임 번호 기억

    public bool IsAttacking => isAttacking;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        HandleAttackInput();
        SafetyCheck();
    }

    void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
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
        attackStartFrame = Time.frameCount; // ★ 추가: 지금 프레임 기록
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

        // ★ 추가: 공격을 시작한 바로 그 프레임엔 아직 애니메이터가 전환 전이라 체크하면 안 됨 → 건너뜀
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