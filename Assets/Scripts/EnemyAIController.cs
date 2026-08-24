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
    [SerializeField] private float attackCooldown = 1.5f;
    
    [Header("Health Bar")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.3f, 0f);
    [SerializeField] private Vector2 healthBarWorldSize = new Vector2(1.0f, 0.14f);
    [SerializeField] private float maxHealth = 20f;

    private float currentHealth;

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

    private float defaultAgentSpeed;
    private float defaultAnimatorSpeed = 1f;
    private bool isTurningHome;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool hasSettledHome;
    private static Sprite s_whiteFillSprite;
    private Camera mainCam;
    private Coroutine aiCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Cache animator, check parent or children if not on this GameObject
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        currentHealth = maxHealth;

        mainCam = Camera.main;

        // Remembered so the enemy can walk back here once the player is dead.
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
        // Start the decision-making loop
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
        // Once the player is dead, stop hunting and head back to where we started.
        if (PlayerState.Instance != null && PlayerState.Instance.IsDead)
        {
            ReturnHome();
            return;
        }

        // Find player if reference is lost or not yet set
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

    /// <summary>
    /// Walks back to the spawn point and settles there. Used after the player dies
    /// so enemies do not stand around the corpse mid-swing.
    /// </summary>
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
                // Stroll rather than sprint on the way back.
                agent.speed = returnHomeSpeed;
                agent.SetDestination(spawnPosition);
            }

            if (animator != null)
            {
                // No walk clip exists on this rig, so the run is slowed down to read
                // as a walk instead.
                animator.speed = returnHomeAnimationSpeed;
                animator.SetBool("isMoving", true);
            }
            return;
        }

        // Arrived: come to a full stop before turning.
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            agent.speed = defaultAgentSpeed;

            // The agent steers its own rotation while pathing, which would fight the
            // turn back to the original facing, so hand rotation control back to us.
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

        // Smoothly rotate back to the facing this enemy started with, then settle.
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

            // Avoid triggering an attack if it is on cooldown
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                
                // Randomly set 'attackIndex' to 0 or 1
                int attackIndex = Random.Range(0, 2);
                animator.SetInteger("attackIndex", attackIndex);
                
                // Set the 'isAttacking' trigger
                animator.SetTrigger("isAttacking");
            }
        }
    }

    /// <summary>
    /// Performs damage to the player if they are still within range. Called via Animation Events.
    /// </summary>
    public void PerformDamage()
    {
        // An attack animation already in flight must not land on a dead player.
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

    /// <summary>
    /// Sets this enemy's stats before it is activated. RoomSpawner calls this while the
    /// object is still inactive, so the values are in place by the time Awake copies
    /// maxHealth into currentHealth.
    /// </summary>
    public void Configure(float newMaxHealth, float newDamage, float newXpReward)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        damage = Mathf.Max(0f, newDamage);
        xpReward = Mathf.Max(0f, newXpReward);
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

        currentHealth -= damageTaken;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHealthBar();

        PlayerState.Instance.Heal(incomingDamage * (PlayerState.Instance.currentLifeSteal / 100));

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
        //StartCoroutine(EnemyDieAfterDelay());
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.AddXP(xpReward);
        }

        StatCounter.Instance.AddKill();

        Destroy(gameObject);
    }

    //private IEnumerator EnemyDieAfterDelay()
    //{
        //if (animator != null)
        //{
        //    animator.SetTrigger("Die");
        //}

        //yield return new WaitForSeconds(1f);

        //Destroy(gameObject);
    //}

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
