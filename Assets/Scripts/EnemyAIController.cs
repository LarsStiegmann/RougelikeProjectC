using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float aggroRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float xpReward = 10f;
    [SerializeField] private int coinReward = 5;
    [SerializeField] private float attackCooldown = 1.5f;
    
    [Header("Health Bar")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.3f, 0f);
    [SerializeField] private Vector2 healthBarWorldSize = new Vector2(1.0f, 0.14f);
    [SerializeField] private float maxHealth = 20f;

    public float currentHealth { get; private set; }

    private NavMeshAgent agent;
    private Animator animator;
    private GameObject player;
    private float lastAttackTime;
    
    private Canvas healthBarCanvas;
    private Image healthBarFillImage;
    private float popupAnchorLocalY = 2.3f;

    [Header("Return Home")]
    [Tooltip("How close to its spawn point the enemy must get before it settles.")]
    [SerializeField] private float homeArrivalDistance = 0.4f;

    [Tooltip("Movement speed while walking home. Lower than the chase speed so they stroll back rather than sprint.")]
    [SerializeField] private float returnHomeSpeed = 1.4f;

    [Tooltip("Playback speed of the run animation while returning. There is no walk clip on this rig, so slowing the run is what sells the walk.")]
    [SerializeField] private float returnHomeAnimationSpeed = 0.5f;

    [Tooltip("How fast the enemy turns back to its original facing after arriving home, in degrees per second.")]
    [SerializeField] private float homeTurnSpeed = 220f;

    [SerializeField] private Achievement damageAchievement;

    private float defaultAgentSpeed;
    private float defaultAnimatorSpeed = 1f;
    private bool isTurningHome;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool hasSettledHome;
    private static Sprite s_whiteFillSprite;
    private Camera mainCam;
    private Coroutine aiCoroutine;

    [Header("Achievements")]
    private EnemyVariant variant;

    [SerializeField] private EnemyVariant dreadKnightBoss;
    [SerializeField] private EnemyVariant golemBoss;
    [SerializeField] private EnemyVariant chieftainBoss;
    [SerializeField] private EnemyVariant wraithLordBoss;

    [SerializeField] private Achievement dreadKnightAchievement;
    [SerializeField] private Achievement golemAchievement;
    [SerializeField] private Achievement chieftainAchievement;
    [SerializeField] private Achievement wraithLordAchievement;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        currentHealth = maxHealth;

        mainCam = Camera.main;

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        defaultAgentSpeed = agent != null ? agent.speed : 3.5f;
        if (animator != null)
        {
            defaultAnimatorSpeed = animator.speed;
        }

        CreateHealthBar();
    }

    private void OnEnable()
    {
        aiCoroutine = StartCoroutine(AIDecisionLoop());
    }

    private void OnDisable()
    {
        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
            aiCoroutine = null;
        }
    }

    private IEnumerator AIDecisionLoop()
    {
        var wait = new WaitForSeconds(0.1f);
        while (true)
        {
            UpdateAIState();
            yield return wait;
        }
    }

    private void UpdateAIState()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                StopMovement();
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance > aggroRange)
        {
            StopMovement();
        }
        else if (distance > attackRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            TriggerAttack();
        }
    }

    private void ReturnHome()
    {
        if (hasSettledHome || isTurningHome)
        {
            return;
        }

        float distanceHome = Vector3.Distance(transform.position, spawnPosition);

        if (distanceHome > homeArrivalDistance)
        {
            if (agent.isActiveAndEnabled)
            {
                agent.speed = returnHomeSpeed;
                agent.SetDestination(spawnPosition);
            }

            if (animator != null)
            {
                animator.speed = returnHomeAnimationSpeed;
                animator.SetBool("isMoving", true);
            }
            return;
        }

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            agent.speed = defaultAgentSpeed;


            agent.updateRotation = false;
        }

        if (animator != null)
        {
            animator.speed = defaultAnimatorSpeed;
            animator.SetBool("isMoving", false);
        }

        isTurningHome = true;
    }

    private void Update()
    {
        if (!isTurningHome)
        {
            return;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            spawnRotation,
            homeTurnSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, spawnRotation) < 0.5f)
        {
            transform.rotation = spawnRotation;
            isTurningHome = false;
            hasSettledHome = true;
        }
    }


    private void StopMovement()
    {
        if (agent.isActiveAndEnabled && agent.hasPath)
        {
            agent.ResetPath();
        }
        
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
        }
    }

    private void MoveTowardsPlayer()
    {
        if (agent.isActiveAndEnabled && player != null)
        {
            agent.SetDestination(player.transform.position);
        }

        if (animator != null)
        {
            animator.SetBool("isMoving", true);
        }
    }

    private void TriggerAttack()
    {
        if (agent.isActiveAndEnabled && agent.hasPath)
        {
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetBool("isMoving", false);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                
                int attackIndex = Random.Range(0, 2);
                animator.SetInteger("attackIndex", attackIndex);
                
                animator.SetTrigger("isAttacking");
            }
        }
    }

    public void PerformDamage()
    {
        if (PlayerState.Instance != null && PlayerState.Instance.IsDead)
        {
            return;
        }

        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
        }

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= attackRange + 1.0f)
            {
                PlayerState playerState = player.GetComponent<PlayerState>();
                if (playerState != null)
                {
                    float scaledDamage = damage;
                    if (RunTimerController.Instance != null)
                    {
                        scaledDamage *= RunTimerController.Instance.EnemyDamageMultiplier;
                    }

                    playerState.TakeDamage(scaledDamage);
                }
            }
        }
    }

    public void Configure(float newMaxHealth, float newDamage, float newXpReward)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        damage = Mathf.Max(0f, newDamage);
        xpReward = Mathf.Max(0f, newXpReward);
    }


    public void ConfigureCoins(int newCoinReward)
    {
        coinReward = Mathf.Max(0, newCoinReward);
    }

    public void ConfigureVariant(EnemyVariant variant)
    {
        this.variant = variant;
    }

    public void EnemyTakeDamage(float incomingDamage)
    {
        float damageTaken = incomingDamage;
        bool isCrit = false;

        if (Random.value < PlayerState.Instance.currentCritChance)
        {
            damageTaken *= PlayerState.Instance.currentCritMultiplier;
            isCrit = true;
        }

        AchievementController.Instance.UpdateAchievement(damageAchievement, damageTaken);

        currentHealth -= damageTaken;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHealthBar();

        DamagePopupSpawner.Spawn(
            transform.position + Vector3.up * popupAnchorLocalY,
            damageTaken,
            isCrit
        );

        if (currentHealth == 0)
        {
            EnemyDie();
        }
    }

    private void LateUpdate()
    {
        if (healthBarCanvas == null)
        {
            return;
        }

        if (mainCam == null)
        {
            mainCam = Camera.main;
        }

        if (mainCam != null)
        {
            healthBarCanvas.transform.rotation = mainCam.transform.rotation;
        }
    }

    private void CreateHealthBar()
    {
        float topY = 2f;
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            topY = capsule.center.y + capsule.height * 0.5f;
        }

        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(healthBarOffset.x, topY + healthBarOffset.y, healthBarOffset.z);
        canvasGO.transform.localRotation = Quaternion.identity;

        popupAnchorLocalY = topY + healthBarOffset.y + 0.45f;

        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRt = canvasGO.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(200f, 28f);
        canvasGO.transform.localScale = new Vector3(
            healthBarWorldSize.x / 200f,
            healthBarWorldSize.y / 28f,
            1f
        );

        GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bgRt.anchoredPosition = Vector2.zero;
        Image bgImg = bgGO.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGO.transform.SetParent(canvasGO.transform, false);
        RectTransform fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0.05f, 0.15f);
        fillRt.anchorMax = new Vector2(0.95f, 0.85f);
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;

        healthBarFillImage = fillGO.GetComponent<Image>();
        healthBarFillImage.sprite = GetWhiteFillSprite();
        healthBarFillImage.type = Image.Type.Filled;
        healthBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthBarFillImage.fillAmount = 1f;
        healthBarFillImage.color = new Color(0.55f, 0.05f, 0.05f, 1f);
    }

    private static Sprite GetWhiteFillSprite()
    {
        if (s_whiteFillSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            s_whiteFillSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return s_whiteFillSprite;
    }


    private void UpdateHealthBar()
    {
        if (healthBarFillImage == null)
        {
            return;
        }

        float pct = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        healthBarFillImage.fillAmount = pct;
    }


    private void EnemyDie()
    {
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.AddXP(xpReward);
        }

        if (CurrencyController.Instance != null && coinReward > 0)
        {
            CurrencyController.Instance.Add(coinReward);
            DamagePopupSpawner.SpawnCoins(
                transform.position + Vector3.up * popupAnchorLocalY, coinReward);
        }

        if (variant == dreadKnightBoss)
        {
            AchievementController.Instance.UnlockAchievement(dreadKnightAchievement);
            StatCounter.Instance.AddBossKill();
        }
        else if (variant == golemBoss)
        {
            AchievementController.Instance.UnlockAchievement(golemAchievement);
            StatCounter.Instance.AddBossKill();
        }
        else if (variant == chieftainBoss)
        {
            AchievementController.Instance.UnlockAchievement(chieftainAchievement);
            StatCounter.Instance.AddBossKill();
        }
        else if (variant == wraithLordBoss)
        {
            AchievementController.Instance.UnlockAchievement(wraithLordAchievement);
            StatCounter.Instance.AddBossKill();
        }
        else 
        {
            StatCounter.Instance.AddKill();
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw Aggro Range gizmo in Yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        // Draw Attack Range gizmo in Red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
