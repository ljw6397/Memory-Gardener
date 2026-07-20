using UnityEngine;
using UnityEngine.U2D;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public PlayerController playerController;
    public Vector3 offset = new Vector3(0f, 1.5f, -15f);
    public float smoothTime = 0.15f;

    [Header("Pixel Perfect")]
    public float pixelsPerUnit = 32f;

    [Header("Aim Zoom")]
    public PixelPerfectCamera pixelPerfectCamera;
    public float aimZoomFactor = 0.85f;
    public float aimLookOffset = 1.2f;
    public float aimTransitionSpeed = 6f;

    [Header("Landing Shake")]
    public float shakeDuration = 0.25f;
    public float shakeMagnitudePixels = 3f;

    [Header("Hit Shake")]
    public float hitShakeDuration = 0.12f;      // ★ 추가: 타격 흔들림은 착지보다 짧고 톡톡 튀는 느낌으로
    public float hitShakeMagnitudePixels = 2f;  // ★ 추가: 세기도 착지보다 살짝 약하게 (기본값)

    private Vector3 velocity = Vector3.zero;
    private float lockedY;
    private Vector3 currentPosition;

    private int baseRefResX;
    private int baseRefResY;
    private float aimBlend = 0f;

    private float shakeTimer = 0f;
    private float currentShakeDuration = 0f;      // ★ 추가: 지금 재생 중인 흔들림이 착지용인지 타격용인지에 따라 다른 duration/magnitude를 씀
    private float currentShakeMagnitude = 0f;      // ★ 추가

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
        PunchHitbox.OnEnemyHit += TriggerHitShake; // ★ 추가: static 이벤트라 인스턴스 없이 바로 구독 가능
    }

    void OnDestroy()
    {
        if (playerController != null) playerController.OnSlamLand -= TriggerLandingShake;
        PunchHitbox.OnEnemyHit -= TriggerHitShake; // ★ 추가
    }

    void TriggerLandingShake()
    {
        shakeTimer = shakeDuration;
        currentShakeDuration = shakeDuration;
        currentShakeMagnitude = shakeMagnitudePixels;
    }

    // ★ 추가: 타격 흔들림 발동
    void TriggerHitShake()
    {
        shakeTimer = hitShakeDuration;
        currentShakeDuration = hitShakeDuration;
        currentShakeMagnitude = hitShakeMagnitudePixels;
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool aiming = playerController != null && playerController.IsAiming;
        aimBlend = Mathf.MoveTowards(aimBlend, aiming ? 1f : 0f, Time.deltaTime * aimTransitionSpeed);

        float facingDir = (playerController != null && playerController.FacingRight) ? 1f : -1f;
        float aimOffsetX = facingDir * aimLookOffset * aimBlend;

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
            float targetResX = Mathf.Lerp(baseRefResX, baseRefResX * aimZoomFactor, aimBlend);
            float targetResY = Mathf.Lerp(baseRefResY, baseRefResY * aimZoomFactor, aimBlend);
            pixelPerfectCamera.refResolutionX = Mathf.RoundToInt(targetResX);
            pixelPerfectCamera.refResolutionY = Mathf.RoundToInt(targetResY);
        }
    }
}