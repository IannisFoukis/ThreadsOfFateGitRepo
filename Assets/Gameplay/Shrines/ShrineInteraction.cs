using UnityEngine;

public class ShrineInteraction : MonoBehaviour
{
    private Shrine shrine;
    private bool playerInRange;

    void Awake()
    {
        shrine = GetComponent<Shrine>();

        if (shrine == null)
            Debug.LogError("[ShrineInteraction] Shrine component missing");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        Debug.Log("[SHRINE] Player approaches the shrine");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange) return;

        // E = Destroy (absence)
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[SHRINE] Player chooses to DESTROY the shrine");
            shrine.ResolveDestroyed();
            ResolveAndDisable();
        }

        // Q = Endure (pressure)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("[SHRINE] Player chooses to ENDURE the shrine");
            shrine.ResolveEndured();
            ResolveAndDisable();
        }
    }

    void ResolveAndDisable()
    {
        // Visually / mechanically silence the shrine
        shrine.gameObject.SetActive(false);
        enabled = false;
    }
}
