using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks the volume of one room and the enemies that belong to it. SwarmSpawner asks
/// which zone the player is standing in and draws from that zone's roster, so each
/// area keeps its own flavour even though enemies now spawn around the player rather
/// than being placed per room.
///
/// Zones register themselves, so the spawner does not need references wiring up.
/// </summary>
public class RoomZone : MonoBehaviour
{
    public static readonly List<RoomZone> ActiveZones = new List<RoomZone>();

    [Header("Identity")]
    [SerializeField] private string roomName = "Room";

    [Header("Volume")]
    [Tooltip("World-space centre of the room.")]
    [SerializeField] private Vector3 boundsCentre;

    [Tooltip("World-space size of the room.")]
    [SerializeField] private Vector3 boundsSize = Vector3.one * 10f;

    [Header("Roster")]
    [Tooltip("Enemy types that belong to this room.")]
    [SerializeField] private List<EnemyVariant> roster = new List<EnemyVariant>();

    /// <summary>Room label, for logs and debugging.</summary>
    public string RoomName => roomName;

    /// <summary>The enemies this room contributes.</summary>
    public List<EnemyVariant> Roster => roster;

    /// <summary>World-space volume of this room.</summary>
    public Bounds Bounds => new Bounds(boundsCentre, boundsSize);

    private void OnEnable()
    {
        if (!ActiveZones.Contains(this))
        {
            ActiveZones.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
    }

    /// <summary>True if the given world position sits inside this room.</summary>
    public bool Contains(Vector3 worldPosition)
    {
        return Bounds.Contains(worldPosition);
    }

    /// <summary>
    /// Squared distance from a point to this room's centre, used to pick the nearest
    /// zone when the player is in a corridor that belongs to no room.
    /// </summary>
    public float SqrDistanceTo(Vector3 worldPosition)
    {
        return (boundsCentre - worldPosition).sqrMagnitude;
    }

    /// <summary>Finds the zone containing the position, else the nearest one.</summary>
    public static RoomZone FindFor(Vector3 worldPosition)
    {
        RoomZone nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < ActiveZones.Count; i++)
        {
            RoomZone zone = ActiveZones[i];
            if (zone == null || zone.roster.Count == 0)
            {
                continue;
            }

            if (zone.Contains(worldPosition))
            {
                return zone;
            }

            float sqr = zone.SqrDistanceTo(worldPosition);
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = zone;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireCube(boundsCentre, boundsSize);
    }
}
