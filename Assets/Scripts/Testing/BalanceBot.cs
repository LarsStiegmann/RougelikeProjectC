using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dev-only autopilot used to measure pacing for balance work. Attached to the
/// Player at runtime by tooling; never ships on a prefab.
///
/// It kites away from the nearest enemies, walks to affordable chests, picks
/// level-up upgrades with a sensible bias, and logs a snapshot every interval so
/// the run can be plotted as: level, HP, kills, coins, enemies alive, abilities.
/// </summary>
public class BalanceBot : MonoBehaviour
{
    public static BalanceBot Instance;
    public static readonly StringBuilder Log = new StringBuilder();

    [SerializeField] public float simSpeed = 3f;
    [SerializeField] public float logInterval = 30f;
    [SerializeField] public float threatRadius = 11f;
    [SerializeField] public float arenaRadius = 68f;
    [SerializeField] public float chestSeekRadius = 45f;

    private CharacterMovement movement;
    private FieldInfo moveInputField;
    private float nextLog;
    private float startTime;
    private int lastKills;
    private float lastLogTime;
    private bool finished;
    private float orbitSign = 1f;
    private float nextOrbitFlip;

    private static readonly string[] Preference =
    {
        "Damage", "AttackSpeed", "MaxHealth", "HealthRegeneration", "Armor",
        "LifeSteal", "Range", "CritChance", "CritDamage", "Speed", "JumpForce"
    };

#if UNITY_EDITOR
    /// <summary>
    /// Attaches the bot the instant the scene loads when the editor pref is set,
    /// so the first 20 seconds of a run are measured rather than lost to tooling
    /// latency.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        if (!UnityEditor.EditorPrefs.GetBool("BalanceBot.AutoAttach", false))
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player != null && player.GetComponent<BalanceBot>() == null)
        {
            BalanceBot b = player.AddComponent<BalanceBot>();
            b.simSpeed = UnityEditor.EditorPrefs.GetFloat("BalanceBot.SimSpeed", 3f);
        }
    }
