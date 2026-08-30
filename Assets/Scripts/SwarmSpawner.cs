using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Keeps a swarm of enemies around the player, Megabonk style. Rather than garrisoning
/// rooms, it tops the population back up to a target count by spawning on a ring around
/// the player, biased toward wherever the camera is looking so the player sees them
/// arrive rather than being ambushed from behind.
///
/// Enemies that fall a long way behind are culled, which keeps the count bounded on a
/// large map and stops a trail of stragglers building up across the dungeon.
///
/// Which enemies appear is decided by the RoomZone the player currently occupies, so
/// the per-room theming survives the change of spawn model.
/// </summary>
public class SwarmSpawner : MonoBehaviour
{
    [Header("Enemy source")]
    [Tooltip("The configured enemy prefab. Every mesh child should be disabled on it.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Used when the player is not inside any RoomZone.")]
    [SerializeField] private List<EnemyVariant> fallbackRoster = new List<EnemyVariant>();

    [Header("Population")]
    [Tooltip("How many enemies to keep alive around the player.")]
    [SerializeField] private int targetAlive = 12;

    [Tooltip("How many may be spawned per batch, to avoid a whole wave appearing in one frame.")]
    [SerializeField] private int maxSpawnsPerBatch = 2;

    [Tooltip("Seconds between spawn batches.")]
    [SerializeField] private float spawnInterval = 0.6f;

    [Header("Placement ring")]
    [Tooltip("Closest an enemy may appear. Keep this outside melee range.")]
    [SerializeField] private float minSpawnDistance = 14f;

    [Tooltip("Furthest an enemy may appear.")]
    [SerializeField] private float maxSpawnDistance = 22f;

    [Tooltip("Fraction of spawns placed within the camera's view cone so the player " +
             "sees them coming. The rest are scattered anywhere on the ring.")]
    [Range(0f, 1f)]
    [SerializeField] private float inViewFraction = 0.75f;

    [Tooltip("Half-angle of the view cone used for in-view spawns, in degrees.")]
    [SerializeField] private float viewHalfAngle = 50f;

    [Header("Culling")]
    [Tooltip("Enemies further than this from the player are removed.")]
    [SerializeField] private float cullDistance = 45f;

    [Tooltip("Seconds between cull sweeps.")]
    [SerializeField] private float cullInterval = 2f;

    [Header("Startup")]
    [Tooltip("Seconds to wait after the run starts before the first spawn.")]
    [SerializeField] private float startDelay = 1.5f;

    private readonly List<GameObject> alive = new List<GameObject>();
    private Transform nursery;
    private Camera cam;
    private float nextSpawnTime;
    private float nextCullTime;

    /// <summary>How many swarm enemies are currently alive.</summary>
    public int AliveCount => alive.Count;

    /// <summary>The room whose roster is currently being drawn from.</summary>
    public string CurrentRoom { get; private set; } = "-";

    private void Awake()
    {
        // Enemies are assembled in here while inactive, so Configure lands before their
        // Awake copies maxHealth into currentHealth.
        GameObject holder = new GameObject("~nursery");
        holder.transform.SetParent(transform, false);
        holder.SetActive(false);
        nursery = holder.transform;

        nextSpawnTime = Time.time + startDelay;
    }

