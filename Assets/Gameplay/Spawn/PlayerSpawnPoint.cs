using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    public static PlayerSpawnPoint Active;

    private void OnEnable()
    {
        Active = this;
        Debug.Log("[PlayerSpawnPoint] Active spawn point set");
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }
}
