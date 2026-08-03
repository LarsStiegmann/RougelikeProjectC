using UnityEngine;

/// <summary>
/// Central spawner for floating damage numbers. Lives in the scene so that any
/// enemy can request a popup without each one needing its own prefab reference.
/// </summary>
public class DamagePopupSpawner : MonoBehaviour
{
    private static DamagePopupSpawner instance;

    public static DamagePopupSpawner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DamagePopupSpawner>();
            }

            return instance;
        }
    }

    [SerializeField] private DamagePopup popupPrefab;

    [Header("Placement")]
    [Tooltip("Random horizontal scatter so repeated hits do not stack into one blob.")]
    [SerializeField] private float horizontalSpread = 0.25f;

    [Tooltip("Random vertical scatter applied on spawn.")]
    [SerializeField] private float verticalSpread = 0.12f;

    [Header("Colour")]
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.4f, 1f);

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Spawns a damage number at a world position. Safe to call when no spawner or
    /// prefab is configured; it simply does nothing.
    /// </summary>
    public static void Spawn(Vector3 worldPosition, float amount)
    {
        DamagePopupSpawner spawner = Instance;
        if (spawner == null || spawner.popupPrefab == null)
        {
            return;
        }

        spawner.SpawnInternal(worldPosition, amount);
    }

    private void SpawnInternal(Vector3 worldPosition, float amount)
    {
        Vector3 jitter = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(-verticalSpread, verticalSpread),
            Random.Range(-horizontalSpread, horizontalSpread)
        );

        DamagePopup popup = Instantiate(popupPrefab, worldPosition + jitter, Quaternion.identity);
        popup.Setup(amount, normalColor);
    }
}
