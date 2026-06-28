using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float aggroRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private GameObject player;
    private float lastAttackTime;
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
                    playerState.TakeDamage(damage);
                    Debug.Log($"[EnemyAIController] Dealt {damage} damage to Player. Player remaining health is calculated via PlayerState.");
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
