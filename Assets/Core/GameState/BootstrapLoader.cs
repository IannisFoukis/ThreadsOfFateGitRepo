using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    static bool loaded = false;

    void Start()
    {
        if (loaded)
            return;

        loaded = true;

        Debug.Log("[Bootstrap] Loading EntryRoom");

        SceneManager.LoadScene("Room_Entry");
    }
}
