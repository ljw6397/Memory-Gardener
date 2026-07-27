using UnityEngine;

public class Grunt1 : Enemy
{
    private enum State { Patrol, Chase, Attack, Exhausted }

    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 2.5f;

    [Header("Patrol")]
    public float patrolRange = 3f;
    public float idlePauseMin = 1f;
    public float idlePauseMax = 2.5f;

    [Header("Chase / Attack")]
    public float attackRange = 1.2f;
    public float exhaustionDuration = 1.2f;

    [Header("Hitboxes (펀치마다 손 위치가 달라서 각각 따로)")]
    public EnemyPunchHitbox punchHitboxA;
    public EnemyPunchHitbox punchHitboxB;
    public EnemyPunchHitbox punchHitboxC;

    private State state = State.Patrol;
    private Vector2 spawnPos;
    private float patrolTargetX;
    private float idleTimer;

    private int comboStep = 0;
    private bool isAttacking = false;
    private int attackStartFrame = -1;
    private float exhaustedTimer = 0f;

    protected override void Start()
    {
        base.Start();
        spawnPos = transform.position;
        PickNewPatrolPoint();
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;
        if (IsKnockedBack) return;

        // ★ 추가: 공격 도중 타겟(플레이어)이 죽었으면 그 즉시 공격 중단
        if (isAttacking && !HasTarget)
        {
            CancelAttack();
        }

        if (isAttacking)
        {
            UpdateAttack();
            return;
        }

        if (state == State.Exhausted)
        {
            UpdateExhausted();
            return;
        }

        if (!HasTarget)
        {
            UpdatePatrol();
        }
        else
        {
            UpdateChaseOrAttack();
        }
    }

    void UpdatePatrol()
    {
        state = State.Patrol;

        if (idleTimer > 0f)
        {
            idleTimer -= Time.deltaTime;
            SetVelocityX(0f);
            animator.SetFloat("Speed", 0f);
            return;
        }

        float dir = Mathf.Sign(patrolTargetX - transform.position.x);
        SetVelocityX(dir * patrolSpeed);
        spriteRenderer.flipX = dir < 0f;
        animator.SetFloat("Speed", patrolSpeed);

        if (Mathf.Abs(transform.position.x - patrolTargetX) < 0.1f)
        {
            SetVelocityX(0f);
            idleTimer = Random.Range(idlePauseMin, idlePauseMax);
            PickNewPatrolPoint();
        }
    }

    void PickNewPatrolPoint()
    {
        patrolTargetX = spawnPos.x + Random.Range(-patrolRange, patrolRange);
    }

    void UpdateChaseOrAttack()
    {
        float distX = Mathf.Abs(Target.position.x - transform.position.x);

        if (distX <= attackRange)
        {
            StartAttack();
            return;
        }

        state = State.Chase;
        float dir = Mathf.Sign(Target.position.x - transform.position.x);
        SetVelocityX(dir * chaseSpeed);
        FaceTarget();
        animator.SetFloat("Speed", chaseSpeed);
    }

    void StartAttack()
    {
        state = State.Attack;
        isAttacking = true;
        comboStep = 1;
        SetVelocityX(0f);
        FaceTarget();
        animator.SetInteger("ComboStep", comboStep);
        animator.SetTrigger("AttackTrigger");
        attackStartFrame = Time.frameCount;
    }

    void UpdateAttack()
    {
        SetVelocityX(0f);

        if (Time.frameCount == attackStartFrame) return;

        AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(0);
        if (!s.IsTag("Attack") && !animator.IsInTransition(0))
        {
            EndAttackToExhausted();
        }
    }

    public void AnimEvent_ComboStepFinished()
    {
        if (comboStep < 3)
        {
            comboStep++;
            animator.SetInteger("ComboStep", comboStep);
            animator.SetTrigger("AttackTrigger");
            attackStartFrame = Time.frameCount;
        }
        else
        {
            EndAttackToExhausted();
        }
    }

    void EndAttackToExhausted()
    {
        CancelAttack();
        state = State.Exhausted;
        exhaustedTimer = exhaustionDuration;
    }

    void UpdateExhausted()
    {
        SetVelocityX(0f);
        animator.SetFloat("Speed", 0f);

        exhaustedTimer -= Time.deltaTime;
        if (exhaustedTimer <= 0f)
        {
            state = State.Chase;
        }
    }

    public void AnimEvent_HitboxOn(string which) => GetHitbox(which)?.Activate();
    public void AnimEvent_HitboxOff(string which) => GetHitbox(which)?.Deactivate();

    EnemyPunchHitbox GetHitbox(string which)
    {
        switch (which)
        {
            case "A": return punchHitboxA;
            case "B": return punchHitboxB;
            case "C": return punchHitboxC;
            default: return null;
        }
    }

    void CancelAttack()
    {
        DeactivateAllHitboxes();
        isAttacking = false;
        comboStep = 0;
    }

    void DeactivateAllHitboxes()
    {
        punchHitboxA?.Deactivate();
        punchHitboxB?.Deactivate();
        punchHitboxC?.Deactivate();
    }

    protected override void OnHit()
    {
        CancelAttack();
        state = State.Exhausted;
        exhaustedTimer = exhaustionDuration * 0.5f;
    }

    void SetVelocityX(float x)
    {
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }
}