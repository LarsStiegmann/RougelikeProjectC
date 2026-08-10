using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shatters the character on death by breaking its actual mesh into chunks rather
/// than hiding it and spawning stand-in debris. The character's pose is baked at
/// the moment of death, the baked triangles are clustered into fragments, and each
/// fragment becomes a physics body carrying the original material - so the pieces
/// visibly are the character coming apart.
/// </summary>
public class PlayerShatterDeath : MonoBehaviour
{
    [Header("Fragmentation")]
    [Tooltip("How many pieces each body part breaks into.")]
    [SerializeField] private int fragmentsPerPart = 5;

    [Tooltip("Hard cap on total fragments, to protect frame rate.")]
    [SerializeField] private int maxFragments = 90;

    [Header("Physics")]
    [SerializeField] private Vector2 burstForceRange = new Vector2(1.5f, 4f);

    [Tooltip("Upward bias so the body bursts rather than only collapsing.")]
    [SerializeField] private float upwardBias = 0.9f;

    [SerializeField] private float fragmentMass = 0.2f;
    [SerializeField] private float fragmentDrag = 0.1f;

    [Tooltip("Spin applied to each piece.")]
    [SerializeField] private float torque = 2.5f;

    [Header("Glass sparkle (optional)")]
    [Tooltip("Extra glass shards thrown in for sparkle. Leave empty to use body pieces only.")]
    [SerializeField] private Mesh[] shardMeshes;

    [SerializeField] private Material shardMaterial;
    [SerializeField] private int shardCount = 14;
    [SerializeField] private Vector2 shardScaleRange = new Vector2(0.05f, 0.12f);

    [Header("Lifetime")]
    [Tooltip("Leave the fragments lying in the world instead of dissolving them away.")]
    [SerializeField] private bool fragmentsPersist = true;

    [Tooltip("Only used when fragmentsPersist is off.")]
    [SerializeField] private float lifetime = 2.4f;
    [SerializeField] private float shrinkDuration = 0.9f;

    [Header("Layers")]
    [Tooltip("Layer for debris. Should collide with the level but not the player or enemies.")]
    [SerializeField] private int debrisLayer = 0;

    private bool hasShattered;
    private Transform pileRoot;

    public void Shatter()
    {
        if (hasShattered)
        {
            return;
        }
        hasShattered = true;

        List<SkinnedMeshRenderer> parts = new List<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>(false))
        {
            if (smr.enabled && smr.sharedMesh != null)
            {
                parts.Add(smr);
            }
        }

        if (parts.Count == 0)
        {
            return;
        }

        // Body centre, used to push pieces outward.
        Bounds combined = parts[0].bounds;
        for (int i = 1; i < parts.Count; i++)
        {
            combined.Encapsulate(parts[i].bounds);
        }
        Vector3 center = combined.center;

        // When persisting, park everything under a root that survives scene reloads
        // so the debris is still there after a retry.
        if (fragmentsPersist)
        {
            pileRoot = PersistentDebris.GetOrCreate().CreatePile();
        }

        int budget = maxFragments;

        foreach (SkinnedMeshRenderer smr in parts)
        {
            if (budget <= 0)
            {
                break;
            }

            int want = Mathf.Min(fragmentsPerPart, budget);
            int made = FragmentPart(smr, want, center);
            budget -= made;
        }

