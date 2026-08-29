using UnityEngine;
using UnityEngine.Rendering;

public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; private set; }

    [SerializeField] private LevelUpController levelUpController;

    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseHealthRegeneration = 1f;
    [SerializeField] private float baseSpeed = 7f;
    [SerializeField] private float baseJumpForce = 2f;
    [SerializeField] private float baseArmor = 0f;
    [SerializeField] private float baseLifeSteal = 0f;
    [SerializeField] private float baseAttackRange = 4f;
    [SerializeField] private float baseCritChance = 0f;
    [SerializeField] private float baseCritMultiplier = 1.5f;
    [SerializeField] private float baseMaxXP = 100f;
    [SerializeField] private float baseXP = 0f;
    [SerializeField] private int baseLevel = 1;

    [SerializeField] private GameObject levelUpPanel;

    [SerializeField] private AudioClip[] damageSounds;
    [SerializeField] private AudioClip[] deathSounds;

    public float currentMaxHealth { get; private set; }
    public float currentDamage { get; private set; }
    public float currentHealthRegeneration { get; private set; }
    public float currentSpeed { get; private set; }
    public float currentJumpForce { get; private set; }
    public float currentArmor { get; private set; }
    public float currentLifeSteal { get; private set; }
    public float currentAttackRange { get; private set; }
    public float currentCritChance { get; private set; }
    public float currentCritMultiplier { get; private set; }
    public float currentHealth { get; private set; }

    // XP and Level System
    public int currentLevel { get; private set; }
    public float currentXP { get; private set; }
    public float currentMaxXP { get; private set; }

    // Events for UI updating
    public event System.Action OnHealthChanged;
    public event System.Action OnXPChanged;
    public event System.Action OnLevelUp;
    public event System.Action OnPlayerDied;

    private bool hasDied;

    public bool IsDead => hasDied;

    public float bonusAttackSpeedPercentage = 0;
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
        currentArmor = baseArmor;
        currentLifeSteal = baseLifeSteal;
        currentAttackRange = baseAttackRange;
        currentCritChance = baseCritChance;
        currentCritMultiplier = baseCritMultiplier;
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
        AudioController.Instance.PlayRandomAudio(damageSounds, transform, 1f, true);

        currentHealth -= CalculateDamage(damage);
        currentHealth = Mathf.Max(currentHealth, 0);
        OnHealthChanged?.Invoke();

        if (currentHealth == 0)
        {
            Die();
        }
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
            currentMaxXP = Mathf.Round(currentMaxXP * 1.5f);
            
            RaiseMaxHealth(1f);
            RaiseMaxXP();

            if (levelUpController != null)
            {
                levelUpController.Open();
            }

            OnLevelUp?.Invoke();
        }
        OnXPChanged?.Invoke();
    }

    public void ApplyUpgrade(StatUpgrade upgrade)
    {
        switch(upgrade.upgradeType)
        {
            case UpgradeType.MaxHealth:
                RaiseMaxHealth(upgrade.value);
                Debug.Log(currentMaxHealth);
                break;
            case UpgradeType.Damage:
                RaiseDamage(upgrade.value);
                Debug.Log(currentDamage);
                break;
            case UpgradeType.AttackSpeed:
                RaiseAttackSpeed(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.HealthRegeneration:
                RaiseHealthRegeneration(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.Speed:
                RaiseSpeed(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.JumpForce:
                RaiseJumpForce(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.Armor:
                RaiseArmor(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.LifeSteal:
                RaiseLifeSteal(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.Range:
                RaiseAttackRange(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.CritChance:
                RaiseCritChance(upgrade.value);
                Debug.Log(upgrade.value);
                break;
            case UpgradeType.CritDamage:
                RaiseCritDamage(upgrade.value);
                Debug.Log(upgrade.value);
                break;
        }
    }

    private void RegenerateHealth()
    {
        Heal(currentHealthRegeneration * Time.deltaTime);
    } 

    private void RaiseMaxXP()
    {
        currentMaxXP = currentMaxXP + (currentMaxXP / 20);
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

    public void RaiseCritChance(float amount)
    {
        currentCritChance = Mathf.Clamp(currentCritChance + amount, 0f, 1f);
    }

    public void RaiseCritDamage(float amount)
    {
        currentCritMultiplier += amount;
    }

    private void Die()
    {
        if (currentHealth <= 0)
        {
            if (hasDied)
            {
                return;
            }
            hasDied = true;

            AudioController.Instance.PlayRandomAudio(deathSounds, transform, 1f, true);

            PlayerShatterDeath shatter = GetComponent<PlayerShatterDeath>();
            if (shatter != null)
            {
                shatter.Shatter();
            }

            CharacterMovement movement = GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }

            PlayerDealDamage attack = GetComponent<PlayerDealDamage>();
            if (attack != null)
            {
                attack.enabled = false;
            }

            currentHealthRegeneration = 0;

            OnPlayerDied?.Invoke();
        }
    }

    public void ResetStats()
    {
        currentMaxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth;
        currentDamage = baseDamage;
        currentHealthRegeneration = baseHealthRegeneration;
        currentSpeed = baseSpeed;
        currentJumpForce = baseJumpForce;
        currentArmor = baseArmor;
        currentLifeSteal = baseLifeSteal;
        currentAttackRange = baseAttackRange;
        currentCritChance = baseCritChance;
        currentCritMultiplier = baseCritMultiplier;
        currentMaxXP = baseMaxXP;
        currentXP = baseXP;
        currentLevel = baseLevel;
    }
}
