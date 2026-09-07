using UnityEngine;

/// <summary>
/// A single seeking bolt fired by the Homing Bolts ability. Flies at its target,
/// damages it on arrival and fades out. Dies on its own if the target is killed
/// by something else first.
/// </summary>
public class AbilityBolt : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float speed;
    private LayerMask enemyLayer;
    private float life;

    private const float HitDistance = 0.9f;
    private const float MaxLife = 4f;

    public void Launch(Transform newTarget, float newDamage, float newSpeed, LayerMask layer, Color colour)
    {
        target = newTarget;
        damage = newDamage;
        speed = newSpeed;
        enemyLayer = layer;

        Light glow = gameObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = colour;
        glow.range = 4f;
        glow.intensity = 2.2f;
    }

    private void Update()
    {
        life += Time.deltaTime;
        if (life > MaxLife)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null)
        {
            // Target died mid-flight: keep going briefly, then expire.
            transform.position += transform.forward * speed * Time.deltaTime;
            if (life > 0.6f)
            {
                Destroy(gameObject);
            }
            return;
        }

        Vector3 aim = target.position + Vector3.up * 0.9f;
        Vector3 to = aim - transform.position;

        if (to.magnitude <= HitDistance)
        {
            EnemyAIController enemy = target.GetComponentInParent<EnemyAIController>();
            if (enemy != null)
            {
                enemy.EnemyTakeDamage(damage);
            }

            Destroy(gameObject);
            return;
        }

        transform.forward = Vector3.Slerp(transform.forward, to.normalized, 12f * Time.deltaTime);
        transform.position += transform.forward * speed * Time.deltaTime;
        transform.Rotate(0f, 0f, 360f * Time.deltaTime, Space.Self);
    }
}
