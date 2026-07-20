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
    private int attackStartFrame = -1;

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

        //조준 중 + 아직 공격 중 아님 + 땅 + 대시 중 아님 → 대시펀치 발동
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

    //공격 시작
    void StartAttack(int step)
    {
        comboStep = step;
        isAttacking = true;
        animator.SetInteger("ComboStep", comboStep);
        animator.SetTrigger("AttackTrigger");
        attackStartFrame = Time.frameCount;
    }

   //대쉬 어택 트리거
    void TriggerDashAttack()
    {
        comboStep = 0;
        queuedAttacks = 0;
        isAttacking = true; //이 걸로 공격중 확인하는거
        animator.SetTrigger("DashAttackTrigger");
        attackStartFrame = Time.frameCount;

        playerController.StartDashAttackBurst(); 
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

    //콤보 취소
    public void CancelCombo()
    {
        comboStep = 0;
        isAttacking = false;
        queuedAttacks = 0;
        comboExpireTime = -10f;
    }

    //공격중인가? 확인 하는 코드
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