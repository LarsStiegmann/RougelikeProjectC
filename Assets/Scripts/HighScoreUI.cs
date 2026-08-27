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

    public void Refresh()
    {
        if (nameTexts == null || scoreTexts == null)
        {
            return;
        }

        HighScoreController controller = HighScoreController.Instance;
        int scoreCount = 0;


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
