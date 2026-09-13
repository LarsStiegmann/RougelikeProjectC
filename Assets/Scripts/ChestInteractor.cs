using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

    //_________________________________________________________________________________________________________
    //Quelle: KI (Claude Opus 5)
    //Prompt: Generate an interactor for the chest with a prompt for opening said chest
    //Datum: 26.08.2026

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

        ChestPrice price = currentTarget.GetComponent<ChestPrice>();
        if (price != null && !price.TryPay())
        {
            return;
        }

        currentTarget.Open();
        HidePrompt();
        currentTarget = null;
    }

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
}   //_________________________________________________________________________________________________________
