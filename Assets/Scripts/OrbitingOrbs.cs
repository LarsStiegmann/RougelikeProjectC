using System.Collections.Generic;
using UnityEngine;


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

    private Vector3 ringCentre;
    private bool centreReady;


    private const float FollowSharpness = 25f;

    private const float SnapDistance = 5f;

    private const float PerEnemyCooldown = 0.45f;
    private const float OrbHitRadius = 0.85f;
    private const float RingHeight = 1f;

    private const float BobHeight = 0.22f;
    private const float BobSpeed = 2.4f;

    private static Material coreMaterial;
    private static Material glowMaterial;
    private static Mesh quadMesh;
    private static Texture2D glowTexture;
    private static readonly int ColourId = Shader.PropertyToID("_Color");

    private AudioClip[] orbHitSounds;

    public void Configure(AbilityDefinition def, int newLevel, LayerMask layer, AudioClip[] orbHits)
    {
        definition = def;
        level = newLevel;
        enemyLayer = layer;
        colour = def.effectColour;

        orbHitSounds = orbHits;

        int wanted = Mathf.Max(1, def.CountAt(newLevel));

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

        Vector3 target = transform.position;

        if (!centreReady || (target - ringCentre).sqrMagnitude > SnapDistance * SnapDistance)
        {
            ringCentre = target;
            centreReady = true;
        }
        else
        {
            ringCentre = Vector3.Lerp(ringCentre, target, 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime));
        }

        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null)
            {
                continue;
            }

            float angle = spin + (360f / orbs.Count) * i;

            float bob = Mathf.Sin((Time.time * BobSpeed) + i * 1.7f) * BobHeight;

            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
            orbs[i].position = ringCentre + offset + Vector3.up * (RingHeight + bob);

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

                AudioController.Instance.PlayRandomAudio(orbHitSounds, transform, 0.4f, true);
            }
        }
    }

    private static Shader GetOrbShader()
    {
        Shader s = Shader.Find("Roguelike/OrbGlow");
        return s != null ? s : Shader.Find("Sprites/Default");
    }

    private static Material GetCoreMaterial()
    {
        if (coreMaterial != null)
        {
            return coreMaterial;
        }

  
        coreMaterial = new Material(GetOrbShader());
        coreMaterial.name = "OrbCore";
        return coreMaterial;
    }

    private static Material GetGlowMaterial()
    {
        if (glowMaterial != null)
        {
            return glowMaterial;
        }

        glowMaterial = new Material(GetOrbShader());
        glowMaterial.name = "OrbGlow";
        glowMaterial.mainTexture = GetGlowTexture();
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
