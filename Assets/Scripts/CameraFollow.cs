using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public PlayerController playerController;
    public Vector3 offset = new Vector3(0f, 1.5f, -15f);
    public float smoothTime = 0.15f;

    [Header("Pixel Perfect")]
    public float pixelsPerUnit = 32f;

    private Vector3 velocity = Vector3.zero;
    private float lockedY;
    private Vector3 currentPosition; // ★ 반올림 안 된 '진짜' 카메라 위치 (SmoothDamp 전용)

    void Start()
    {
        currentPosition = transform.position; // 초기값 맞춰두기
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPosition;

        if (playerController.IsGrounded)
        {
            desiredPosition = target.position + offset;
            lockedY = target.position.y + offset.y;
        }
        else
        {
            desiredPosition = new Vector3(target.position.x + offset.x, lockedY, target.position.z + offset.z);
        }

        // ★ SmoothDamp는 항상 '순수한 연속값'(currentPosition) 기준으로만 진행 — transform.position은 안 건드림
        currentPosition = Vector3.SmoothDamp(currentPosition, desiredPosition, ref velocity, smoothTime);

        // 화면에 보여줄 때만 픽셀 격자에 맞춰 반올림 (currentPosition 자체는 그대로 순수하게 유지됨)
        float unitsPerPixel = 1f / pixelsPerUnit;
        float snappedX = Mathf.Round(currentPosition.x / unitsPerPixel) * unitsPerPixel;
        float snappedY = Mathf.Round(currentPosition.y / unitsPerPixel) * unitsPerPixel;

        transform.position = new Vector3(snappedX, snappedY, currentPosition.z);
    }
}