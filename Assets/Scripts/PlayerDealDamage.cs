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

    [Header("Aiming")]
    [Tooltip("Radius of the aim check around the crosshair. Larger values are more forgiving to aim with.")]
    [SerializeField] private float aimAssistRadius = 0.4f;

    [Tooltip("Layers that block line of sight to a target, e.g. walls. Leave empty to disable the check.")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Slash VFX")]
    [Tooltip("How far along the camera's centre line the slash is placed, measured forward from the player. Larger pushes it further from the character.")]
    [SerializeField] private float slashForwardOffset = 0.7f;

    [Tooltip("Yaw applied to the slash to line the artwork up with the aim direction.")]
    [SerializeField] private float slashYawOffset = 50f;

    [Tooltip("Nudge to re-centre the slash artwork on the crosshair, in the slash's own local space. The VFX prefab's sub-effects are not centred on its origin.")]
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

        EnemyAIController target = FindTargetUnderCrosshair();
        if (target == null)
        {
            return;
        }

        target.EnemyTakeDamage(PlayerState.Instance.currentDamage);

        if (CrosshairHitMarker.Instance != null)
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

        if (cam == null)
        {
            cam = Camera.main;
        }

        Vector3 spawnPosition;
        Quaternion aimRotation;

        if (cam != null)
        {
            // Sit the effect directly on the camera's centre line, which is exactly
            // where the crosshair dot is, so the slash reads as centred on it
            // regardless of which way the character happens to be facing.
            Transform camT = cam.transform;
            float distanceToPlayer = Vector3.Distance(camT.position, transform.position);
            spawnPosition = camT.position + camT.forward * (distanceToPlayer + slashForwardOffset);

            // Keep the sweep level rather than tipping with camera pitch.
            Vector3 aimDirection = camT.forward;
            aimDirection.y = 0f;
            aimRotation = aimDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(aimDirection.normalized, Vector3.up)
                : transform.rotation;
        }
        else if (attackPoint != null)
        {
            spawnPosition = attackPoint.position;
            aimRotation = attackPoint.rotation;
        }
        else
        {
            return;
        }

        Quaternion finalRotation = aimRotation * Quaternion.Euler(0f, slashYawOffset, 0f);

        // Applied in the slash's own space so the correction follows the aim.
        spawnPosition += finalRotation * slashCenterCorrection;

        GameObject slash = Instantiate(shlashVFX, spawnPosition, finalRotation);

        Destroy(slash, 0.2f);
    }

    /// <summary>
    /// Fires a ray from the camera straight through the centre-screen crosshair and
    /// returns the closest enemy under it that is also within the player's attack
    /// range and not hidden behind geometry.
    /// </summary>
    private EnemyAIController FindTargetUnderCrosshair()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null || PlayerState.Instance == null)
        {
            return null;
        }

        // Use the camera's own viewport rather than Screen.*, which is not reliable
        // in every context and would aim the ray incorrectly.
        Ray ray = cam.ScreenPointToRay(new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f));

        float attackRange = PlayerState.Instance.currentAttackRange;

        // The camera sits behind the player, so the ray has to cover that gap too.
        float searchDistance = Vector3.Distance(cam.transform.position, transform.position) + attackRange + 1f;

        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            aimAssistRadius,
            searchDistance,
            enemyLayer,
            // Enemy capsules are triggers, so these must be included.
            QueryTriggerInteraction.Collide
        );

        if (hits.Length == 0)
        {
            return null;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyAIController enemy = hits[i].collider.GetComponentInParent<EnemyAIController>();
            if (enemy == null)
            {
                continue;
            }

            // Aiming at something far across the room must not let the player hit it.
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy > attackRange + aimAssistRadius)
            {
                continue;
            }

            if (IsBlocked(enemy))
            {
                continue;
            }

            return enemy;
        }

        return null;
    }

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

        // Reach of the attack, regardless of where the crosshair points.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        // Aim ray through the crosshair.
        Camera c = Camera.main;
        if (c != null)
        {
            Gizmos.color = Color.cyan;
            Ray ray = c.ScreenPointToRay(new Vector3(c.pixelWidth * 0.5f, c.pixelHeight * 0.5f, 0f));
            Gizmos.DrawRay(ray.origin, ray.direction * (Vector3.Distance(c.transform.position, transform.position) + range));
        }
    }
}
