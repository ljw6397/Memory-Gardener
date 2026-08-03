using UnityEngine;
using System.Collections.Generic;

public class EnemyPunchHitbox : MonoBehaviour
{
    public int damage = 5;
    public float knockbackForce = 4f;
    public float knockbackUpward = 0.2f;

    private Collider2D col;
    private Enemy owner;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private Vector3 baseLocalPos;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = false;

        owner = GetComponentInParent<Enemy>();
        baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (owner == null) return;
        float dir = owner.FacingRight ? 1f : -1f;
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
        if (!other.CompareTag("Player")) return;
        if (hitTargets.Contains(other)) return;
        hitTargets.Add(other);

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        float dir = (owner != null && owner.FacingRight) ? 1f : -1f;

        // ★ 변경: normalized 제거 (위와 동일한 이유)
        Vector2 knockback = new Vector2(dir * knockbackForce, knockbackUpward);

        playerHealth.TakeDamage(damage, knockback);
    }
}