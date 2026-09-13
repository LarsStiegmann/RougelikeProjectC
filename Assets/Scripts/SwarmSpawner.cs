using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class SwarmSpawner : MonoBehaviour
{
    [Header("Enemy source")]
    [Tooltip("The configured enemy prefab. Every mesh child should be disabled on it.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Used when the player is not inside any RoomZone.")]
    [SerializeField] private List<EnemyVariant> fallbackRoster = new List<EnemyVariant>();

    [Header("Population")]
    [Tooltip("How many enemies to keep alive around the player at minute 0. See the curve below for growth.")]
    [SerializeField] private int targetAlive = 12;

    [Header("Difficulty over time")]
    [Tooltip("Multiplier on targetAlive by run minute. Lets the opening breathe and the late game overwhelm.")]
    [SerializeField] private AnimationCurve densityByMinute = new AnimationCurve(
        new Keyframe(0f, 0.4f), new Keyframe(3f, 1f), new Keyframe(8f, 1.6f), new Keyframe(15f, 2.4f), new Keyframe(25f, 3.2f));

    [Tooltip("Absolute ceiling on alive enemies regardless of the curve. 160 measured at ~9ms CPU/frame; keep headroom.")]
    [SerializeField] private int maxAliveHardCap = 140;

    [Tooltip("Enemy max health is multiplied by 1 + this per minute.")]
    [SerializeField] private float healthGrowthPerMinute = 0.10f;

    [Tooltip("Enemy damage is multiplied by 1 + this per minute.")]
    [SerializeField] private float damageGrowthPerMinute = 0.045f;

    [Tooltip("XP reward is multiplied by 1 + this per minute, so levelling keeps pace with tougher enemies.")]
    [SerializeField] private float xpGrowthPerMinute = 0.04f;

    [Tooltip("Enemy move speed is multiplied by 1 + this per minute. The player runs at 6; once the horde passes that, kiting stops being free and the run ends.")]
    [SerializeField] private float speedGrowthPerMinute = 0.025f;

    [Tooltip("Hard cap on the speed multiplier so enemies never become absurd.")]
    [SerializeField] private float maxSpeedMultiplier = 1.5f;

    public float CurrentSpeedMultiplier => Mathf.Min(maxSpeedMultiplier, 1f + speedGrowthPerMinute * RunMinutes);

    [Tooltip("Extra enemies per batch once the run passes this many minutes.")]
    [SerializeField] private float bigBatchAfterMinutes = 7f;

    private static float RunMinutes
    {
        get
        {
            return RunTimerController.Instance != null
                ? RunTimerController.Instance.ElapsedTime / 60f
                : Time.timeSinceLevelLoad / 60f;
        }
    }

    public int CurrentTargetAlive
    {
        get
        {
            float mult = densityByMinute != null && densityByMinute.length > 0
                ? Mathf.Max(0.1f, densityByMinute.Evaluate(RunMinutes))
                : 1f;
            return Mathf.Clamp(Mathf.RoundToInt(targetAlive * mult), 1, Mathf.Max(1, maxAliveHardCap));
        }
    }

    public float CurrentHealthMultiplier => 1f + healthGrowthPerMinute * RunMinutes;

    public float CurrentDamageMultiplier => 1f + damageGrowthPerMinute * RunMinutes;

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


    public int AliveCount => alive.Count;


    public string CurrentRoom { get; private set; } = "-";

    private void Awake()
    {

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

        int target = CurrentTargetAlive;
        if (Time.time < nextSpawnTime || alive.Count >= target)
        {
            return;
        }

        nextSpawnTime = Time.time + spawnInterval;

        int batch = RunMinutes >= bigBatchAfterMinutes ? maxSpawnsPerBatch * 2 : maxSpawnsPerBatch;
        int wanted = Mathf.Min(batch, target - alive.Count);
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
            float minutes = RunMinutes;
            ai.Configure(
                variant.maxHealth * CurrentHealthMultiplier,
                variant.damage * CurrentDamageMultiplier,
                variant.xpReward * (1f + xpGrowthPerMinute * minutes));
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

            NavMeshAgent authored = enemyPrefab.GetComponent<NavMeshAgent>();
            float baseSpeed = authored != null ? authored.speed : agent.speed;
            agent.speed = baseSpeed * CurrentSpeedMultiplier;
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
