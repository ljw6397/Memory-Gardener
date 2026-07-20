using UnityEngine;
using System.Collections.Generic;

public class PunchHitbox : MonoBehaviour
{
    public int damage = 10;
    public float knockbackForce = 6f;
    public float knockbackUpward = 0.3f;

    private Collider2D col;
    private PlayerController playerController;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private Vector3 baseLocalPos;

    public static System.Action OnEnemyHit;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;

        playerController = GetComponentInParent<PlayerController>();
        baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (playerController == null) return;

        float dir = playerController.FacingRight ? 1f : -1f;
        transform.localPosition = new Vector3(baseLocalPos.x * dir, baseLocalPos.y, baseLocalPos.z);
    }

    public void Activate()
    {
        hitTargets.Clear();
        col.enabled = true;
    }

    public void Deactivate()
    {
        col.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (hitTargets.Contains(other)) return;

        hitTargets.Add(other);

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        float dir = (playerController != null && playerController.FacingRight) ? 1f : -1f;
        Vector2 knockback = new Vector2(dir, knockbackUpward).normalized * knockbackForce;

        enemy.TakeDamage(damage, knockback);

        OnEnemyHit?.Invoke(); 
    }
}