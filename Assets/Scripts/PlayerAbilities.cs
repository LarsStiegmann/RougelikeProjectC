using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerAbilities : MonoBehaviour
{
    public static PlayerAbilities Instance { get; private set; }


    public const int MaxAbilities = 2;

    [Tooltip("Same layer mask the auto-attack uses to find enemies.")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Height above the player's feet used as the centre of ability hit checks.")]
    [SerializeField] private float castHeight = 1f;

    [Tooltip("Seconds before the first cast after picking an ability up.")]
    [SerializeField] private float initialDelay = 0.6f;

    [Header("Homing Bolts")]
    [Tooltip("Travel speed of a bolt in metres per second.")]
    [SerializeField] private float boltSpeed = 16f;

    [Tooltip("How far the ability looks for targets, as a multiple of its radius.")]
    [SerializeField] private float boltSearchMultiplier = 2.5f;

    [SerializeField] private Achievement frostNovaAchievement;
    [SerializeField] private Achievement orbitingOrbsAchievement;
    [SerializeField] private Achievement homingBoltsAchievement;

    [SerializeField] private AudioClip[] freezeSounds;
    [SerializeField] private AudioClip[] boltCastSounds;
    [SerializeField] private AudioClip[] boltHitSounds;
    [SerializeField] private AudioClip[] orbHitSounds;

    public class OwnedAbility
    {
        public AbilityDefinition definition;
        public int level = 1;
        public float timer;

        public OrbitingOrbs rig;

        public int rigLevel = -1;

        public float Cooldown => definition.CooldownAt(level);
        public float Charge => definition.CooldownAt(level) <= 0f
            ? 1f
            : Mathf.Clamp01(timer / definition.CooldownAt(level));
    }

    private readonly List<OwnedAbility> owned = new List<OwnedAbility>();

    public IReadOnlyList<OwnedAbility> Owned => owned;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Grant(AbilityDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        foreach (OwnedAbility a in owned)
        {
            if (a.definition == definition)
            {
                LevelUp(a);
                return;
            }
        }

        if (owned.Count >= MaxAbilities)
        {
            OwnedAbility lowest = null;
            foreach (OwnedAbility a in owned)
            {
                if (a.definition == null)
                {
                    continue;
                }

                if (lowest == null || a.level < lowest.level)
                {
                    lowest = a;
                }
            }

            if (lowest != null)
            {
                LevelUp(lowest);
            }

            return;
        }

        OwnedAbility added = new OwnedAbility
        {
            definition = definition,
            level = 1,
            timer = Mathf.Max(0f, definition.CooldownAt(1) - initialDelay)
        };

        owned.Add(added);
        ReportAchievement(added);
    }

    private void LevelUp(OwnedAbility a)
    {
        a.level = Mathf.Min(a.level + 1, Mathf.Max(1, a.definition.maxLevel));
        ReportAchievement(a);
    }

    private void ReportAchievement(OwnedAbility a)
    {
        if (a == null || a.definition == null || AchievementController.Instance == null)
        {
            return;
        }

        switch (a.definition.kind)
        {
            case AbilityKind.FrostNova:
                AchievementController.Instance.UpdateAchievement(frostNovaAchievement, a.level);
                break;
            case AbilityKind.OrbitingOrbs:
                AchievementController.Instance.UpdateAchievement(orbitingOrbsAchievement, a.level);
                break;
            case AbilityKind.HomingBolts:
                AchievementController.Instance.UpdateAchievement(homingBoltsAchievement, a.level);
                break;
        }
    }

    public bool IsFull => owned.Count >= MaxAbilities;

    public int LevelOf(AbilityDefinition definition)
    {
        foreach (OwnedAbility a in owned)
        {
            if (a.definition == definition)
            {
                return a.level;
            }
        }

        return 0;
    }

    private void Update()
    {
        if (PlayerState.Instance == null || PlayerState.Instance.IsDead)
        {
            return;
        }

        for (int i = 0; i < owned.Count; i++)
        {
            OwnedAbility a = owned[i];
            if (a.definition == null)
            {
                continue;
            }

            if (a.definition.kind == AbilityKind.OrbitingOrbs)
            {
                EnsureOrbiters(a);
                continue;
            }

            a.timer += Time.deltaTime;

            if (a.timer >= a.Cooldown)
            {
                a.timer = 0f;
                Execute(a);
            }
        }
    }

    private void Execute(OwnedAbility a)
    {
        switch (a.definition.kind)
        {
            case AbilityKind.FrostNova:
                CastFrostNova(a);
                break;

            case AbilityKind.HomingBolts:
                FireHomingBolts(a);
                break;

            default:
                CastFrostNova(a);
                break;
        }
    }

    private void EnsureOrbiters(OwnedAbility a)
    {
        if (a.rig == null)
        {
            GameObject go = new GameObject("ArcaneOrbs");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            a.rig = go.AddComponent<OrbitingOrbs>();
            a.rigLevel = -1;
        }

        if (a.rigLevel != a.level)
        {
            a.rig.Configure(a.definition, a.level, enemyLayer, orbHitSounds);
            a.rigLevel = a.level;
        }

        a.timer = a.Cooldown;
    }

    private void FireHomingBolts(OwnedAbility a)
    {
        AbilityDefinition def = a.definition;
        float searchRadius = def.RadiusAt(a.level) * Mathf.Max(1f, boltSearchMultiplier);
        float damage = PlayerState.Instance.currentDamage * def.DamageMultiplierAt(a.level);

        Vector3 origin = transform.position + Vector3.up * castHeight;

        Collider[] candidates = Physics.OverlapSphere(
            origin,
            searchRadius,
            enemyLayer,
            QueryTriggerInteraction.Collide);

        List<Transform> targets = new List<Transform>();
        HashSet<EnemyAIController> seen = new HashSet<EnemyAIController>();

        for (int i = 0; i < candidates.Length; i++)
        {
            EnemyAIController enemy = candidates[i].GetComponentInParent<EnemyAIController>();
            if (enemy == null || !seen.Add(enemy))
            {
                continue;
            }

            targets.Add(enemy.transform);
        }

        if (targets.Count == 0)
        {
            a.timer = Mathf.Max(0f, a.Cooldown - 0.35f);
            return;
        }

        targets.Sort((x, y) =>
            (x.position - origin).sqrMagnitude.CompareTo((y.position - origin).sqrMagnitude));

        int count = Mathf.Max(1, def.CountAt(a.level));

        for (int i = 0; i < count; i++)
        {
            Transform target = targets[i % targets.Count];
            SpawnBolt(origin, target, damage, def.effectColour, i, count);
        }
    }

    private void SpawnBolt(Vector3 origin, Transform target, float damage, Color colour, int index, int total)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "AbilityBolt";
        go.transform.localScale = new Vector3(0.28f, 0.28f, 0.55f);

        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            Destroy(c);
        }

        float spread = total <= 1 ? 0f : (index / (float)(total - 1) - 0.5f) * 1.6f;
        go.transform.position = origin + transform.right * spread + Vector3.up * 0.2f;
        go.transform.forward = (target.position + Vector3.up * 0.8f - go.transform.position).normalized;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Material m = new Material(Shader.Find("Sprites/Default"));
            m.color = colour;
            r.material = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        AbilityBolt bolt = go.AddComponent<AbilityBolt>();
        bolt.Launch(target, damage, boltSpeed, enemyLayer, colour, boltCastSounds, boltHitSounds);
    }

    private void CastFrostNova(OwnedAbility a)
    {
        AbilityDefinition def = a.definition;
        float radius = def.RadiusAt(a.level);
        float damage = PlayerState.Instance.currentDamage * def.DamageMultiplierAt(a.level);

        Vector3 centre = transform.position + Vector3.up * castHeight;

        SpawnBurst(centre, radius, def.effectColour);

        AudioController.Instance.PlayRandomAudio(freezeSounds, transform, 0.5f, true);

        Collider[] candidates = Physics.OverlapSphere(
            centre,
            radius,
            enemyLayer,
            QueryTriggerInteraction.Collide);

        HashSet<EnemyAIController> alreadyHit = new HashSet<EnemyAIController>();

        for (int i = 0; i < candidates.Length; i++)
        {
            EnemyAIController enemy = candidates[i].GetComponentInParent<EnemyAIController>();
            if (enemy == null || !alreadyHit.Add(enemy))
            {
                continue;
            }

            enemy.EnemyTakeDamage(damage);
        }
    }

    private void SpawnBurst(Vector3 centre, float radius, Color colour)
    {
        GameObject go = new GameObject("AbilityBurst");
        go.transform.position = centre;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 2.2f, radius * 3.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startColor = colour;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)90) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;
        shape.radiusThickness = 0f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.05f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(colour, 0f), new GradientColorKey(colour, 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.7f, 0.45f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetBurstMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();

        GameObject flashGo = new GameObject("AbilityFlash");
        flashGo.transform.SetParent(go.transform, false);
        Light flash = flashGo.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = colour;
        flash.range = radius * 2f;
        flash.intensity = 4f;

        Destroy(go, 1.2f);
    }

    private static Material burstMaterial;
    private static Texture2D burstTexture;

    private static Material GetBurstMaterial()
    {
        if (burstMaterial != null)
        {
            return burstMaterial;
        }

        burstMaterial = new Material(Shader.Find("Sprites/Default"));
        burstMaterial.name = "AbilityBurst";
        burstMaterial.mainTexture = GetBurstTexture();
        return burstMaterial;
    }

    private static Texture2D GetBurstTexture()
    {
        if (burstTexture != null)
        {
            return burstTexture;
        }

        const int S = 64;
        burstTexture = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f;
                float dy = (y + 0.5f) / S - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float alpha = Mathf.Clamp01(1f - d);
                alpha = alpha * alpha * (3f - 2f * alpha);
                burstTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        burstTexture.Apply();
        return burstTexture;
    }
}