using UnityEngine;

/// <summary>
/// Makes a chest refill. The moment its potion has been awarded the lid shuts again
/// and a clock appears above it counting down to when it can be looted next.
///
/// While cooling down the chest is pulled out of TreasureChest.ActiveChests rather
/// than being disabled, so ChestInteractor will not offer it, but the lid-closing
/// coroutine on the chest still runs to completion.
///
/// Each chest only refills a limited number of times; after the final use it stays
/// open for the rest of the run, which keeps total stat growth bounded.
///
/// The timer uses scaled time on purpose, so it does not tick down while the potion
/// wheel has the game paused.
/// </summary>
[RequireComponent(typeof(TreasureChest))]
public class ChestCooldown : MonoBehaviour
{
    private enum Phase
    {
        Ready,
        AwaitingReward,
        ClosingLid,
        CoolingDown,
        Exhausted
    }

    [Header("Cooldown")]
    [Tooltip("Seconds before the chest can be looted again.")]
    [SerializeField] private float cooldownSeconds = 120f;

    [Tooltip("How many times this chest can be opened in total. " +
             "After the last one it stays open permanently.")]
    [SerializeField] private int maxUses = 3;

    [Header("Timing")]
    [Tooltip("Time allowed for the lid to finish swinging shut before the clock starts. " +
             "Should be at least the chest's own Close Duration.")]
    [SerializeField] private float lidCloseWait = 0.5f;

    private TreasureChest chest;
    private ChestPotionReward reward;
    private ChestCooldownClock clock;

    private Phase phase = Phase.Ready;
    private float readyAtTime;
    private float closeTimer;
    private int usesConsumed;

    /// <summary>How many opens this chest has spent.</summary>
    public int UsesConsumed => usesConsumed;

    /// <summary>How many opens remain before the chest is spent for good.</summary>
    public int UsesRemaining => Mathf.Max(0, maxUses - usesConsumed);

    /// <summary>True while the chest is waiting to become lootable again.</summary>
    public bool IsCoolingDown => phase == Phase.CoolingDown;

    /// <summary>Seconds until the chest can be looted again.</summary>
    public float RemainingSeconds => phase == Phase.CoolingDown ? Mathf.Max(0f, readyAtTime - Time.time) : 0f;

    private void Awake()
    {
        chest = GetComponent<TreasureChest>();
        reward = GetComponent<ChestPotionReward>();

        clock = GetComponent<ChestCooldownClock>();
        if (clock == null)
        {
            clock = gameObject.AddComponent<ChestCooldownClock>();
        }
    }

    private void Update()
    {
        if (chest == null)
        {
            return;
        }

        switch (phase)
        {
            case Phase.Ready:
                TickReady();
                break;

            case Phase.AwaitingReward:
                TickAwaitingReward();
                break;

            case Phase.ClosingLid:
                TickClosingLid();
                break;

            case Phase.CoolingDown:
                TickCoolingDown();
                break;
        }
    }

    private void TickReady()
    {
        if (!chest.IsOpened)
        {
            return;
        }

        usesConsumed++;

        // Stop offering it immediately, so it cannot be re-looted while the potion
        // wheel is still up or the lid is mid-swing.
        TreasureChest.ActiveChests.Remove(chest);

        if (usesConsumed >= maxUses)
        {
            // Final use: leave it open and unlootable for the rest of the run.
            phase = Phase.Exhausted;
            return;
        }

        phase = Phase.AwaitingReward;
    }

    private void TickAwaitingReward()
    {
        // Wait for the potion wheel to finish so the lid does not slam shut behind it.
        if (PotionWheelController.Instance != null && PotionWheelController.Instance.IsSpinning)
        {
            return;
        }

        chest.Close();
        closeTimer = 0f;
        phase = Phase.ClosingLid;
    }

    private void TickClosingLid()
    {
        closeTimer += Time.deltaTime;
        if (closeTimer < lidCloseWait)
        {
            return;
        }

        readyAtTime = Time.time + cooldownSeconds;
        phase = Phase.CoolingDown;

        if (clock != null)
        {
            clock.SetProgress(1f);
            clock.Activate();
        }
    }

    private void TickCoolingDown()
    {
        float remaining = readyAtTime - Time.time;

        if (remaining > 0f)
        {
            if (clock != null && cooldownSeconds > 0f)
            {
                clock.SetProgress(remaining / cooldownSeconds);
            }

            return;
        }

        if (clock != null)
        {
            clock.SetProgress(0f);
            clock.Deactivate();
        }

        if (!TreasureChest.ActiveChests.Contains(chest))
        {
            TreasureChest.ActiveChests.Add(chest);
        }

        if (reward != null)
        {
            reward.ResetReward();
        }

        phase = Phase.Ready;
    }
}
