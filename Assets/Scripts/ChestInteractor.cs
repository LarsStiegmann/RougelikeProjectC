using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Lets the player open nearby TreasureChests with the Interact action, and drives
/// the on-screen "Press E to open" prompt.
/// </summary>
public class ChestInteractor : MonoBehaviour
{
    [Header("Prompt")]
    [Tooltip("Text element shown when an openable chest is in range.")]
    [SerializeField] private TMP_Text promptText;

    [Header("Detection")]
    [Tooltip("Extra reach on top of each chest's own interaction range.")]
    [SerializeField] private float rangePadding = 0f;

    private TreasureChest currentTarget;

private void OnEnable()
    {
        InputController input = InputController.Instance;
        if (input != null && input.Actions != null)
        {
            input.Actions.Player.Interact.performed += OnInteract;
        }

        HidePrompt();
    }

private void OnDisable()
    {
        InputController input = InputController.Instance;
        if (input != null && input.Actions != null)
        {
            input.Actions.Player.Interact.performed -= OnInteract;
        }
    }

    private void Update()
    {
        currentTarget = FindNearestChest();

        if (currentTarget != null)
        {
            ShowPrompt(currentTarget.PromptText);
        }
        else
        {
            HidePrompt();
        }
    }

    private TreasureChest FindNearestChest()
    {
        TreasureChest nearest = null;
        float nearestSqr = float.MaxValue;
        Vector3 position = transform.position;

        var chests = TreasureChest.ActiveChests;
        for (int i = 0; i < chests.Count; i++)
        {
            TreasureChest chest = chests[i];
            if (chest == null || chest.IsOpened)
            {
                continue;
            }

            float range = chest.InteractionRange + rangePadding;
            float sqrDistance = (chest.transform.position - position).sqrMagnitude;

            if (sqrDistance <= range * range && sqrDistance < nearestSqr)
            {
                nearestSqr = sqrDistance;
                nearest = chest;
            }
        }

        return nearest;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (currentTarget == null || currentTarget.IsOpened)
        {
            return;
        }

        currentTarget.Open();
        HidePrompt();
        currentTarget = null;
    }

    private void ShowPrompt(string message)
    {
        if (promptText == null)
        {
            return;
        }

        if (promptText.text != message)
        {
            promptText.text = message;
        }

        if (!promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (promptText != null && promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}
