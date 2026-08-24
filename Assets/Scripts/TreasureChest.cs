using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Makes a chest prop openable. The lid child is rotated on its hinge when opened.
/// Chests register themselves so ChestInteractor can find the nearest one without
/// running physics queries every frame.
/// </summary>
public class TreasureChest : MonoBehaviour
{
    public static readonly List<TreasureChest> ActiveChests = new List<TreasureChest>();

    [Header("Lid")]
    [Tooltip("The lid transform. If left empty, the first child with 'Lid' in its name is used.")]
    [SerializeField] private Transform lid;

    [Tooltip("Degrees to rotate the lid about its local X axis when opening. Negative swings the lid up and back.")]
    [SerializeField] private float openAngle = -100f;

    [Tooltip("How long the lid takes to swing open, in seconds.")]
    [SerializeField] private float openDuration = 0.6f;

    [Tooltip("How long the lid takes to swing shut again, in seconds.")]
    [SerializeField] private float closeDuration = 0.45f;

    [Header("Interaction")]
    [Tooltip("How close the player must be to open this chest.")]
    [SerializeField] private float interactionRange = 3f;

    [Tooltip("Text shown in the interaction prompt when the player is in range.")]
    [SerializeField] private string promptText = "Press E to open";

    [Header("Events")]
    [Tooltip("Fired once when the chest is opened. Hook rewards up here.")]
    [SerializeField] private UnityEvent onOpened;

    private Quaternion closedRotation;
    private bool isOpened;
    private Coroutine openRoutine;

    public bool IsOpened => isOpened;
    public float InteractionRange => interactionRange;
    public string PromptText => promptText;

    private void Awake()
    {
        if (lid == null)
        {
            lid = FindLid();
        }

        if (lid != null)
        {
            closedRotation = lid.localRotation;
        }
    }

    private void OnEnable()
    {
        if (!ActiveChests.Contains(this))
        {
            ActiveChests.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveChests.Remove(this);
    }

    private Transform FindLid()
    {
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("lid"))
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// Opens the chest. Safe to call more than once; only the first call does anything.
    /// </summary>
    public void Open()
    {
        if (isOpened)
        {
            return;
        }

        isOpened = true;

        if (lid != null)
        {
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(SwingLidOpen());
        }

        onOpened?.Invoke();
    }

    private IEnumerator SwingLidOpen()
    {
        Quaternion target = closedRotation * Quaternion.Euler(openAngle, 0f, 0f);

        if (openDuration <= 0f)
        {
            lid.localRotation = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);

            // Ease out so the lid slows as it reaches the top.
            t = 1f - (1f - t) * (1f - t);

            lid.localRotation = Quaternion.Slerp(closedRotation, target, t);
            yield return null;
        }

        lid.localRotation = target;
        openRoutine = null;
    }

    /// <summary>
    /// Closes the chest again so it can be opened another time. Safe to call when
    /// already closed; only a chest that is currently open does anything.
    /// </summary>
    public void Close()
    {
        if (!isOpened)
        {
            return;
        }

        isOpened = false;

        if (lid != null)
        {
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(SwingLidClosed());
        }
    }

    private IEnumerator SwingLidClosed()
    {
        Quaternion start = lid.localRotation;

        if (closeDuration <= 0f)
        {
            lid.localRotation = closedRotation;
            openRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);

            // Ease in so the lid picks up speed and drops shut.
            t = t * t;

            lid.localRotation = Quaternion.Slerp(start, closedRotation, t);
            yield return null;
        }

        lid.localRotation = closedRotation;
        openRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
