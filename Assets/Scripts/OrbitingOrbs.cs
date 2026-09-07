using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent ring of arcane orbs circling the player. Always active once owned:
/// levelling adds orbs, widens the ring and speeds the spin.
///
/// Replaces the earlier crystal-blade version. Solid weapon meshes visibly rammed
/// through pillars and rocks as the ring swept past scenery; a small glowing orb
/// with a trail reads as magic passing through the world instead of a prop stuck
/// in it, and its tiny silhouette barely intersects anything.
///
/// Damage goes through EnemyAIController.EnemyTakeDamage, with a short per-enemy
/// cooldown so an orb sweeping a crowd does not delete it instantly.
/// </summary>
public class OrbitingOrbs : MonoBehaviour
{
    private AbilityDefinition definition;
    private int level = 1;
    private LayerMask enemyLayer;

    private readonly List<Transform> orbs = new List<Transform>();
    private readonly List<Renderer> halos = new List<Renderer>();
    private readonly Dictionary<EnemyAIController, float> hitCooldowns = new Dictionary<EnemyAIController, float>();

    private float spin;
    private Color colour = new Color(0.55f, 0.75f, 1f);

    private const float PerEnemyCooldown = 0.55f;
    private const float OrbHitRadius = 0.85f;
    private const float RingHeight = 1f;

    // How far the orbs rise and fall as they circle.
    private const float BobHeight = 0.22f;
    private const float BobSpeed = 2.4f;

    private static Material coreMaterial;
    private static Material glowMaterial;
    private static Mesh quadMesh;
    private static Texture2D glowTexture;
    private static readonly int ColourId = Shader.PropertyToID("_Color");

    public void Configure(AbilityDefinition def, int newLevel, LayerMask layer)
    {
        definition = def;
        level = newLevel;
        enemyLayer = layer;
        colour = def.effectColour;

        int wanted = Mathf.Max(1, def.CountAt(newLevel));

        // Rebuild only when the orb count actually changes.
        if (orbs.Count != wanted)
        {
            foreach (Transform o in orbs)
            {
                if (o != null)
                {
                    Destroy(o.gameObject);
                }
            }

            orbs.Clear();
            halos.Clear();

            for (int i = 0; i < wanted; i++)
            {
                orbs.Add(BuildOrb(i));
            }
        }
        else
        {
            RecolourOrbs();
        }
    }

