using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Keeps one room garrisoned. It fills its spawn points with enemies drawn from a
/// weighted roster, watches them, and once the whole group is dead waits a delay and
/// refills with a slightly larger and tougher group.
///
/// Enemies are built inside an inactive holder first, so Configure() lands before the
/// enemy's Awake copies maxHealth into currentHealth.
///
/// Spawning is skipped while the player is close enough to watch it happen, so groups
/// do not pop into existence in front of them.
/// </summary>
public class RoomSpawner : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Room name, for readability in the hierarchy and logs.")]
    [SerializeField] private string roomName = "Room";

    [Header("Enemy source")]
    [Tooltip("The configured enemy prefab. Every mesh child should be disabled on it.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Which enemy types this room can spawn.")]
    [SerializeField] private List<EnemyVariant> roster = new List<EnemyVariant>();

    [Header("Spawn points")]
    [Tooltip("Where enemies appear. Each should sit on the NavMesh.")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Group size")]
    [Tooltip("How many enemies the first group holds.")]
    [SerializeField] private int baseGroupSize = 3;

    [Tooltip("Extra enemies added per full minute of run time.")]
    [SerializeField] private float extraPerMinute = 0.5f;

    [Tooltip("Hard ceiling on group size, so a long run cannot flood a room.")]
    [SerializeField] private int maxGroupSize = 8;

    [Header("Respawn")]
    [Tooltip("Seconds after the last enemy dies before the room refills.")]
    [SerializeField] private float respawnDelay = 12f;

    [Tooltip("The player must be at least this far away for a refill to happen, " +
             "so groups never appear in view.")]
    [SerializeField] private float safeSpawnDistance = 18f;

    [Header("Scaling")]
    [Tooltip("Fraction of health added per full minute of run time. 0.15 means +15% a minute.")]
    [SerializeField] private float healthGrowthPerMinute = 0.15f;

    [Tooltip("Ceiling on the health multiplier.")]
    [SerializeField] private float maxHealthMultiplier = 4f;

    [Header("Startup")]
    [Tooltip("Fill the room as soon as the game starts.")]
    [SerializeField] private bool fillOnStart = true;

    private readonly List<GameObject> alive = new List<GameObject>();
    private Transform nursery;
    private float refillAtTime;
    private bool waitingToRefill;
    private int groupsSpawned;

    /// <summary>How many of this room's enemies are still alive.</summary>
    public int AliveCount => alive.Count;

    /// <summary>How many groups this room has spawned so far.</summary>
    public int GroupsSpawned => groupsSpawned;

    /// <summary>Room label, for logs.</summary>
    public string RoomName => roomName;

    private void Awake()
    {
        // Enemies are assembled in here while inactive so their Awake does not run
        // until they are fully configured.
        GameObject holder = new GameObject("~nursery");
        holder.transform.SetParent(transform, false);
        holder.SetActive(false);
        nursery = holder.transform;
    }

    private void Start()
    {
        if (fillOnStart)
        {
            SpawnGroup();
        }
    }

    private void Update()
    {
        // Drop anything that has been destroyed.
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
            {
                alive.RemoveAt(i);
            }
        }

        if (alive.Count > 0)
        {
            waitingToRefill = false;
            return;
        }

        if (!waitingToRefill)
        {
            waitingToRefill = true;
            refillAtTime = Time.time + respawnDelay;
            return;
        }

        if (Time.time < refillAtTime)
        {
            return;
        }

        // Do not let a group appear while the player is watching.
        if (PlayerState.Instance != null)
        {
            if (PlayerState.Instance.IsDead)
            {
                return;
            }

            float sqr = (PlayerState.Instance.transform.position - transform.position).sqrMagnitude;
            if (sqr < safeSpawnDistance * safeSpawnDistance)
            {
                return;
            }
        }

        waitingToRefill = false;
        SpawnGroup();
    }

    private float RunMinutes()
    {
        return RunTimerController.Instance != null
            ? RunTimerController.Instance.ElapsedTime / 60f
            : 0f;
    }

    private int CurrentGroupSize()
    {
        int size = baseGroupSize + Mathf.FloorToInt(RunMinutes() * extraPerMinute);
        return Mathf.Clamp(size, 1, Mathf.Max(1, maxGroupSize));
    }

    private float CurrentHealthMultiplier()
    {
        return Mathf.Clamp(1f + RunMinutes() * healthGrowthPerMinute, 1f, maxHealthMultiplier);
    }

    private EnemyVariant PickVariant()
    {
        float minutes = RunMinutes();

        float total = 0f;
        foreach (EnemyVariant v in roster)
        {
            if (v != null && minutes >= v.unlockAtMinutes)
            {
                total += Mathf.Max(0f, v.weight);
            }
        }

        // Nothing unlocked yet: fall back to the earliest-available variant.
        if (total <= 0f)
        {
            EnemyVariant fallback = null;
            foreach (EnemyVariant v in roster)
            {
                if (v == null)
                {
                    continue;
                }

                if (fallback == null || v.unlockAtMinutes < fallback.unlockAtMinutes)
                {
                    fallback = v;
                }
            }

            return fallback;
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

    /// <summary>Spawns a fresh group immediately, ignoring the respawn delay.</summary>
    public void SpawnGroup()
    {
        if (enemyPrefab == null || spawnPoints.Count == 0 || roster.Count == 0)
        {
            return;
        }

        int size = CurrentGroupSize();
        float healthMul = CurrentHealthMultiplier();

        // Shuffle the spawn points so repeat groups do not stand in identical spots.
        List<Transform> order = new List<Transform>(spawnPoints);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform tmp = order[i];
            order[i] = order[j];
            order[j] = tmp;
        }

        for (int i = 0; i < size; i++)
        {
            Transform point = order[i % order.Count];
            EnemyVariant variant = PickVariant();
            if (variant == null)
            {
                continue;
            }

            GameObject enemy = SpawnOne(variant, point, healthMul, i >= order.Count);
            if (enemy != null)
            {
                alive.Add(enemy);
            }
        }

        groupsSpawned++;
    }

    private GameObject SpawnOne(EnemyVariant variant, Transform point, float healthMul, bool scatter)
    {
        Vector3 position = point.position;

        // If we ran out of distinct points, nudge extras off the spot.
        if (scatter)
        {
            Vector2 offset = Random.insideUnitCircle * 2.2f;
            position += new Vector3(offset.x, 0f, offset.y);
        }

        // Always land on the NavMesh, never inside geometry.
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(position, out hit, 6f, NavMesh.AllAreas))
        {
            return null;
        }

        // Built inactive, so Awake waits until Configure has run.
        GameObject enemy = Instantiate(enemyPrefab, nursery);
        enemy.name = variant.displayName + "_" + roomName;

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
            Debug.LogWarning("RoomSpawner (" + roomName + "): no mesh child named '"
                + variant.meshChildName + "' on the enemy prefab.", this);
        }

        EnemyAIController ai = enemy.GetComponent<EnemyAIController>();
        if (ai != null)
        {
            ai.Configure(variant.maxHealth * healthMul, variant.damage, variant.xpReward);
        }

        enemy.transform.localScale = Vector3.one * variant.modelScale;

        // Move out of the nursery and switch on.
        enemy.transform.SetParent(null, false);
        enemy.transform.position = hit.position;
        enemy.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        enemy.SetActive(true);

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(hit.position);
        }

        return enemy;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (Transform p in spawnPoints)
        {
            if (p != null)
            {
                Gizmos.DrawWireSphere(p.position, 0.5f);
            }
        }

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, safeSpawnDistance);
    }
}
