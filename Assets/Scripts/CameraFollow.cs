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
    public float shakeMagnitudePixels = 3f; // 흔들리는 폭 (픽셀 단위)

    private Vector3 velocity = Vector3.zero;
    private float lockedY;
    private Vector3 currentPosition;

    private int baseRefResX;
    private int baseRefResY;
    private float aimBlend = 0f;
    private float shakeTimer = 0f;

    void Start()
    {
        currentPosition = transform.position;

        if (pixelPerfectCamera == null) pixelPerfectCamera = GetComponent<PixelPerfectCamera>();

        if (pixelPerfectCamera != null)
        {
            baseRefResX = pixelPerfectCamera.refResolutionX;
            baseRefResY = pixelPerfectCamera.refResolutionY;
        }

        // ★ 추가: 플레이어가 착지 슬램하면 흔들림 이벤트 구독
        if (playerController != null) playerController.OnSlamLand += TriggerLandingShake;
    }

    void OnDestroy()
    {
        if (playerController != null) playerController.OnSlamLand -= TriggerLandingShake;
    }

    void TriggerLandingShake()
    {
        shakeTimer = shakeDuration;
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

        //착지 흔들림 
        float shakeOffsetX = 0f;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float decay = Mathf.Clamp01(shakeTimer / shakeDuration);
            float unitsPerPixelShake = 1f / pixelsPerUnit;
            shakeOffsetX = Random.Range(-1f, 1f) * shakeMagnitudePixels * unitsPerPixelShake * decay;
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