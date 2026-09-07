using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns Megabonk-style mini-bosses on a timer: a red telegraph flares on the
/// ground, then a giant elite erupts (EnemyEmerge handles the dig-out), wearing a
/// golden crown and a red aura light. Each successive boss is stronger.
/// Uses only the public Configure API of the enemy — no controller scripts touched.
/// </summary>
public class MiniBossSpawner : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float firstSpawnAt = 90f;
    [SerializeField] private float interval = 110f;

    [Header("Scaling per boss")]
    [SerializeField] private float statGrowth = 1.3f;
    [SerializeField] private float coinGrowth = 1.25f;

    [Header("Placement")]
    [SerializeField] private float minDistance = 16f;
    [SerializeField] private float maxDistance = 24f;

    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<EnemyVariant> bossVariants = new List<EnemyVariant>();
    [SerializeField] private GameObject crownPrefab;
    [SerializeField] private Material crownMaterial;

    [Header("Telegraph")]
    [SerializeField] private float telegraphSeconds = 1.6f;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.15f, 0.1f);

    private float nextSpawnTime;
    private int bossCount;
    private Transform nursery;

    public int BossesSpawned => bossCount;

    private void Start()
    {
        nextSpawnTime = Time.timeSinceLevelLoad + firstSpawnAt;

        // Bosses are assembled inside this inactive holder so Configure lands
        // before Awake copies maxHealth into currentHealth.
        GameObject holder = new GameObject("~bossNursery");
        holder.transform.SetParent(transform, false);
        holder.SetActive(false);
        nursery = holder.transform;
    }

    private void Update()
    {
        if (PlayerState.Instance == null || PlayerState.Instance.IsDead)
        {
            return;
        }

        if (Time.timeSinceLevelLoad >= nextSpawnTime)
        {
            nextSpawnTime = Time.timeSinceLevelLoad + interval;
            StartCoroutine(TelegraphAndSpawn());
        }
    }

    /// <summary>Force a boss right now (testing).</summary>
    public void SpawnNow()
    {
        StartCoroutine(TelegraphAndSpawn());
    }

    private IEnumerator TelegraphAndSpawn()
    {
        Vector3 point;
        if (!TryFindSpawnPoint(out point))
        {
            yield break;
        }

        // --- telegraph: pulsing red light column + dust swirl ---
        GameObject tele = new GameObject("BossTelegraph");
        tele.transform.position = point;

        Light glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(tele.transform, false);
        glow.transform.localPosition = Vector3.up * 1.2f;
        glow.type = LightType.Point;
        glow.color = telegraphColor;
        glow.range = 12f;

        float t = 0f;
        while (t < telegraphSeconds)
        {
            t += Time.deltaTime;
            glow.intensity = 2f + Mathf.PingPong(t * 9f, 4f);
            yield return null;
        }

        Destroy(tele);
        SpawnBoss(point);
    }

    private void SpawnBoss(Vector3 point)
    {
        if (enemyPrefab == null || bossVariants.Count == 0)
        {
            return;
        }

        EnemyVariant variant = bossVariants[bossCount % bossVariants.Count];
        float mult = Mathf.Pow(statGrowth, bossCount);
        float coinMult = Mathf.Pow(coinGrowth, bossCount);
        bossCount++;

        GameObject boss = Instantiate(enemyPrefab, nursery);
        boss.name = variant.displayName + "_boss";

        bool meshFound = false;
        foreach (Transform child in boss.transform)
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
            Debug.LogWarning("MiniBossSpawner: mesh child '" + variant.meshChildName + "' not found.", this);
        }

        EnemyAIController ai = boss.GetComponent<EnemyAIController>();
        if (ai != null)
        {
            ai.Configure(variant.maxHealth * mult, variant.damage * mult, variant.xpReward);
            ai.ConfigureCoins(Mathf.RoundToInt(variant.coinReward * coinMult));
        }

        boss.transform.localScale = Vector3.one * variant.modelScale;
        boss.transform.SetParent(null, false);   // leave the nursery -> Awake runs with configured stats
        boss.transform.position = point;

        Vector3 toPlayer = PlayerState.Instance != null
            ? PlayerState.Instance.transform.position - point
            : Vector3.forward;
        toPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            boss.transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }

        NavMeshAgent agent = boss.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(point);
        }

        StartCoroutine(AttachRegalia(boss, variant.modelScale));
    }

    private IEnumerator AttachRegalia(GameObject boss, float scale)
    {
        // wait for the emerge rise to finish so bounds are final
        yield return new WaitForSeconds(1.3f);
        if (boss == null)
        {
            yield break;
        }

        float top = 2.1f * scale;
        var renderers = boss.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers)
            {
                b.Encapsulate(r.bounds);
            }

            top = b.max.y - boss.transform.position.y;
        }

        if (crownPrefab != null)
        {
            GameObject crown = Instantiate(crownPrefab, boss.transform);
            crown.name = "Crown";
            crown.transform.localPosition = Vector3.up * ((top + 0.35f) / Mathf.Max(0.01f, boss.transform.localScale.y));
            crown.transform.localScale = Vector3.one * 0.5f;
            if (crownMaterial != null)
            {
                foreach (var r in crown.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = crownMaterial;
                }
            }

            crown.AddComponent<BossCrownSpin>();
        }

        Light aura = new GameObject("BossAura").AddComponent<Light>();
        aura.transform.SetParent(boss.transform, false);
        aura.transform.localPosition = Vector3.up * 1.2f;
        aura.type = LightType.Point;
        aura.color = new Color(1f, 0.2f, 0.12f);
        aura.intensity = 2.4f;
        aura.range = 9f;
    }

    private bool TryFindSpawnPoint(out Vector3 result)
    {
        Vector3 playerPos = PlayerState.Instance != null
            ? PlayerState.Instance.transform.position
            : Vector3.zero;

        for (int i = 0; i < 24; i++)
        {
            float angle = Random.Range(0f, 360f);
            float dist = Random.Range(minDistance, maxDistance);
            Vector3 candidate = playerPos + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * dist;

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(candidate, out hit, 6f, NavMesh.AllAreas))
            {
                continue;
            }

            float actual = Vector3.Distance(hit.position, playerPos);
            if (actual < minDistance * 0.7f || actual > maxDistance * 1.4f)
            {
                continue;
            }

            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }
}

/// <summary>Slow ceremonial spin for the boss crown.</summary>
public class BossCrownSpin : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(0f, 45f * Time.deltaTime, 0f, Space.Self);
    }
}