        // hide the original character
        foreach (SkinnedMeshRenderer smr in parts)
        {
            smr.enabled = false;
        }
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>(false))
        {
            mr.enabled = false;
        }

        SpawnGlassSparkle(center, combined);
    }

    /// <summary>
    /// Bakes one skinned part in its current pose and splits its triangles into
    /// spatial clusters, each becoming an independent physics fragment.
    /// </summary>
    private int FragmentPart(SkinnedMeshRenderer smr, int fragmentCount, Vector3 center)
    {
        Mesh baked = new Mesh();
        smr.BakeMesh(baked, true);

        Vector3[] verts = baked.vertices;
        int[] tris = baked.triangles;
        Vector2[] uvs = baked.uv;
        Vector3[] normals = baked.normals;

        if (verts.Length == 0 || tris.Length < 3)
        {
            Destroy(baked);
            return 0;
        }

        int triCount = tris.Length / 3;
        fragmentCount = Mathf.Clamp(fragmentCount, 1, Mathf.Max(1, triCount));

        // Seed points chosen from actual triangle centroids, then every triangle
        // joins its nearest seed. Gives chunky, irregular pieces.
        Vector3[] seeds = new Vector3[fragmentCount];
        for (int i = 0; i < fragmentCount; i++)
        {
            int t = Random.Range(0, triCount) * 3;
            seeds[i] = (verts[tris[t]] + verts[tris[t + 1]] + verts[tris[t + 2]]) / 3f;
        }

        List<int>[] buckets = new List<int>[fragmentCount];
        for (int i = 0; i < fragmentCount; i++)
        {
            buckets[i] = new List<int>();
        }

        for (int t = 0; t < triCount; t++)
        {
            int b = t * 3;
            Vector3 c = (verts[tris[b]] + verts[tris[b + 1]] + verts[tris[b + 2]]) / 3f;

            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < fragmentCount; i++)
            {
                float d = (c - seeds[i]).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            buckets[best].Add(t);
        }

        Material mat = smr.sharedMaterial;
        int created = 0;

        for (int i = 0; i < fragmentCount; i++)
        {
            if (buckets[i].Count == 0)
            {
                continue;
            }

            Mesh piece = BuildPiece(buckets[i], verts, tris, uvs, normals);
            if (piece == null)
            {
                continue;
            }

            GameObject frag = new GameObject("BodyFragment");
            frag.layer = debrisLayer;
            frag.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            frag.transform.localScale = smr.transform.lossyScale;

            frag.AddComponent<MeshFilter>().sharedMesh = piece;

            MeshRenderer mr = frag.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            AddPhysics(frag, piece.bounds, center);
            StartCoroutine(HandleLifetime(frag));
            created++;
        }

        Destroy(baked);
        return created;
    }

    private Mesh BuildPiece(List<int> triIndices, Vector3[] verts, int[] tris, Vector2[] uvs, Vector3[] normals)
    {
        Dictionary<int, int> remap = new Dictionary<int, int>();
        List<Vector3> pv = new List<Vector3>();
        List<Vector2> pu = new List<Vector2>();
        List<Vector3> pn = new List<Vector3>();
        List<int> pt = new List<int>();

        bool hasUv = uvs != null && uvs.Length == verts.Length;
        bool hasNormals = normals != null && normals.Length == verts.Length;

        foreach (int t in triIndices)
        {
            int b = t * 3;
            for (int k = 0; k < 3; k++)
            {
                int original = tris[b + k];
                int mapped;
                if (!remap.TryGetValue(original, out mapped))
                {
                    mapped = pv.Count;
                    remap[original] = mapped;
                    pv.Add(verts[original]);
                    if (hasUv) pu.Add(uvs[original]);
                    if (hasNormals) pn.Add(normals[original]);
                }
                pt.Add(mapped);
            }
        }

        if (pv.Count == 0 || pt.Count < 3)
        {
            return null;
        }

        Mesh m = new Mesh();
        m.SetVertices(pv);
        if (hasUv) m.SetUVs(0, pu);
        if (hasNormals) m.SetNormals(pn);
        m.SetTriangles(pt, 0);
        m.RecalculateBounds();
        if (!hasNormals) m.RecalculateNormals();
        return m;
    }

    private void AddPhysics(GameObject go, Bounds localBounds, Vector3 center)
    {
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.center = localBounds.center;
        // keep colliders modest so pieces don't visibly hover apart
        col.size = Vector3.Max(localBounds.size * 0.8f, Vector3.one * 0.02f);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = fragmentMass;
        rb.linearDamping = fragmentDrag;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        Vector3 worldCentre = go.transform.TransformPoint(localBounds.center);
        Vector3 outward = worldCentre - center;
        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = Random.onUnitSphere;
        }
        outward = outward.normalized + Vector3.up * upwardBias * 0.5f;

        rb.AddForce(outward.normalized * Random.Range(burstForceRange.x, burstForceRange.y), ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
    }

    private void SpawnGlassSparkle(Vector3 center, Bounds bounds)
    {
        if (shardMeshes == null || shardMeshes.Length == 0 || shardCount <= 0)
        {
            return;
        }

        for (int i = 0; i < shardCount; i++)
        {
            Vector3 pos = center + new Vector3(
                Random.Range(-bounds.extents.x, bounds.extents.x),
                Random.Range(-bounds.extents.y, bounds.extents.y),
                Random.Range(-bounds.extents.z, bounds.extents.z));

            GameObject shard = new GameObject("GlassShard");
            shard.layer = debrisLayer;
            shard.transform.position = pos;
            shard.transform.rotation = Random.rotation;
            shard.transform.localScale = Vector3.one * Random.Range(shardScaleRange.x, shardScaleRange.y);

            shard.AddComponent<MeshFilter>().sharedMesh = shardMeshes[Random.Range(0, shardMeshes.Length)];
            MeshRenderer mr = shard.AddComponent<MeshRenderer>();
            mr.sharedMaterial = shardMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            AddPhysics(shard, new Bounds(Vector3.zero, Vector3.one * 0.5f), center);
            StartCoroutine(HandleLifetime(shard));
        }
    }

    private IEnumerator ShrinkAndDestroy(GameObject go)
    {
        yield return new WaitForSeconds(lifetime + Random.Range(0f, 0.5f));

        if (go == null)
        {
            yield break;
        }

        Vector3 start = go.transform.localScale;
        float t = 0f;
        while (t < shrinkDuration && go != null)
        {
            t += Time.deltaTime;
            go.transform.localScale = Vector3.Lerp(start, Vector3.zero, t / shrinkDuration);
            yield return null;
        }

        if (go != null)
        {
            Destroy(go);
        }
    }


private IEnumerator HandleLifetime(GameObject go)
    {
        if (!fragmentsPersist)
        {
            yield return ShrinkAndDestroy(go);
            yield break;
        }

        // Persisting: keep the piece and hand the settle-and-freeze work to
        // PersistentDebris, which outlives this player object across reloads.
        if (go == null)
        {
            yield break;
        }

        if (pileRoot != null)
        {
            go.transform.SetParent(pileRoot, true);
        }

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            PersistentDebris.GetOrCreate().SettleWhenAsleep(rb);
        }
    }
}
