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
    [Tooltip("Colour used for ordinary, non-critical hits.")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("Colour used for critical hits.")]
    [SerializeField] private Color critColor = new Color(1f, 0.95f, 0.4f, 1f);

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
        Spawn(worldPosition, amount, false);
    }

    /// <summary>
    /// Spawns a damage number coloured according to whether the hit was critical.
    /// </summary>
    public static void Spawn(Vector3 worldPosition, float amount, bool isCrit)
    {
        DamagePopupSpawner spawner = Instance;
        if (spawner == null || spawner.popupPrefab == null)
        {
            return;
        }

        spawner.SpawnInternal(worldPosition, amount, isCrit);
    }

    private void SpawnInternal(Vector3 worldPosition, float amount, bool isCrit)
    {
        Vector3 jitter = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(-verticalSpread, verticalSpread),
            Random.Range(-horizontalSpread, horizontalSpread)
        );

        DamagePopup popup = Instantiate(popupPrefab, worldPosition + jitter, Quaternion.identity);
        popup.Setup(amount, isCrit ? critColor : normalColor);
    }
}
