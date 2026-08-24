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

    [Header("Attack Arc")]
    [Tooltip("Total width of the damage arc in front of the character, in degrees.")]
    [SerializeField] private float attackArcAngle = 180f;

    [Tooltip("Height above the character's feet used as the centre of the hit check.")]
    [SerializeField] private float attackCheckHeight = 1f;

    [Tooltip("Layers that block line of sight to a target, e.g. walls. Leave empty to disable the check.")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Slash VFX")]
    [Tooltip("Extra push forward along the character's facing. The arc already pivots on the character, so 0 keeps it centred on them.")]
    [SerializeField] private float slashForwardOffset = 0f;

    [Tooltip("Height above the character's feet at which the slash arc sits.")]
    [SerializeField] private float slashHeight = 1.1f;

    [Tooltip("Yaw applied to the slash to line the artwork up with the facing direction.")]
    [SerializeField] private float slashYawOffset = 50f;

    [Tooltip("Positional nudge for the slash artwork, in the slash's own local space.")]
    [SerializeField] private Vector3 slashCenterCorrection = Vector3.zero;

    private Camera cam;

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
        SpawnSlash();

        int enemiesHit = DamageEnemiesInArc();

        if (enemiesHit > 0 && CrosshairHitMarker.Instance != null)
        {
            CrosshairHitMarker.Instance.Flash();
        }
    }

private void SpawnSlash()
    {
        if (shlashVFX == null)
        {
            return;
        }

        // Parented to the character so the slash travels with them. Without this it
        // would hang in world space while the player kept falling or running.
        GameObject slash = Instantiate(shlashVFX, transform);

        slash.transform.localPosition = new Vector3(0f, slashHeight, 0f) + Vector3.forward * slashForwardOffset;
        slash.transform.localRotation = Quaternion.Euler(0f, slashYawOffset, 0f);
        slash.transform.localPosition += slash.transform.localRotation * slashCenterCorrection;

        // SlashEffect removes itself when its animation ends; this is only a
        // safety net in case a VFX prefab without that script is assigned.
        Destroy(slash, 2f);
    }

/// <summary>
    /// Damages every enemy inside the arc the character is facing. Multiple enemies
    /// can be hit by one swing, matching the feel of survivors-style games.
    /// </summary>
    private int DamageEnemiesInArc()
    {
        if (PlayerState.Instance == null)
        {
            return 0;
        }

        float range = PlayerState.Instance.currentAttackRange;
        Vector3 center = transform.position + Vector3.up * attackCheckHeight;

        Collider[] candidates = Physics.OverlapSphere(
            center,
            range,
            enemyLayer,
            // Enemy capsules are triggers, so they must be included.
            QueryTriggerInteraction.Collide
        );

        if (candidates.Length == 0)
        {
            return 0;
        }

        HashSet<EnemyAIController> alreadyHit = new HashSet<EnemyAIController>();
        float halfAngle = attackArcAngle * 0.5f;
        int hitCount = 0;

        Vector3 facing = transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f)
        {
            return 0;
        }
        facing.Normalize();

        for (int i = 0; i < candidates.Length; i++)
        {
            EnemyAIController enemy = candidates[i].GetComponentInParent<EnemyAIController>();
            if (enemy == null || !alreadyHit.Add(enemy))
            {
                continue;
            }

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;

            // Anything essentially on top of the player always counts as in front.
            if (toEnemy.sqrMagnitude > 0.0001f)
            {
                if (Vector3.Angle(facing, toEnemy.normalized) > halfAngle)
                {
                    continue;
                }
            }

            if (IsBlocked(enemy))
            {
                continue;
            }

            enemy.EnemyTakeDamage(PlayerState.Instance.currentDamage);
            hitCount++;
        }

        return hitCount;
    }


    /// <summary>
    /// Fires a ray from the camera straight through the centre-screen crosshair and
    /// returns the closest enemy under it that is also within the player's attack
    /// range and not hidden behind geometry.
    /// </summary>


    private bool IsBlocked(EnemyAIController enemy)
    {
        if (obstacleLayer.value == 0)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up;
        Vector3 targetPoint = enemy.transform.position + Vector3.up;
        Vector3 direction = targetPoint - origin;

        return Physics.Raycast(
            origin,
            direction.normalized,
            direction.magnitude,
            obstacleLayer,
            QueryTriggerInteraction.Ignore
        );
    }


private void OnDrawGizmosSelected()
    {
        float range = (Application.isPlaying && PlayerState.Instance != null)
            ? PlayerState.Instance.currentAttackRange
            : 2f;

        Vector3 center = transform.position + Vector3.up * attackCheckHeight;

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawWireSphere(center, range);

        // Edges of the damage arc.
        Gizmos.color = Color.red;
        float half = attackArcAngle * 0.5f;
        Vector3 left = Quaternion.Euler(0f, -half, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, half, 0f) * transform.forward;
        Gizmos.DrawRay(center, left * range);
        Gizmos.DrawRay(center, right * range);
        Gizmos.DrawRay(center, transform.forward * range);
    }
}
