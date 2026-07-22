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

    [Header("Hit Shake")]
    public float hitShakeDuration = 0.12f;
    public float hitShakeMagnitudePixels = 2f;

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
    }

    void OnDestroy()
    {
        if (playerController != null) playerController.OnSlamLand -= TriggerLandingShake;
        PunchHitbox.OnEnemyHit -= TriggerHitShake;
    }

    void TriggerLandingShake()
    {
        shakeTimer = shakeDuration;
        currentShakeDuration = shakeDuration;
        currentShakeMagnitude = shakeMagnitudePixels;
    }

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