using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    [Header("Hit Stop")]
    public float hitStopDuration = 0.035f;

    [Header("References")]
    public PlayerHealth playerHealth; // ★ 추가

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

        Time.timeScale = 1f;
        currentHitStop = null;
    }
}