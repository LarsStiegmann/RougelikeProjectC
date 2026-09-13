using UnityEngine;

public class GlowingEyes : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Radius of one eye sprite, before the model's own scale.")]
    [SerializeField] private float eyeSize = 0.045f;

    [Tooltip("Half the gap between the two eyes.")]
    [SerializeField] private float eyeSeparation = 0.038f;

    [Tooltip("Nudge along the character's forward axis. 0 sits exactly in the socket; negative sinks into the skull.")]
    [SerializeField] private float forwardOffset = 0f;

    [Tooltip("Vertical nudge from the eye bone.")]
    [SerializeField] private float heightOffset = 0f;

    [Tooltip("Degrees the eye sprites tilt up toward the overhead camera. 0 lies flat on the face.")]
    [SerializeField] private float faceTilt = 22f;

    [Header("Glow")]
    [Tooltip("Peak sprite brightness at full night. Above 1 so bloom catches it.")]
    [SerializeField] private float glowIntensity = 3.2f;

    [Tooltip("Peak point-light intensity at full night.")]
    [SerializeField] private float lightIntensity = 0f;

    [Tooltip("Point-light reach in metres.")]
    [SerializeField] private float lightRange = 1.5f;

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed = 6.5f;

    [Tooltip("How deep the flicker dips, 0-1.")]
    [SerializeField] private float flickerDepth = 0.1f;

    [Header("Budget")]
    [Tooltip("Past this distance from the camera the point light switches off.")]
    [SerializeField] private float lightCullDistance = 26f;

    [Tooltip("Most eye lights allowed at once. Sprites are never capped.")]
    [SerializeField] private int maxSimultaneousLights = 14;

    private Transform anchor;
    private Transform leftEye;
    private Transform rightEye;
    private Renderer leftRenderer;
    private Renderer rightRenderer;
    private Light spill;
    private Color eyeColour = new Color(1f, 0.28f, 0.12f);
    private float flickerSeed;
    private MaterialPropertyBlock block;

    private static Material sharedMaterial;
    private static Mesh sharedQuad;
    private static readonly int ColourId = Shader.PropertyToID("_Color");

    private static int lightsThisFrame;
    private static int budgetFrame = -1;

    private void OnEnable()
    {
        flickerSeed = Random.value * 100f;
        Build();
    }

    private void Build()
    {
        if (leftEye != null && anchor != null)
        {
            eyeColour = PickColour();
            return;
        }

        anchor = FindEyeBone();
        if (anchor == null)
        {
            enabled = false;
            return;
        }

        eyeColour = PickColour();
        block = new MaterialPropertyBlock();

        leftEye = CreateEye("EyeGlow_L", out leftRenderer);
        rightEye = CreateEye("EyeGlow_R", out rightRenderer);

        if (lightIntensity > 0f)
        {
            GameObject lightGo = new GameObject("EyeSpill");
            lightGo.transform.SetParent(anchor, false);
            spill = lightGo.AddComponent<Light>();
            spill.type = LightType.Point;
            spill.range = lightRange;
            spill.shadows = LightShadows.None;
            spill.renderMode = LightRenderMode.ForceVertex;
        }
    }

    private Transform CreateEye(string eyeName, out Renderer r)
    {
        GameObject go = new GameObject(eyeName);
        go.transform.SetParent(anchor, false);
        go.transform.localScale = Vector3.one * eyeSize;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GetQuad();

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GetMaterial();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        r = mr;
        return go.transform;
    }

    private Color PickColour()
    {
        string mesh = string.Empty;

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf && child.GetComponent<SkinnedMeshRenderer>() != null)
            {
                mesh = child.name;
                break;
            }
        }

        if (mesh.Contains("Ghost") || mesh.Contains("Tormented"))
        {
            return new Color(0.45f, 1f, 0.85f);
        }

        if (mesh.Contains("Goblin"))
        {
            return new Color(0.75f, 1f, 0.25f);
        }

        if (mesh.Contains("Golem"))
        {
            return new Color(1f, 0.55f, 0.15f);
        }

        return new Color(1f, 0.28f, 0.12f);
    }

    private Transform FindEyeBone()
    {
        Transform t = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Neck/Head/Eyes");
        if (t != null)
        {
            return t;
        }

        Transform head = null;
        foreach (Transform b in GetComponentsInChildren<Transform>(true))
        {
            if (b.name == "Eyes")
            {
                return b;
            }

            if (head == null && b.name == "Head")
            {
                head = b;
            }
        }

        return head;
    }

    private void LateUpdate()
    {
        if (anchor == null || leftEye == null)
        {
            return;
        }

        float darkness = DayNightCycle.Darkness;

        float flicker = 1f - flickerDepth * (0.5f + 0.5f * Mathf.Sin((Time.time + flickerSeed) * flickerSpeed));
        float strength = darkness * flicker;

        bool visible = darkness > 0.02f;
        if (leftEye.gameObject.activeSelf != visible)
        {
            leftEye.gameObject.SetActive(visible);
            rightEye.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            if (spill != null)
            {
                spill.enabled = false;
            }

            return;
        }


        float s = transform.lossyScale.x;
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;
        Vector3 centre = anchor.position + fwd * (forwardOffset * s) + Vector3.up * (heightOffset * s);

        leftEye.position = centre - right * (eyeSeparation * s);
        rightEye.position = centre + right * (eyeSeparation * s);

        Camera cam = Camera.main;

        Quaternion facing = Quaternion.LookRotation(fwd, Vector3.up);
        facing = Quaternion.AngleAxis(-faceTilt, right) * facing;
        leftEye.rotation = facing;
        rightEye.rotation = facing;

        Color hdr = eyeColour * (glowIntensity * strength);
        hdr.a = 1f;

        block.SetColor(ColourId, hdr);
        leftRenderer.SetPropertyBlock(block);
        rightRenderer.SetPropertyBlock(block);

        UpdateSpill(cam, centre, strength);
    }

    private void UpdateSpill(Camera cam, Vector3 centre, float strength)
    {
        if (spill == null)
        {
            return;
        }

        if (budgetFrame != Time.frameCount)
        {
            budgetFrame = Time.frameCount;
            lightsThisFrame = 0;
        }

        bool near = cam != null &&
                    (cam.transform.position - centre).sqrMagnitude < lightCullDistance * lightCullDistance;

        if (near && lightsThisFrame < maxSimultaneousLights)
        {
            lightsThisFrame++;
            spill.enabled = true;
            spill.transform.position = centre;
            spill.color = eyeColour;
            spill.intensity = lightIntensity * strength;
            spill.range = lightRange * transform.lossyScale.x;
        }
        else
        {
            spill.enabled = false;
        }
    }

    private static Mesh GetQuad()
    {
        if (sharedQuad != null)
        {
            return sharedQuad;
        }

        sharedQuad = new Mesh();
        sharedQuad.name = "EyeQuad";
        sharedQuad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        sharedQuad.uv = new Vector2[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 1f), new Vector2(1f, 1f)
        };
        sharedQuad.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        sharedQuad.RecalculateBounds();
        return sharedQuad;
    }

    private static Material GetMaterial()
    {
        if (sharedMaterial != null)
        {
            return sharedMaterial;
        }

        sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        sharedMaterial.name = "EyeGlow";
        sharedMaterial.mainTexture = BuildGlowTexture();
        sharedMaterial.enableInstancing = true;

        sharedMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return sharedMaterial;
    }

     private static Texture2D BuildGlowTexture()
    {
        const int S = 64;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f;
                float dy = (y + 0.5f) / S - 0.5f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);

                float core = Mathf.Clamp01(1f - d * 4.5f);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 4.5f);
                float a = Mathf.Clamp01(core * 0.8f + halo * 0.3f);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }
}
