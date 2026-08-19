using UnityEngine;
using UnityEngine.U2D;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public PlayerController playerController;
    public PlayerCombat playerCombat;
    public Vector3 offset = new Vector3(0f, 1.5f, -15f);
    public float smoothTime = 0.15f;

    [Header("Pixel Perfect")]
    public float pixelsPerUnit = 32f;

    [Header("Aim Zoom (평소 조준, 락온 대상 없을 때)")]
    public PixelPerfectCamera pixelPerfectCamera;
    public float aimZoomFactor = 0.85f;
    public float aimLookOffset = 1.2f;
    public float aimTransitionSpeed = 6f;

    [Header("Lock-On Zoom (락온 대상 있을 때, 더 확대)")]
    public float lockOnZoomFactor = 0.65f;
    public float lockOnOffsetFraction = 0.5f;
    public float maxLockOnOffsetX = 3f;
    public float lockOnTransitionSpeed = 6f;

    [Header("Landing Shake")]
    public float shakeDuration = 0.25f;
    public float shakeMagnitudePixels = 3f;

    [Header("Hit Shake - 적을 때렸을 때")] // ★ 변경
    public float hitShakeDuration = 0.15f;           // ★ 조금 늘림
    public float hitShakeMagnitudeMin = 3f;          // ★ 추가: 랜덤 최소 세기
    public float hitShakeMagnitudeMax = 6f;          // ★ 추가: 랜덤 최대 세기 (기존 2보다 훨씬 세짐)

    [Header("Power Hit Shake - 파워펀치로 때렸을 때 (더 크게)")] // ★ 추가
    public float powerHitShakeDuration = 0.25f;
    public float powerHitShakeMagnitudeMin = 8f;
    public float powerHitShakeMagnitudeMax = 12f;

    [Header("Player Hit Shake - 내가 맞았을 때")] // ★ 추가
    public float playerHitShakeDuration = 0.15f;
    public float playerHitShakeMagnitudeMin = 1f;    // ★ 적 때릴 때보다 약하게
    public float playerHitShakeMagnitudeMax = 2f;

    private Vector3 velocity = Vector3.zero;
    private float lockedY;
    private Vector3 currentPosition;

    private int baseRefResX;
    private int baseRefResY;
    private float aimBlend = 0f;
    private float lockBlend = 0f;
    private float lastLockOnOffsetX = 0f;

    private float shakeTimer = 0f;
    private float currentShakeDuration = 0f;
    private float currentShakeMagnitude = 0f;

    void Start()
    {
        currentPosition = transform.position;

        if (pixelPerfectCamera == null) pixelPerfectCamera = GetComponent<PixelPerfectCamera>();

        if (pixelPerfectCamera != null)
        {
            baseRefResX = pixelPerfectCamera.refResolutionX;
            baseRefResY = pixelPerfectCamera.refResolutionY;
        }

        if (playerController != null) playerController.OnSlamLand += TriggerLandingShake;
        PunchHitbox.OnEnemyHit += TriggerHitShake;
        PunchHitbox.OnPowerHit += TriggerPowerHitShake;
        EnemyPunchHitbox.OnPlayerHit += TriggerPlayerHitShake; // ★ 추가
    }

    void OnDestroy()
    {
        if (playerController != null) playerController.OnSlamLand -= TriggerLandingShake;
        PunchHitbox.OnEnemyHit -= TriggerHitShake;
        EnemyPunchHitbox.OnPlayerHit -= TriggerPlayerHitShake; // ★ 추가
        PunchHitbox.OnPowerHit -= TriggerPowerHitShake;
    }

    void TriggerLandingShake()
    {
        shakeTimer = shakeDuration;
        currentShakeDuration = shakeDuration;
        currentShakeMagnitude = shakeMagnitudePixels;
    }

    // ★ 변경: 랜덤 세기로 뽑음
    void TriggerHitShake()
    {
        shakeTimer = hitShakeDuration;
        currentShakeDuration = hitShakeDuration;
        currentShakeMagnitude = Random.Range(hitShakeMagnitudeMin, hitShakeMagnitudeMax);
    }

    void TriggerPowerHitShake()
    {
        shakeTimer = powerHitShakeDuration;
        currentShakeDuration = powerHitShakeDuration;
        currentShakeMagnitude = Random.Range(powerHitShakeMagnitudeMin, powerHitShakeMagnitudeMax);
    }

    // ★ 추가: 플레이어가 맞았을 때 (더 약한 랜덤 세기)
    void TriggerPlayerHitShake()
    {
        shakeTimer = playerHitShakeDuration;
        currentShakeDuration = playerHitShakeDuration;
        currentShakeMagnitude = Random.Range(playerHitShakeMagnitudeMin, playerHitShakeMagnitudeMax);
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool aiming = playerController != null && playerController.IsAiming;
        aimBlend = Mathf.MoveTowards(aimBlend, aiming ? 1f : 0f, Time.deltaTime * aimTransitionSpeed);

        float facingDir = (playerController != null && playerController.FacingRight) ? 1f : -1f;
        float normalAimOffsetX = facingDir * aimLookOffset;

        Transform lockedEnemy = (playerCombat != null) ? playerCombat.CurrentLockOnTarget : null;
        bool hasLockTarget = lockedEnemy != null;
        lockBlend = Mathf.MoveTowards(lockBlend, hasLockTarget ? 1f : 0f, Time.deltaTime * lockOnTransitionSpeed);

        float lockOnOffsetX;
        if (lockedEnemy != null)
        {
            float raw = (lockedEnemy.position.x - target.position.x) * lockOnOffsetFraction;
            raw = Mathf.Clamp(raw, -maxLockOnOffsetX, maxLockOnOffsetX);
            lastLockOnOffsetX = raw;
            lockOnOffsetX = raw;
        }
        else
        {
            lockOnOffsetX = lastLockOnOffsetX;
        }

        float blendedOffsetX = Mathf.Lerp(normalAimOffsetX, lockOnOffsetX, lockBlend);
        float aimOffsetX = blendedOffsetX * aimBlend;

        Vector3 desiredPosition;

        if (playerController.IsGrounded)
        {
            desiredPosition = target.position + offset;
            desiredPosition.x += aimOffsetX;
            lockedY = target.position.y + offset.y;
        }
        else
        {
            desiredPosition = new Vector3(target.position.x + offset.x + aimOffsetX, lockedY, target.position.z + offset.z);
        }

        currentPosition = Vector3.SmoothDamp(currentPosition, desiredPosition, ref velocity, smoothTime);

        float shakeOffsetX = 0f;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float decay = Mathf.Clamp01(shakeTimer / currentShakeDuration);
            float unitsPerPixelShake = 1f / pixelsPerUnit;
            shakeOffsetX = Random.Range(-1f, 1f) * currentShakeMagnitude * unitsPerPixelShake * decay;
        }

        float unitsPerPixel = 1f / pixelsPerUnit;
        float snappedX = Mathf.Round((currentPosition.x + shakeOffsetX) / unitsPerPixel) * unitsPerPixel;
        float snappedY = Mathf.Round(currentPosition.y / unitsPerPixel) * unitsPerPixel;

        transform.position = new Vector3(snappedX, snappedY, currentPosition.z);

        if (pixelPerfectCamera != null)
        {
            float zoomFactor = Mathf.Lerp(aimZoomFactor, lockOnZoomFactor, lockBlend);

            float targetResX = Mathf.Lerp(baseRefResX, baseRefResX * zoomFactor, aimBlend);
            float targetResY = Mathf.Lerp(baseRefResY, baseRefResY * zoomFactor, aimBlend);
            pixelPerfectCamera.refResolutionX = Mathf.RoundToInt(targetResX);
            pixelPerfectCamera.refResolutionY = Mathf.RoundToInt(targetResY);
        }
    }
}