#endif

    private void Awake()
    {
        Instance = this;
        movement = GetComponent<CharacterMovement>();
        moveInputField = typeof(CharacterMovement).GetField("moveInput", BindingFlags.NonPublic | BindingFlags.Instance);
        startTime = Time.time;
        nextLog = Time.time;
        lastLogTime = Time.time;
        Log.Length = 0;
        Log.AppendLine("t(s)  lvl   hp/max    dmg  atk%  kills  k/min  coins alive chests   ms  dmgIn/min  abilities");
    }

    private void Update()
    {
        if (finished || PlayerState.Instance == null)
        {
            return;
        }

        if (PlayerState.Instance.IsDead)
        {
            Snapshot();
            Log.AppendLine("DEAD at " + (Time.time - startTime).ToString("F0") + "s");
            finished = true;
            Time.timeScale = 1f;
            return;
        }

        frameMsAccum += Time.unscaledDeltaTime * 1000f; frameCount++;
        float hpNow = PlayerState.Instance.currentHealth;
        if (lastHp >= 0f && hpNow < lastHp) dmgTakenAccum += lastHp - hpNow;
        lastHp = hpNow;
        HandleLevelUp();

        bool paused = Time.timeScale == 0f;
        if (!paused)
        {
            Time.timeScale = simSpeed;
            Drive();
            TryChests();
        }

        if (Time.time >= nextLog)
        {
            Snapshot();
            nextLog = Time.time + logInterval;
        }
    }

    private void Snapshot()
    {
        PlayerState p = PlayerState.Instance;
        int kills = StatCounter.Instance != null ? StatCounter.Instance.kills : 0;
        float dt = Mathf.Max(0.01f, Time.time - lastLogTime);
        float kpm = (kills - lastKills) / dt * 60f;
        lastKills = kills; lastLogTime = Time.time;
        int alive = FindObjectsOfType<EnemyAIController>().Length;
        int coins = CurrencyController.Instance != null ? CurrencyController.Instance.Coins : 0;

        string ab = "";
        if (PlayerAbilities.Instance != null)
        {
            foreach (var a in PlayerAbilities.Instance.Owned)
            {
                ab += a.definition.displayName.Substring(0, 5) + "L" + a.level + " ";
            }
        }

        Log.AppendLine(
            (Time.time - startTime).ToString("F0").PadLeft(4) + "  " +
            p.currentLevel.ToString().PadLeft(3) + "  " +
            (p.currentHealth.ToString("F0") + "/" + p.currentMaxHealth.ToString("F0")).PadLeft(8) + "  " +
            p.currentDamage.ToString("F0").PadLeft(4) + "  " +
            (p.currentAttackSpeed * 100f - 100f).ToString("F0").PadLeft(4) + "  " +
            kills.ToString().PadLeft(5) + "  " +
            kpm.ToString("F0").PadLeft(5) + "  " +
            coins.ToString().PadLeft(5) + "  " +
            alive.ToString().PadLeft(4) + "  " +
            (StatCounter.Instance != null ? StatCounter.Instance.chests : 0).ToString().PadLeft(5) + "  " +
            (frameCount > 0 ? frameMsAccum / frameCount : 0f).ToString("F1").PadLeft(5) + "  " +
            (dmgTakenAccum / dt * 60f).ToString("F0").PadLeft(9) + "  " + ab);
        frameMsAccum = 0f; frameCount = 0; dmgTakenAccum = 0f;
    }

    private void HandleLevelUp()
    {
        GameObject panel = GameObject.Find("LevelUpPanel");
        if (panel == null || !panel.activeInHierarchy)
        {
            return;
        }

        UpgradeButton[] buttons = panel.GetComponentsInChildren<UpgradeButton>(false);
        if (buttons.Length == 0)
        {
            return;
        }

        UpgradeButton best = buttons[0];
        int bestRank = int.MaxValue;
        foreach (UpgradeButton b in buttons)
        {
            var f = typeof(UpgradeButton).GetField("upgrade", BindingFlags.NonPublic | BindingFlags.Instance);
            StatUpgrade u = f != null ? f.GetValue(b) as StatUpgrade : null;
            if (u == null) continue;
            int rank = System.Array.IndexOf(Preference, u.upgradeType.ToString());
            if (rank < 0) rank = 99;
            // Rarity breaks ties: an Epic damage beats a Common damage.
            rank = rank * 10 - (int)u.rarity;
            if (rank < bestRank) { bestRank = rank; best = b; }
        }

        Button btn = best.GetComponent<Button>();
        if (btn != null) btn.onClick.Invoke();
    }

    private bool fleeing;
    private float dmgTakenAccum; private float lastHp = -1f;
    private float frameMsAccum; private int frameCount;

    private void Drive()
    {
        if (movement == null || moveInputField == null) return;

        PlayerState p = PlayerState.Instance;
        float hpFrac = p.currentMaxHealth > 0f ? p.currentHealth / p.currentMaxHealth : 1f;
        if (hpFrac < 0.35f) fleeing = true;
        if (hpFrac > 0.70f) fleeing = false;

        // When healthy, only step back from enemies about to swing (their reach is
        // ~2m); when hurt, back off properly and let regen work. This is how a
        // competent player actually plays: circle through the pack, retreat to heal.
        float radius = fleeing ? 12f : 3.4f;

        Vector3 me = transform.position;
        Vector3 flee = Vector3.zero;
        Vector3 nearest = Vector3.zero; float nearestD = 9999f; int near = 0;
        foreach (EnemyAIController e in FindObjectsOfType<EnemyAIController>())
        {
            Vector3 d = me - e.transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist < nearestD) { nearestD = dist; nearest = e.transform.position; }
            if (dist < 15f) near++;
            if (dist < radius && dist > 0.05f)
            {
                flee += d.normalized * (radius - dist) / radius;
            }
        }

        if (Time.time > nextOrbitFlip) { orbitSign = -orbitSign; nextOrbitFlip = Time.time + Random.Range(3f, 7f); }

        Vector3 desired = Vector3.zero;
        Transform chest = FindChest();

        if (fleeing && flee.sqrMagnitude > 0.001f)
        {
            Vector3 side = Vector3.Cross(Vector3.up, flee.normalized) * orbitSign;
            desired = (flee.normalized * 0.8f + side * 0.5f).normalized;
        }
        else if (chest != null && hpFrac > 0.5f && (near == 0 || Vector3.Distance(me, chest.position) < 24f))
        {
            desired = chest.position - me; desired.y = 0f; desired.Normalize();
        }
        else if (nearestD < 9000f && nearestD > 4.5f)
        {
            // Engage: close to auto-attack range.
            desired = nearest - me; desired.y = 0f; desired.Normalize();
        }
        else if (flee.sqrMagnitude > 0.001f)
        {
            // In the thick of it: strafe around the pack, edging back a little.
            Vector3 side = Vector3.Cross(Vector3.up, flee.normalized) * orbitSign;
            desired = (side * 0.85f + flee.normalized * 0.35f).normalized;
        }
        else if (nearestD < 9000f)
        {
            Vector3 toE = nearest - me; toE.y = 0f;
            desired = Vector3.Cross(Vector3.up, toE.normalized) * orbitSign;
        }
        else
        {
            desired = -me; desired.y = 0f;
            desired = desired.sqrMagnitude > 25f ? desired.normalized : Vector3.zero;
        }

        Vector3 flat = me; flat.y = 0f;
        if (flat.magnitude > arenaRadius)
        {
            desired = (desired * 0.4f - flat.normalized).normalized;
        }

        Camera cam = Camera.main;
        Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = cam != null ? cam.transform.right : Vector3.right; right.y = 0f; right.Normalize();
        Vector2 input = new Vector2(Vector3.Dot(desired, right), Vector3.Dot(desired, fwd));
        moveInputField.SetValue(movement, Vector2.ClampMagnitude(input, 1f));
    }

    private Transform FindChest()
    {
        Transform best = null; float bestD = chestSeekRadius;
        foreach (TreasureChest c in FindObjectsOfType<TreasureChest>())
        {
            if (c.IsOpened) continue;
            var price = c.GetComponent<ChestPrice>();
            var cool = c.GetComponent<ChestCooldown>();
            if (price != null && !price.CanAfford()) continue;
            if (cool != null && cool.IsCoolingDown) continue;
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < bestD) { bestD = d; best = c.transform; }
        }
        return best;
    }

    private void TryChests()
    {
        foreach (TreasureChest c in FindObjectsOfType<TreasureChest>())
        {
            if (c.IsOpened) continue;
            if (Vector3.Distance(transform.position, c.transform.position) > c.InteractionRange) continue;
            var cool = c.GetComponent<ChestCooldown>();
            if (cool != null && cool.IsCoolingDown) continue;
            var price = c.GetComponent<ChestPrice>();
            if (price != null && !price.TryPay()) continue;
            c.Open();
            return;
        }
    }
}
