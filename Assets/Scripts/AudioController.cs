using Unity.VisualScripting;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    private static AudioController instance;

    /// <summary>
    /// Finds the scene instance, or creates one on the fly so that scenes started
    /// directly in the editor (without going through the MainMenu) still have audio.
    /// An auto-created controller has no soundObject prefab; PlayRandomAudio falls
    /// back to building a bare AudioSource in that case.
    /// </summary>
    public static AudioController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioController>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject("AudioController (auto)");
                instance = go.AddComponent<AudioController>();
            }

            return instance;
        }
    }

    [SerializeField] private AudioSource soundObject;

    private void Awake()
    {
        // Compare against the backing field, not the property: the property getter
        // would find this very component and then destroy it as a "duplicate".
        if (instance == null || instance == this)
        {
            instance = this;
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
        if (audioClips == null || audioClips.Length == 0 || spawnTransform == null)
        {
            return;
        }

        int rand = Random.Range(0, audioClips.Length);
        if (audioClips[rand] == null)
        {
            return;
        }

        AudioSource audioSource;
        if (soundObject != null)
        {
            audioSource = Instantiate(soundObject, spawnTransform.position, Quaternion.identity);
        }
        else
        {
            // Auto-created controller without the prefab: a bare source still plays,
            // it just skips the mixer routing.
            GameObject fx = new GameObject("SoundFX (auto)");
            fx.transform.position = spawnTransform.position;
            audioSource = fx.AddComponent<AudioSource>();
        }

        audioSource.clip = audioClips[rand];

        audioSource.volume = volume;

        audioSource.spatialBlend = is3D ? 1f : 0f;

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    //_________________________________________________________________________________________________________
}
