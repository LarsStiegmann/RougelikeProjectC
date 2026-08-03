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
            // If the distance > aggroRange, set 'isMoving' to false.
            StopMovement();
        }
        else if (distance > attackRange)
        {
            // If the distance is between 'aggroRange' and 'attackRange', set 'isMoving' to true and move the NavMeshAgent toward the player.
            MoveTowardsPlayer();
        }
        else
        {
            // If the distance <= 'attackRange', set 'isMoving' to false, randomly set 'attackIndex' to 0 or 1, and set the 'isAttacking' trigger.
            TriggerAttack();
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
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
        }

        if (player != null)
        {
            // Calculate distance to verify if player is still within attack range (with a slight buffer for movement)
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
                    Debug.Log($"[EnemyAIController] Dealt {scaledDamage} damage to Player (base {damage}, multiplier {(RunTimerController.Instance != null ? RunTimerController.Instance.EnemyDamageMultiplier : 1f)}). Player remaining health is calculated via PlayerState.");
                }
                else
                {
                    Debug.LogWarning("[EnemyAIController] Player found but lacks PlayerState component.");
                }
            }
            else
            {
                Debug.Log("[EnemyAIController] Player moved out of range, attack missed.");
            }
        }
    }

public void EnemyTakeDamage(float incomingDamage)
    {
        currentHealth -= incomingDamage;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHealthBar();

        DamagePopupSpawner.Spawn(
            transform.position + Vector3.up * popupAnchorLocalY,
            incomingDamage
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

        Destroy(gameObject);
        //count kill
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
