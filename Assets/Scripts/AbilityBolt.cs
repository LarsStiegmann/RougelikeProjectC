using UnityEngine;

public class AbilityBolt : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float speed;
    private LayerMask enemyLayer;
    private float life;

    private const float HitDistance = 0.9f;
    private const float MaxLife = 4f;

    private AudioClip[] boltCastSounds;
    private AudioClip[] boltHitSounds;

    public void Launch(Transform newTarget, float newDamage, float newSpeed, LayerMask layer, Color colour, AudioClip[] boltCasts, AudioClip[] boltHits)
    {
        target = newTarget;
        damage = newDamage;
        speed = newSpeed;
        enemyLayer = layer;

        boltCastSounds = boltCasts;
        boltHitSounds = boltHits;

        AudioController.Instance.PlayRandomAudio(boltCastSounds, transform, 0.5f, true);

        Light glow = gameObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = colour;
        glow.range = 4f;
        glow.intensity = 2.2f;
    }
    //_________________________________________________________________________________________________________
    //Quelle: KI (Claude Opus 5)
    //Prompt: Generate code that throws a firebolt per Update() function with autoaim on enemies
    //Datum: 02.09.2026
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

                AudioController.Instance.PlayRandomAudio(boltHitSounds, transform, 0.5f, true);
            }

            Destroy(gameObject);
            return;
        }

        transform.forward = Vector3.Slerp(transform.forward, to.normalized, 12f * Time.deltaTime);
        transform.position += transform.forward * speed * Time.deltaTime;
        transform.Rotate(0f, 0f, 360f * Time.deltaTime, Space.Self);
    }
    //_________________________________________________________________________________________________________ 
}
