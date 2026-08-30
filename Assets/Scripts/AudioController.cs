using Unity.VisualScripting;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [SerializeField] private AudioSource soundObject;

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //_________________________________________________________________________________________________________
    //Quelle: Sasquatch B Studios auf Youtube
    //Titel: How To Add Sound Effects the RIGHT Way | Unity Tutorial
    //URL: https://www.youtube.com/watch?v=DU7cgVsU2rM
    //Datum: 08.12.2022

    public void PlayRandomAudio(AudioClip[] audioClips, Transform spawnTransform, float volume, bool is3D)
    {
        int rand = Random.Range(0, audioClips.Length);

        AudioSource audioSource = Instantiate(soundObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClips[rand];

        audioSource.volume = volume;

        audioSource.spatialBlend = is3D ? 1f : 0f;

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    //_________________________________________________________________________________________________________
}
