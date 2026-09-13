using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PersistentDebris : MonoBehaviour
{
    private static PersistentDebris instance;

    [Tooltip("How many separate death piles to keep. The oldest is removed when exceeded. Set to 0 for unlimited.")]
    [SerializeField] private int maxPiles = 5;

    [Tooltip("Scenes that are NOT gameplay. Loading one of these clears all debris.")]
    [SerializeField] private string[] nonGameplayScenes = { "MainMenu" };

    private readonly List<GameObject> piles = new List<GameObject>();

    public static PersistentDebris GetOrCreate()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<PersistentDebris>();
        }

        if (instance == null)
        {
            GameObject go = new GameObject("PersistentDebris");
            instance = go.AddComponent<PersistentDebris>();
        }

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }


    public Transform CreatePile()
    {
        GameObject pile = new GameObject("DebrisPile");
        DontDestroyOnLoad(pile);

        piles.Add(pile);
        TrimOldPiles();

        return pile.transform;
    }


    public void SettleWhenAsleep(Rigidbody rb)
    {
        if (rb != null)
        {
            StartCoroutine(SettleRoutine(rb));
        }
    }

    private IEnumerator SettleRoutine(Rigidbody rb)
    {
        float elapsed = 0f;
        const float timeout = 12f;

        while (rb != null && elapsed < timeout && !rb.IsSleeping())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void TrimOldPiles()
    {
        piles.RemoveAll(p => p == null);

        if (maxPiles <= 0)
        {
            return;
        }

        while (piles.Count > maxPiles)
        {
            GameObject oldest = piles[0];
            piles.RemoveAt(0);
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < nonGameplayScenes.Length; i++)
        {
            if (scene.name == nonGameplayScenes[i])
            {
                ClearAll();
                return;
            }
        }
    }

    public void ClearAll()
    {
        for (int i = 0; i < piles.Count; i++)
        {
            if (piles[i] != null)
            {
                Destroy(piles[i]);
            }
        }
        piles.Clear();
    }
}
