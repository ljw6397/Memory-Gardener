using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    [Header("Hit Stop")]
    public float hitStopDuration = 0.035f;

    private Coroutine currentHitStop;

    void OnEnable()
    {
        PunchHitbox.OnEnemyHit += TriggerHitStop;
    }

    void OnDisable()
    {
        PunchHitbox.OnEnemyHit -= TriggerHitStop;
    }

    void TriggerHitStop()
    {
        if (currentHitStop != null) return;

        currentHitStop = StartCoroutine(DoHitStop());
    }

    IEnumerator DoHitStop()
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(hitStopDuration);

        // ★ 변경: 그냥 항상 정상 속도로 복귀. 슬로우모션이 그 이후에 다시 자기 값을 세팅하는 건
        // PlayerHealth.HandleTimeScaleRecovery()가 매 프레임 알아서 처리해주니 여기서 신경 안 써도 됨.
        Time.timeScale = 1f;

        currentHitStop = null;
    }
}