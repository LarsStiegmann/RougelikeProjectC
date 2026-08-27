using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider controllerSensitivitySlider;
    [SerializeField] private Slider volumeSlider;

    [SerializeField] private TMP_Text mouseSensitivityValueText;
    [SerializeField] private TMP_Text controllerSensitivityValueText;
    [SerializeField] private TMP_Text volumeValueText;

    private void Start()
    {
        mouseSensitivitySlider.value = SaveSystem.Instance.mouseSensitivity;
        controllerSensitivitySlider.value = SaveSystem.Instance.controllerSensitivity;
        volumeSlider.value = SaveSystem.Instance.volume;

        mouseSensitivityValueText.text = mouseSensitivitySlider.value.ToString();
        controllerSensitivityValueText.text = controllerSensitivitySlider.value.ToString();
        volumeValueText.text = volumeSlider.value.ToString();
    }

    public void OnMouseSensitivityChanged(float value)
    {
        SaveSystem.Instance.mouseSensitivity = value;
        mouseSensitivityValueText.text = value.ToString("0.##");
        SaveSystem.Instance.Save();
    }

    public void OnControllerSensitivityChanged(float value)
    {
        SaveSystem.Instance.controllerSensitivity = value;
        controllerSensitivityValueText.text = value.ToString("0.##");
        SaveSystem.Instance.Save();
    }

    public void OnVolumeChanged(float value)
    {
        SaveSystem.Instance.volume = value;
        volumeValueText.text = value.ToString("0.##");

        AudioListener.volume = value;

        SaveSystem.Instance.Save();
    }
}
