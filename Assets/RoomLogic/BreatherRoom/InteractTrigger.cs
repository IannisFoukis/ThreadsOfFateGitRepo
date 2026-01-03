using UnityEngine;

public class InteractTrigger : MonoBehaviour
{
    BreatherInteraction interaction;
    bool playerInside;

    void Awake()
    {
        interaction = GetComponent<BreatherInteraction>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Debug.Log("[InteractTrigger] Player entered interaction trigger.");
        playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Debug.Log("[InteractTrigger] Player exited interaction trigger.");  
        playerInside = false;
    }

    void Update()
    {
        if (!playerInside) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            interaction?.Interact();
        }
    }
}
