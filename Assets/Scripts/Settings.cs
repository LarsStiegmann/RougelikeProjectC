using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class Settings : MonoBehaviour
{
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider controllerSensitivitySlider;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider soundFXVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    [SerializeField] private TMP_Text mouseSensitivityValueText;
    [SerializeField] private TMP_Text controllerSensitivityValueText;
    [SerializeField] private TMP_Text masterVolumeValueText;
    [SerializeField] private TMP_Text soundFXVolumeValueText;
    [SerializeField] private TMP_Text musicVolumeValueText;

    [SerializeField] private AudioMixer audioMixer;

    private void Start()
    {
        mouseSensitivitySlider.value = SaveSystem.Instance.mouseSensitivity;
        controllerSensitivitySlider.value = SaveSystem.Instance.controllerSensitivity;
        masterVolumeSlider.value = SaveSystem.Instance.masterVolume;
        soundFXVolumeSlider.value = SaveSystem.Instance.soundFXVolume;
        musicVolumeSlider.value = SaveSystem.Instance.musicVolume;


        mouseSensitivityValueText.text = mouseSensitivitySlider.value.ToString("0.##");
        controllerSensitivityValueText.text = Mathf.RoundToInt(controllerSensitivitySlider.value).ToString();
        masterVolumeValueText.text = masterVolumeSlider.value.ToString("0.##");
        soundFXVolumeValueText.text = soundFXVolumeSlider.value.ToString("0.##");
        musicVolumeValueText.text = musicVolumeSlider.value.ToString("0.##");
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
        controllerSensitivityValueText.text = Mathf.RoundToInt(value).ToString();
        SaveSystem.Instance.Save();
    }

    public void OnMasterVolumeChanged(float value)
    {
        SaveSystem.Instance.masterVolume = value;
        masterVolumeValueText.text = value.ToString("0.##");

        audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) *20);

        SaveSystem.Instance.Save();
    }

    public void OnSoundFXVolumeChanged(float value)
    {
        SaveSystem.Instance.soundFXVolume = value;
        soundFXVolumeValueText.text = value.ToString("0.##");

        audioMixer.SetFloat("SoundFXVolume", Mathf.Log10(value) * 20);

        SaveSystem.Instance.Save();
    }

    public void OnMusicVolumeChanged(float value)
    {
        SaveSystem.Instance.musicVolume = value;
        musicVolumeValueText.text = value.ToString("0.##");

        audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);

        SaveSystem.Instance.Save();
    }
}
