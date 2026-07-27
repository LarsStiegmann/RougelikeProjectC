using UnityEngine;
using TMPro;

public class RunTimerController : MonoBehaviour
{
    public static RunTimerController Instance { get; private set; }

    
    [Header("Difficulty Scaling")]
    [Tooltip("Time in seconds it takes for enemy damage to ramp up to the max multiplier.")]
    [SerializeField] private float damageRampDuration = 300f;
    [Tooltip("Enemy damage multiplier once the ramp duration has fully elapsed.")]
    [SerializeField] private float maxDamageMultiplier = 2.5f;
[SerializeField] private TMP_Text timerText;

    public float ElapsedTime { get; private set; }
    public bool IsRunning { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateTimerDisplay();
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        ElapsedTime += Time.deltaTime;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(ElapsedTime / 60f);
        int seconds = Mathf.FloorToInt(ElapsedTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    public void ResumeTimer()
    {
        IsRunning = true;
    }

    public void ResetTimer()
    {
        ElapsedTime = 0f;
        UpdateTimerDisplay();
    }

/// <summary>
    /// Smoothly scales from 1x at the start of the run up to maxDamageMultiplier once
    /// damageRampDuration seconds have elapsed, then holds steady so late runs stay
    /// survivable instead of spiraling into one-shots.
    /// </summary>
    public float EnemyDamageMultiplier
    {
        get
        {
            float t = damageRampDuration > 0f ? Mathf.Clamp01(ElapsedTime / damageRampDuration) : 1f;
            return Mathf.Lerp(1f, maxDamageMultiplier, t);
        }
    }

}