    private Transform BuildOrb(int index)
    {
        GameObject root = new GameObject("ArcaneOrb_" + index);
        root.transform.SetParent(transform, false);

        // Bright solid core.
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Core";
        Collider cc = core.GetComponent<Collider>();
        if (cc != null)
        {
            Destroy(cc);
        }

        core.transform.SetParent(root.transform, false);
        core.transform.localScale = Vector3.one * 0.26f;

        Renderer cr = core.GetComponent<Renderer>();
        cr.sharedMaterial = GetCoreMaterial();
        cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        cr.receiveShadows = false;
        ApplyColour(cr, colour * 1.1f);

        // Billboarded halo so the orb reads as light rather than a plastic ball.
        GameObject halo = new GameObject("Halo");
        halo.transform.SetParent(root.transform, false);
        halo.transform.localScale = Vector3.one * 0.95f;

        MeshFilter mf = halo.AddComponent<MeshFilter>();
        mf.sharedMesh = GetQuad();

        MeshRenderer hr = halo.AddComponent<MeshRenderer>();
        hr.sharedMaterial = GetGlowMaterial();
        hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hr.receiveShadows = false;
        hr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        ApplyColour(hr, colour * 1.15f);
        halos.Add(hr);

        // Comet trail - this is what sells the whirl.
        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.32f;
        trail.startWidth = 0.17f;
        trail.endWidth = 0f;
        trail.material = GetGlowMaterial();
        trail.numCapVertices = 4;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.alignment = LineAlignment.View;

        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(colour, 0f), new GradientColorKey(colour, 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;

        // Small light so the orbs actually throw colour on the ground at night.
        GameObject lightGo = new GameObject("OrbLight");
        lightGo.transform.SetParent(root.transform, false);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = colour;
        l.range = 3.2f;
        l.intensity = 1.1f;
        l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForceVertex;

        return root.transform;
    }

    private void RecolourOrbs()
    {
        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null)
            {
                continue;
            }

            Transform core = orbs[i].Find("Core");
            if (core != null)
            {
                ApplyColour(core.GetComponent<Renderer>(), colour * 1.1f);
            }

            Transform halo = orbs[i].Find("Halo");
            if (halo != null)
            {
                ApplyColour(halo.GetComponent<Renderer>(), colour * 1.15f);
            }
        }
    }

    private static void ApplyColour(Renderer r, Color c)
    {
        if (r == null)
        {
            return;
        }

        MaterialPropertyBlock b = new MaterialPropertyBlock();
        r.GetPropertyBlock(b);
        c.a = 1f;
        b.SetColor(ColourId, c);
        r.SetPropertyBlock(b);
    }

    private void Update()
    {
        if (definition == null || orbs.Count == 0)
        {
            return;
        }

        if (PlayerState.Instance == null || PlayerState.Instance.IsDead)
        {
            return;
        }

        float radius = definition.RadiusAt(level);
        float spinSpeed = 110f + 15f * (level - 1);
        spin += spinSpeed * Time.deltaTime;

        Camera cam = Camera.main;

        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null)
            {
                continue;
            }

            float angle = spin + (360f / orbs.Count) * i;

            // Each orb bobs on its own phase so the ring undulates instead of
            // sitting on a flat disc.
            float bob = Mathf.Sin((Time.time * BobSpeed) + i * 1.7f) * BobHeight;

            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
            orbs[i].localPosition = offset + Vector3.up * (RingHeight + bob);

            // Keep the halo facing the camera.
            if (cam != null && i < halos.Count && halos[i] != null)
            {
                halos[i].transform.rotation = Quaternion.LookRotation(
                    halos[i].transform.position - cam.transform.position, Vector3.up);
            }
        }

        TickCooldowns();
        DamageTouchedEnemies();
    }

    private void TickCooldowns()
    {
        if (hitCooldowns.Count == 0)
        {
            return;
        }

        var keys = new List<EnemyAIController>(hitCooldowns.Keys);
        foreach (EnemyAIController key in keys)
        {
            if (key == null)
            {
                hitCooldowns.Remove(key);
                continue;
            }

            float remaining = hitCooldowns[key] - Time.deltaTime;
            if (remaining <= 0f)
            {
                hitCooldowns.Remove(key);
            }
            else
            {
                hitCooldowns[key] = remaining;
            }
        }
    }

    private void DamageTouchedEnemies()
    {
        float damage = PlayerState.Instance.currentDamage * definition.DamageMultiplierAt(level);

        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null)
            {
                continue;
            }

            Collider[] hits = Physics.OverlapSphere(
                orbs[i].position,
                OrbHitRadius,
                enemyLayer,
                QueryTriggerInteraction.Collide);

            for (int h = 0; h < hits.Length; h++)
            {
                EnemyAIController enemy = hits[h].GetComponentInParent<EnemyAIController>();
                if (enemy == null || hitCooldowns.ContainsKey(enemy))
                {
                    continue;
                }

                hitCooldowns[enemy] = PerEnemyCooldown;
                enemy.EnemyTakeDamage(damage);
            }
        }
    }

    private static Material GetCoreMaterial()
    {
        if (coreMaterial != null)
        {
            return coreMaterial;
        }

        // Unlit and untextured, so the core is a flat blob of colour rather than
        // a shaded ball that reads as a marble.
        coreMaterial = new Material(Shader.Find("Sprites/Default"));
        coreMaterial.name = "OrbCore";
        coreMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        coreMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        coreMaterial.enableInstancing = true;
        return coreMaterial;
    }

    private static Material GetGlowMaterial()
    {
        if (glowMaterial != null)
        {
            return glowMaterial;
        }

        glowMaterial = new Material(Shader.Find("Sprites/Default"));
        glowMaterial.name = "OrbGlow";
        glowMaterial.mainTexture = GetGlowTexture();
        glowMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        glowMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return glowMaterial;
    }

    private static Texture2D GetGlowTexture()
    {
        if (glowTexture != null)
        {
            return glowTexture;
        }

        const int S = 64;
        glowTexture = new Texture2D(S, S, TextureFormat.RGBA32, false);
        glowTexture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f;
                float dy = (y + 0.5f) / S - 0.5f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.4f);
                glowTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        glowTexture.Apply();
        return glowTexture;
    }

    private static Mesh GetQuad()
    {
        if (quadMesh != null)
        {
            return quadMesh;
        }

        quadMesh = new Mesh();
        quadMesh.name = "OrbHaloQuad";
        quadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        quadMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 1f), new Vector2(1f, 1f)
        };
        quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        quadMesh.RecalculateBounds();
        return quadMesh;
    }
}
