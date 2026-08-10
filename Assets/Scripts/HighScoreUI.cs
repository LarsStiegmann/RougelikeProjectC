using UnityEngine;
using TMPro;

public class HighScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text[] nameTexts;
    [SerializeField] private TMP_Text[] scoreTexts;

    private void Start()
    {
        Refresh();
    }

    /// <summary>
    /// Fills the board from the current highscores. Rows beyond the number of
    /// scores are blanked, so leftover placeholder text is not left on screen.
    /// </summary>
    public void Refresh()
    {
        if (nameTexts == null || scoreTexts == null)
        {
            return;
        }

        HighScoreController controller = HighScoreController.Instance;
        int scoreCount = 0;

        // The controller lives in this scene, but guard anyway so entering play from
        // another scene cannot throw here.
        if (controller != null && controller.Highscores != null)
        {
            scoreCount = controller.Highscores.Count;
        }

        int rows = Mathf.Min(nameTexts.Length, scoreTexts.Length);

        for (int i = 0; i < rows; i++)
        {
            bool hasEntry = i < scoreCount;

            if (nameTexts[i] != null)
            {
                nameTexts[i].text = hasEntry ? controller.Highscores[i].playerName : string.Empty;
            }

            if (scoreTexts[i] != null)
            {
                scoreTexts[i].text = hasEntry ? controller.Highscores[i].score.ToString() : string.Empty;
            }
        }
    }
}
