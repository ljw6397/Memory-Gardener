using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    [Header("Hit Stop")]
    public float hitStopDuration = 0.035f;

    [Header("Power Hit Stop (더 긴 히트스탑)")] // ★ 추가
    public float powerHitStopDuration = 0.09f;

    private Coroutine currentHitStop;

    void OnEnable()
    {
        PunchHitbox.OnEnemyHit += TriggerHitStop;
        PunchHitbox.OnPowerHit += TriggerPowerHitStop; // ★ 추가
    }

    void OnDisable()
    {
        PunchHitbox.OnEnemyHit -= TriggerHitStop;
        PunchHitbox.OnPowerHit -= TriggerPowerHitStop; // ★ 추가
    }

    void TriggerHitStop()
    {
        StartHitStop(hitStopDuration);
    }

    void TriggerPowerHitStop() // ★ 추가
    {
        StartHitStop(powerHitStopDuration);
    }

    void StartHitStop(float duration) // ★ 변경: 공통 로직을 파라미터화
    {
        if (currentHitStop != null) StopCoroutine(currentHitStop);
        currentHitStop = StartCoroutine(DoHitStop(duration));
    }

    IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        currentHitStop = null;
    }
}