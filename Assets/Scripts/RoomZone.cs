using System.Collections.Generic;
using UnityEngine;

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

    public string RoomName => roomName;

    public List<EnemyVariant> Roster => roster;

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

    public bool Contains(Vector3 worldPosition)
    {
        return Bounds.Contains(worldPosition);
    }


    public float SqrDistanceTo(Vector3 worldPosition)
    {
        return (boundsCentre - worldPosition).sqrMagnitude;
    }

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
