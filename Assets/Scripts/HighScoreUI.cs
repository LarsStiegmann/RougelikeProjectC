using UnityEngine;
using TMPro;

public class HighScoreUI : MonoBehaviour
{

    [SerializeField] private TMP_Text[] nameTexts;
    [SerializeField] private TMP_Text[] scoreTexts;


    private void Start()
    {
        Debug.Log(HighScoreController.Instance.Highscores[0].playerName);

        for (int i = 0; i < HighScoreController.Instance.Highscores.Count; i++)
        {
            nameTexts[i].text = HighScoreController.Instance.Highscores[i].playerName;
            scoreTexts[i].text = HighScoreController.Instance.Highscores[i].score.ToString();
        }
    }
}
