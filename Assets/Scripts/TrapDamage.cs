using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    public enum VolumeShape { Sphere, Box }

    [Header("Hurt volume")]
    [SerializeField] private VolumeShape shape = VolumeShape.Sphere;

    [Tooltip("Centre of the hurt volume, relative to this object.")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Tooltip("Radius when using a Sphere volume.")]
    [SerializeField] private float radius = 1f;

    [Tooltip("Half-extents when using a Box volume.")]
    [SerializeField] private Vector3 halfExtents = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Damage")]
    [SerializeField] private float damage = 15f;

    [Tooltip("Seconds before the same player can be hurt again by this trap.")]
    [SerializeField] private float cooldown = 1f;

    [Tooltip("Only deal damage while this is true. Moving traps set it themselves, e.g. spears only hurt while extended.")]
    [SerializeField] private bool armed = true;

    [Header("Detection")]
    [Tooltip("Layers to search for the player. Player is on layer 3 by default.")]
    [SerializeField] private LayerMask playerLayer = 1 << 3;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;

    private float lastHitTime = -999f;
    private readonly Collider[] hits = new Collider[8];


    public void SetArmed(bool value)
    {
        armed = value;
    }

    private void Update()
    {
        if (!armed || PlayerState.Instance == null || PlayerState.Instance.IsDead)
        {
            return;
        }

        if (Time.time - lastHitTime < cooldown)
        {
            return;
        }

        Vector3 centre = transform.TransformPoint(offset);
        int count;

        if (shape == VolumeShape.Sphere)
        {
            count = Physics.OverlapSphereNonAlloc(centre, radius, hits, playerLayer, QueryTriggerInteraction.Collide);
        }
        else
        {
            count = Physics.OverlapBoxNonAlloc(centre, halfExtents, hits, transform.rotation, playerLayer, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            if (!hits[i].CompareTag("Player"))
            {
                continue;
            }

            PlayerState.Instance.TakeDamage(damage);
            lastHitTime = Time.time;
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
        {
            return;
        }

        Gizmos.color = armed ? new Color(1f, 0.3f, 0.2f, 0.55f) : new Color(0.5f, 0.5f, 0.5f, 0.35f);
        Vector3 centre = transform.TransformPoint(offset);

        if (shape == VolumeShape.Sphere)
        {
            Gizmos.DrawWireSphere(centre, radius);
        }
        else
        {
            Gizmos.matrix = Matrix4x4.TRS(centre, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        }
    }
}
