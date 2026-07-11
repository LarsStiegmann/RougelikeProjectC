using UnityEngine;
using UnityEngine.Rendering;

public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; private set; }

    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseHealthRegeneration = 0f;
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float baseJumpForce = 2f;
    [SerializeField] private float baseShield = 0f;
    [SerializeField] private float baseArmor = 0f;
    [SerializeField] private float baseLifeSteal = 0f;
    [SerializeField] private float baseAttackRange = 2f;
    [SerializeField] private float baseMaxXP = 100f;
    [SerializeField] private float baseXP = 0f;
    [SerializeField] private int baseLevel = 1;

    public float currentMaxHealth { get; private set; }
    public float currentDamage { get; private set; }
    public float currentHealthRegeneration { get; private set; }
    public float currentSpeed { get; private set; }
    public float currentJumpForce { get; private set; }
    public float currentShield { get; private set; }
    public float currentArmor { get; private set; }
    public float currentLifeSteal { get; private set; }
    public float currentAttackRange { get; private set; }
    public float currentHealth { get; private set; }

    // XP and Level System
    public int currentLevel { get; private set; }
    public float currentXP { get; private set; }
    public float currentMaxXP { get; private set; }

    // Events for UI updating
    public event System.Action OnHealthChanged;
    public event System.Action OnXPChanged;
    public event System.Action OnLevelUp;

    private float bonusAttackSpeedPercentage = 0;
    public float currentAttackSpeed => baseAttackSpeed * (1f + bonusAttackSpeedPercentage / 100f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentMaxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth;
        currentDamage = baseDamage;
        currentHealthRegeneration = baseHealthRegeneration;
        currentSpeed = baseSpeed;
        currentJumpForce = baseJumpForce;
        currentShield = baseShield;
        currentArmor = baseArmor;
        currentLifeSteal = baseLifeSteal;
        currentAttackRange = baseAttackRange;
        currentMaxXP = baseMaxXP;
        currentXP = baseXP;
        currentLevel = baseLevel;
    }

    private void Update()
    {
        RegenerateHealth();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= CalculateDamage(damage);
        currentHealth = Mathf.Max(currentHealth, 0);
        OnHealthChanged?.Invoke();
    }

    private float CalculateDamage(float incomingDamage)
    {
        return incomingDamage * (100f / (100f + currentArmor));
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, currentMaxHealth);
        OnHealthChanged?.Invoke();
    }

    public void AddXP(float amount)
    {
        currentXP += amount;
        if (currentXP >= currentMaxXP)
        {
            currentXP -= currentMaxXP;
            currentLevel++;
            currentMaxXP = Mathf.Round(currentMaxXP * 1.5f); // Scale level cost
            
            // Reward: increase max health (and fully heal)
            RaiseMaxHealth(1f);
            //Heal(currentMaxHealth);
            
            OnLevelUp?.Invoke();
        }
        OnXPChanged?.Invoke();
    }

    private void RegenerateHealth()
    {
        Heal(currentHealthRegeneration * Time.deltaTime);
    } 

    private void RaiseMaxXP()
    {
        currentMaxXP = currentMaxXP + (currentMaxXP / 10);
    }

    public void RaiseMaxHealth(float amount)
    {
        currentMaxHealth += amount;
    }

    public void RaiseDamage(float amount)
    {
        currentDamage += amount;
    }

    public void RaiseAttackSpeed(float amount)
    {
        bonusAttackSpeedPercentage += amount;
    }
    public void RaiseHealthRegeneration(float amount)
    {
        currentHealthRegeneration += amount;
    }

    public void RaiseSpeed(float amount)
    {
        currentSpeed += amount;
    }

    public void RaiseJumpForce(float amount)
    {
        currentJumpForce += amount;
    }

    public void RaiseShield(float amount)
    {
        currentShield += amount;
    }

    public void RaiseArmor(float amount)
    {
        currentArmor += amount;
    }

    public void RaiseLifeSteal(float amount)
    {
        currentLifeSteal += amount;
    }

    public void RaiseAttackRange(float amount)
    {
        currentAttackRange += amount;
    }

    public void ResetStats()
    {
        currentMaxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth;
        currentDamage = baseDamage;
        currentHealthRegeneration = baseHealthRegeneration;
        currentSpeed = baseSpeed;
        currentJumpForce = baseJumpForce;
        currentShield = baseShield;
        currentArmor = baseArmor;
        currentLifeSteal = baseLifeSteal;
        currentAttackRange = baseAttackRange;
        currentMaxXP = baseMaxXP;
        currentXP = baseXP;
        currentLevel = baseLevel;
    }
}
