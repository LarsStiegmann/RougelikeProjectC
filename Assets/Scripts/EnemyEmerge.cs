using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Makes an enemy climb out of the ground when it spawns instead of popping into
/// existence. The NavMeshAgent and the AI are switched off for the duration, so the
/// enemy cannot walk or attack while it is still half buried, then handed back once
/// it is standing.
///
/// This runs from Start rather than Awake on purpose: the spawner positions the enemy
/// and enables its agent immediately after activating it, and Start is the first point
/// after that where we can safely take control back.
///
/// EnemyAIController records its spawn point in Awake, which happens while the enemy is
/// still at ground level, so sinking it here does not corrupt the return-home position.
/// </summary>
[RequireComponent(typeof(EnemyAIController))]
public class EnemyEmerge : MonoBehaviour
{
    [Header("Rise")]
    [Tooltip("How far below the spawn point the enemy starts.")]
    [SerializeField] private float depth = 2.2f;

    [Tooltip("Seconds taken to climb all the way out.")]
    [SerializeField] private float duration = 0.9f;

    [Tooltip("Extra seconds to stand still after surfacing, before charging.")]
    [SerializeField] private float settleTime = 0.1f;

    [Header("Dust")]
    [Tooltip("Puff of dirt kicked up at the surface as the enemy climbs out.")]
    [SerializeField] private bool spawnDust = true;

    [SerializeField] private int dustParticles = 18;
    [SerializeField] private float dustRadius = 0.55f;
    [SerializeField] private Color dustColor = new Color(0.42f, 0.35f, 0.27f, 1f);

    private static Material dustMaterial;

    private void Start()
    {
        StartCoroutine(Rise());
    }

    private IEnumerator Rise()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        EnemyAIController ai = GetComponent<EnemyAIController>();
        Animator animator = GetComponent<Animator>();

        Vector3 ground = transform.position;
        Vector3 start = ground - Vector3.up * depth;

        // Take control: a live agent would fight the manual transform moves, and a live
        // AI would try to chase while still underground.
        if (agent != null)
        {
            agent.enabled = false;
        }

        if (ai != null)
        {
            ai.enabled = false;
        }

        if (animator != null)
        {
            animator.speed = 0f;
        }

        transform.position = start;

        if (spawnDust)
        {
            SpawnDust(ground);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease out: quick initial heave, then a slow settle as it stands up.
            float eased = 1f - (1f - t) * (1f - t);

            transform.position = Vector3.Lerp(start, ground, eased);
            yield return null;
        }

        transform.position = ground;

        if (settleTime > 0f)
        {
            yield return new WaitForSeconds(settleTime);
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }

        // Hand control back. Warp re-seats the agent on the NavMesh at the final spot.
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.Warp(ground);
            }
        }

        if (ai != null)
        {
            ai.enabled = true;
        }
    }

    private void SpawnDust(Vector3 groundPosition)
    {
        GameObject go = new GameObject("EmergeDust");
        go.transform.position = groundPosition;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.26f);
        main.startColor = dustColor;
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)dustParticles) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = dustRadius;
        shape.radiusThickness = 1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // spray upward and outward

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(dustColor, 0f), new GradientColorKey(dustColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetDustMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
        Destroy(go, 2f);
    }

    private static Material GetDustMaterial()
    {
        if (dustMaterial != null)
        {
            return dustMaterial;
        }

        // A plain unlit particle material; the tint comes from the particle colours.
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        dustMaterial = new Material(shader);
        dustMaterial.name = "EmergeDust";
        return dustMaterial;
    }
}
