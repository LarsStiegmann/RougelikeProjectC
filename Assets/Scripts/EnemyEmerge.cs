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

    [Tooltip("Solid clods of earth thrown up as the enemy breaks the surface.")]
    [SerializeField] private int chunkCount = 14;

    [Tooltip("Base size of a dirt clod in metres.")]
    [SerializeField] private float chunkSize = 0.24f;

    [Tooltip("Low-poly clod meshes (modelled in Blender). Random per particle. Falls back to cubes when empty.")]
    [SerializeField] private Mesh[] clodMeshes;

    [Tooltip("Material for the clods — the arena terrain palette, so they match the soil.")]
    [SerializeField] private Material clodMaterial;

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
        // --- dust plume ---
        GameObject go = new GameObject("EmergeDust");
        go.transform.position = groundPosition;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
        main.startColor = dustColor;
        main.gravityModifier = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 96;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0f, (short)dustParticles),
            new ParticleSystem.Burst(0.25f, (short)(dustParticles / 2)),
            new ParticleSystem.Burst(0.55f, (short)(dustParticles / 3))
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 32f;
        shape.radius = dustRadius;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // cone pointing up

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.4f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(dustColor, 0f), new GradientColorKey(dustColor, 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetDustMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();

        // --- dirt clods: solid chunks that erupt and rain back down ---
        GameObject ck = new GameObject("EmergeChunks");
        ck.transform.position = groundPosition;

        ParticleSystem cps = ck.AddComponent<ParticleSystem>();
        cps.Stop();

        var cmain = cps.main;
        cmain.duration = 0.5f;
        cmain.loop = false;
        cmain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        cmain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        cmain.startSize = new ParticleSystem.MinMaxCurve(chunkSize * 0.5f, chunkSize);
        Color dark = dustColor * 0.75f; dark.a = 1f;
        cmain.startColor = new ParticleSystem.MinMaxGradient(dark, dustColor);
        cmain.gravityModifier = 1.6f;
        cmain.simulationSpace = ParticleSystemSimulationSpace.World;
        cmain.maxParticles = 48;
        cmain.startRotation3D = true;
        cmain.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        cmain.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        cmain.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var cemission = cps.emission;
        cemission.rateOverTime = 0f;
        cemission.SetBursts(new[] {
            new ParticleSystem.Burst(0f, (short)chunkCount),
            new ParticleSystem.Burst(0.22f, (short)(chunkCount / 3)),
            new ParticleSystem.Burst(0.45f, (short)(chunkCount / 4))
        });

        var cshape = cps.shape;
        cshape.enabled = true;
        cshape.shapeType = ParticleSystemShapeType.Cone;
        cshape.angle = 26f;
        cshape.radius = dustRadius * 0.6f;
        cshape.rotation = new Vector3(-90f, 0f, 0f);

        var crot = cps.rotationOverLifetime;
        crot.enabled = true;
        crot.separateAxes = false;
        crot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

        var crenderer = ck.GetComponent<ParticleSystemRenderer>();
        crenderer.renderMode = ParticleSystemRenderMode.Mesh;
        if (clodMeshes != null && clodMeshes.Length > 0)
        {
            crenderer.SetMeshes(clodMeshes);
            crenderer.material = clodMaterial != null ? clodMaterial : GetDustMaterial();
        }
        else
        {
            crenderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            crenderer.material = GetDustMaterial();
        }
        crenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        crenderer.receiveShadows = false;

        cps.Play();

        Destroy(go, 2f);
        Destroy(ck, 2.5f);
    }

    private static Material GetDustMaterial()
    {
        if (dustMaterial != null)
        {
            return dustMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        dustMaterial = new Material(shader);
        dustMaterial.name = "EmergeDust";

        // Soft round sprite so puffs read as dust, not squares.
        dustMaterial.SetTexture("_BaseMap", GetSoftTexture());
        dustMaterial.mainTexture = GetSoftTexture();

        return dustMaterial;
    }

    private static Texture2D softTexture;

    private static Texture2D GetSoftTexture()
    {
        if (softTexture != null)
        {
            return softTexture;
        }

        const int S = 64;
        softTexture = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f;
                float dy = (y + 0.5f) / S - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                softTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        softTexture.Apply();
        return softTexture;
    }
}
