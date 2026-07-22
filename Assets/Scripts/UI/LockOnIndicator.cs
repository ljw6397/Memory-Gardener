using UnityEngine;

public class LockOnIndicator : MonoBehaviour
{
    public PlayerCombat playerCombat;
    public PlayerController playerController;

    [Header("Position")]
    public float verticalOffset = 1f;

    [Header("Bob (¼±ÅÃ, »ìÂ¦ ±îµüÀÌ´Â È¿°ú)")]
    public float bobSpeed = 4f;
    public float bobAmount = 0.05f; 

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetVisible(false);
    }

    void LateUpdate()
    {
        bool aiming = playerController != null && playerController.IsAiming;
        Transform target = playerCombat != null ? playerCombat.CurrentLockOnTarget : null;

        if (aiming && target != null)
        {
            SetVisible(true);
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = target.position + new Vector3(0f, verticalOffset + bob, 0f);
        }
        else
        {
            SetVisible(false);
        }
    }

    void SetVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
    }
}