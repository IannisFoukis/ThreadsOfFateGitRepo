using UnityEngine;
using System.Collections.Generic;

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;
    public RoomDoctrineConfig doctrine;

    private List<EnemyAgent> agents = new List<EnemyAgent>();

    void Start()
    {
        ResolvePlayer();
    }

    void Update()
    {
        // Keep trying to find player if missing
        if (playerTransform == null)
        {
            ResolvePlayer();
        }

        // If still null, nothing can be coordinated
        if (playerTransform == null)
            return;

        // Optional debug info every second
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Coordinator] Registered enemies: {agents.Count}");
        }
    }

    private void ResolvePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");

        if (go != null)
        {
            playerTransform = go.transform;
            Debug.Log("[Coordinator] Player found and set: " + go.name);
        }
    }

    public void Register(EnemyAgent agent)
    {
        if (!agents.Contains(agent))
        {
            agents.Add(agent);
            AssignSlot(agent);
        }
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);
    }

    public List<EnemyAgent> GetEnemies()
    {
        return new List<EnemyAgent>(agents);
    }

    void AssignSlot(EnemyAgent agent)
    {
        agent.assignedSlot = (EnemySlotType)(agents.Count - 1);
    }

    public Vector3 GetWorldPositionFor(EnemyAgent agent)
    {
        // Safety: if no player yet, stay where you are
        if (playerTransform == null)
            return agent.transform.position;

        Vector3 center = playerTransform.position;

        int index = agent.assignedSlot.HasValue
            ? (int)agent.assignedSlot.Value
            : 0;

        // Fixed simple ring formation for now
        float angle = index * 25f * Mathf.Deg2Rad;
        float radius = 2.2f;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle),
            Mathf.Sin(angle),
            0
        ) * radius;

        return center + offset;
    }

    void OnDrawGizmos()
    {
        if (playerTransform == null)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < agents.Count; i++)
        {
            float angle = i * 25f * Mathf.Deg2Rad;
            float radius = 2.2f;

            Vector3 pos = playerTransform.position + new Vector3(
                Mathf.Cos(angle),
                Mathf.Sin(angle),
                0
            ) * radius;

            Gizmos.DrawSphere(pos, 0.3f);
        }
    }
}
