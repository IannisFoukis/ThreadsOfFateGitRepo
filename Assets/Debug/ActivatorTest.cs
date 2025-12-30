using System.Collections;
using UnityEngine;

// Runtime test helper: spawns a shrine, projectile pool and an activator enemy, moves the enemy
// to the shrine and triggers activation so you can observe the shrine's bullet hell.
public class ActivatorTest : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject shrinePrefab;
    public GameObject projectilePoolPrefab;

    [Header("Positions")]
    public Vector3 shrinePosition = new Vector3(5f, 0f, 0f);

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float activationDistance = 0.5f;

    GameObject spawnedEnemy;
    GameObject spawnedShrine;

    void Start()
    {
        Debug.Log("[ACTIVATOR TEST] Starting activator test");

        // Ensure ProjectilePool exists
        if (ProjectilePool.Instance == null)
        {
            if (projectilePoolPrefab != null)
            {
                Instantiate(projectilePoolPrefab);
                Debug.Log("[ACTIVATOR TEST] Spawned ProjectilePool from prefab");
            }
            else
            {
                Debug.LogWarning("[ACTIVATOR TEST] No ProjectilePool instance and no prefab assigned. Shrine attacks will error.");
            }
        }

        // Spawn shrine
        if (shrinePrefab != null)
        {
            spawnedShrine = Instantiate(shrinePrefab, shrinePosition, Quaternion.identity);
        }
        else
        {
            spawnedShrine = new GameObject("TestShrine", typeof(Shrine), typeof(ShrineAttackController));
            spawnedShrine.transform.position = shrinePosition;
        }

        // Spawn enemy
        if (enemyPrefab != null)
        {
            spawnedEnemy = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            Debug.LogError("[ACTIVATOR TEST] No enemyPrefab assigned for test");
            return;
        }

        // Set role to Activator and apply role immediately
        var rc = spawnedEnemy.GetComponent<EnemyRoleController>();
        if (rc != null) rc.role = EnemyRole.Activator;

        var enemyComp = spawnedEnemy.GetComponent<Enemy>();
        if (enemyComp != null)
            enemyComp.ApplyRole(EnemyRole.Activator);

        // Assign shrine to ActivatorRunner if present
        var activator = spawnedEnemy.GetComponent<ActivatorRunner>();
        if (activator != null)
            activator.SetShrine(spawnedShrine.GetComponent<Shrine>());

        // Ensure Rigidbody2D exists for movement
        var rb = spawnedEnemy.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = spawnedEnemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        StartCoroutine(MoveToShrineRoutine());
    }

    IEnumerator MoveToShrineRoutine()
    {
        if (spawnedEnemy == null || spawnedShrine == null) yield break;

        var et = spawnedEnemy.transform;
        var st = spawnedShrine.transform;

        Debug.Log("[ACTIVATOR TEST] Moving enemy to shrine...");

        while (Vector2.Distance(et.position, st.position) > activationDistance)
        {
            et.position = Vector3.MoveTowards(et.position, st.position, moveSpeed * Time.deltaTime);
            yield return null;
        }

        Debug.Log("[ACTIVATOR TEST] Enemy reached shrine — triggering activation");

        var shrine = spawnedShrine.GetComponent<Shrine>();
        if (shrine != null)
        {
            shrine.ActivateByActivator();
            Debug.Log("[ACTIVATOR TEST] Shrine.ActivateByActivator called");
        }

        yield break;
    }
}
