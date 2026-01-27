using UnityEngine;

// ==================================================
// SCRIPT ROLE: INPUT GATE
// SYSTEM: Rooms / Interaction
// RESPONSIBILITY: Player interaction with Keeper
// ==================================================

public class KeeperInteraction : MonoBehaviour
{
    private KeeperRoom keeperRoom;
    private bool playerInside = false;

    void Awake()
    {
        keeperRoom = GetComponent<KeeperRoom>();
    }

    void Update()
    {
        if (!playerInside) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[KEEPER] Player acknowledges the Keeper");
            playerInside = false;   // ← add this
            enabled = false;        // ← and this
            keeperRoom.ConcludeKeeperRoom();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        Debug.Log("[KEEPER] Player approaches");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
    }
}
