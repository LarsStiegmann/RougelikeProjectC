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
            ShowPrompt(BuildPrompt(currentTarget));
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

        // Chests cost coins, and the price doubles each time this one is looted.
        ChestPrice price = currentTarget.GetComponent<ChestPrice>();
        if (price != null && !price.TryPay())
        {
            // Not enough coins: leave it shut and keep the prompt up.
            return;
        }

        currentTarget.Open();
        HidePrompt();
        currentTarget = null;
    }

    /// <summary>
    /// Appends the chest's price to its prompt, and says so plainly when the player
    /// cannot afford it.
    /// </summary>
    private string BuildPrompt(TreasureChest chest)
    {
        ChestPrice price = chest.GetComponent<ChestPrice>();
        if (price == null)
        {
            return chest.PromptText;
        }

        int cost = price.CurrentCost;
        return price.CanAfford()
            ? chest.PromptText + " (" + cost + " coins)"
            : "Need " + cost + " coins";
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
