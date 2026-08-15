using UnityEngine;

public class StatCounter : MonoBehaviour
{
    public static StatCounter Instance { get; private set; }

    public int kills { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddKill()
    {
        kills++;
    }
}
