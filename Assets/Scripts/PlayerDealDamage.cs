using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDealDamage : MonoBehaviour
{
    private float baseAttackInterval = 1f;
    private float timer;

    [SerializeField] private Transform attackPoint;

    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] private GameObject shlashVFX;

    [SerializeField] private Vector3 boxSize = new Vector3(1f, 1f, 2f);

    private void Update()
    {
        timer += Time.deltaTime;

        float currentAttackInterval = baseAttackInterval / PlayerState.Instance.currentAttackSpeed;

        if (timer >= currentAttackInterval)
        {
            timer -= currentAttackInterval;
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        Quaternion rotationOffset = Quaternion.Euler(0, 50, 0);

        GameObject slash = Instantiate(shlashVFX, 
            attackPoint.position, 
            attackPoint.rotation * rotationOffset, 
            attackPoint
        );

        Collider[] hits = Physics.OverlapBox(
            attackPoint.position + transform.forward * 1f,
            boxSize / 2,
            transform.rotation,
            enemyLayer
        );

        Destroy(slash, 0.2f);

        HashSet<EnemyAIController> enemiesHit = new HashSet<EnemyAIController>();

        foreach (Collider hit in hits)
        {
            EnemyAIController enemy = hit.GetComponentInParent<EnemyAIController>();

            if (enemy != null && enemiesHit.Add(enemy))
            {
                //enemy.TakeDamage(PlayerState.Instance.currentDamage);
            }
        }
    }
}
