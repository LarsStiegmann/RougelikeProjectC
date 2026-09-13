using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GameOverBackgroundFade : MonoBehaviour
{
    [Tooltip("Seconds for the backdrop to reach solid black.")]
    [SerializeField] private float fadeDuration = 3f;

    private Image backdrop;
    private float elapsed;

    private void Awake()
    {
        backdrop = GetComponent<Image>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        backdrop.color = new Color(0f, 0f, 0f, 0f);
    }

    private void Update()
    {
        if (elapsed >= fadeDuration)
        {
            return;
        }

        elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        float a = Mathf.Clamp01(elapsed / fadeDuration);
        backdrop.color = new Color(0f, 0f, 0f, a * a);
    }
}
