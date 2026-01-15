using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab; // MUST be prefab asset

    private GameObject playerInstance;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player Prefab is NOT assigned.");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (playerPrefab == null)
            return;

        // Create player if missing OR destroyed
        if (playerInstance == null)
        {
            playerInstance = Instantiate(playerPrefab);
            DontDestroyOnLoad(playerInstance);
            Debug.Log("[PlayerSpawner] Player instantiated");
        }

        var spawnPoint = PlayerSpawnPoint.Active;
        if (spawnPoint == null)
        {
            Debug.LogWarning("[PlayerSpawner] No PlayerSpawnPoint found in scene.");
            return;
        }

        playerInstance.transform.position = spawnPoint.transform.position;
        playerInstance.transform.rotation = spawnPoint.transform.rotation;

        Debug.Log("[PlayerSpawner] Player positioned at spawn point");
    }
}