    private void Update()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
            {
                alive.RemoveAt(i);
            }
        }

        if (PlayerState.Instance == null || PlayerState.Instance.IsDead)
        {
            return;
        }

        Vector3 playerPos = PlayerState.Instance.transform.position;

        if (Time.time >= nextCullTime)
        {
            nextCullTime = Time.time + cullInterval;
            CullDistant(playerPos);
        }

        if (Time.time < nextSpawnTime || alive.Count >= targetAlive)
        {
            return;
        }

        nextSpawnTime = Time.time + spawnInterval;

        int wanted = Mathf.Min(maxSpawnsPerBatch, targetAlive - alive.Count);
        for (int i = 0; i < wanted; i++)
        {
            SpawnOne(playerPos);
        }
    }

    private void CullDistant(Vector3 playerPos)
    {
        float sqrCull = cullDistance * cullDistance;
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            GameObject e = alive[i];
            if (e == null)
            {
                alive.RemoveAt(i);
                continue;
            }

            if ((e.transform.position - playerPos).sqrMagnitude > sqrCull)
            {
                Destroy(e);
                alive.RemoveAt(i);
            }
        }
    }

    private List<EnemyVariant> CurrentRoster()
    {
        RoomZone zone = PlayerState.Instance != null
            ? RoomZone.FindFor(PlayerState.Instance.transform.position)
            : null;

        if (zone != null && zone.Roster.Count > 0)
        {
            CurrentRoom = zone.RoomName;
            return zone.Roster;
        }

        CurrentRoom = "-";
        return fallbackRoster;
    }

    private EnemyVariant PickVariant(List<EnemyVariant> roster)
    {
        if (roster == null || roster.Count == 0)
        {
            return null;
        }

        float minutes = RunTimerController.Instance != null
            ? RunTimerController.Instance.ElapsedTime / 60f
            : 0f;

        float total = 0f;
        foreach (EnemyVariant v in roster)
        {
            if (v != null && minutes >= v.unlockAtMinutes)
            {
                total += Mathf.Max(0f, v.weight);
            }
        }

        // Nothing unlocked yet: fall back to whichever unlocks earliest.
        if (total <= 0f)
        {
            EnemyVariant earliest = null;
            foreach (EnemyVariant v in roster)
            {
                if (v == null)
                {
                    continue;
                }

                if (earliest == null || v.unlockAtMinutes < earliest.unlockAtMinutes)
                {
                    earliest = v;
                }
            }

            return earliest;
        }

        float roll = Random.Range(0f, total);
        foreach (EnemyVariant v in roster)
        {
            if (v == null || minutes < v.unlockAtMinutes)
            {
                continue;
            }

            roll -= Mathf.Max(0f, v.weight);
            if (roll <= 0f)
            {
                return v;
            }
        }

        return null;
    }

    /// <summary>
    /// Picks a spot on the ring around the player. Most spawns land inside the camera's
    /// view cone so the player watches them arrive; the remainder are scattered so the
    /// swarm does not only ever come from straight ahead.
    /// </summary>
    private bool TryFindSpawnPoint(Vector3 playerPos, out Vector3 result)
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        for (int attempt = 0; attempt < 24; attempt++)
        {
            float angle;

            if (cam != null && attempt < 18 && Random.value < inViewFraction)
            {
                Vector3 fwd = cam.transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.001f)
                {
                    fwd = Vector3.forward;
                }

                float baseAngle = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                angle = baseAngle + Random.Range(-viewHalfAngle, viewHalfAngle);
            }
            else
            {
                angle = Random.Range(0f, 360f);
            }

            float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 candidate = playerPos + dir * dist;

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(candidate, out hit, 6f, NavMesh.AllAreas))
            {
                continue;
            }

            // The sample can snap somewhere much closer; reject those so nothing
            // materialises on top of the player.
            float actual = Vector3.Distance(hit.position, playerPos);
            if (actual < minSpawnDistance * 0.75f || actual > maxSpawnDistance * 1.4f)
            {
                continue;
            }

            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private void SpawnOne(Vector3 playerPos)
    {
        if (enemyPrefab == null)
        {
            return;
        }

        EnemyVariant variant = PickVariant(CurrentRoster());
        if (variant == null)
        {
            return;
        }

        Vector3 point;
        if (!TryFindSpawnPoint(playerPos, out point))
        {
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, nursery);
        enemy.name = variant.displayName + "_swarm";

        bool meshFound = false;
        foreach (Transform child in enemy.transform)
        {
            if (child.name == "Root")
            {
                continue;
            }

            bool match = child.name == variant.meshChildName;
            child.gameObject.SetActive(match);
            meshFound |= match;
        }

        if (!meshFound)
        {
            Debug.LogWarning("SwarmSpawner: no mesh child named '" + variant.meshChildName
                + "' on the enemy prefab.", this);
        }

        EnemyAIController ai = enemy.GetComponent<EnemyAIController>();
        if (ai != null)
        {
            ai.Configure(variant.maxHealth, variant.damage, variant.xpReward);
            ai.ConfigureCoins(variant.coinReward);
        }

        enemy.transform.localScale = Vector3.one * variant.modelScale;
        enemy.transform.SetParent(null, false);
        enemy.transform.position = point;
        enemy.transform.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(playerPos - point, Vector3.up).normalized, Vector3.up);

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        enemy.SetActive(true);

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(point);
        }

        alive.Add(enemy);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || PlayerState.Instance == null)
        {
            return;
        }

        Vector3 p = PlayerState.Instance.transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(p, minSpawnDistance);
        Gizmos.DrawWireSphere(p, maxSpawnDistance);
        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(p, cullDistance);
    }
}
