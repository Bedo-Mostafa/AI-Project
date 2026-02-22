using System.Collections.Generic;
using UnityEngine;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("FartBoss/Summon")]
public class Summon : GOAction
{
    [InParam("Prefab Holder")] public GameObject prefabHolder;
    [InParam("Spawn Point Holder")] public GameObject spawnPointHolder;
    [InParam("Spawn Count")] public int spawnCount = 3;
    [InParam("Respawn Delay")] public float respawnDelay = 2f;

    private GameObject[] prefabs;
    private Transform[] spawnPoints;

    // Track each spawned object and its spawn point
    private List<SpawnEntry> spawnedEntries = new List<SpawnEntry>();

    private class SpawnEntry
    {
        public GameObject instance;
        public Transform spawnPoint;
        public float respawnTimer;   
        public bool waitingRespawn;
    }

    public override void OnStart()
    {
        spawnedEntries.Clear();

        // Grab arrays from the Holder scripts
        Holder prefabH = prefabHolder != null ? prefabHolder.GetComponent<Holder>() : null;
        Holder spawnH = spawnPointHolder != null ? spawnPointHolder.GetComponent<Holder>() : null;

        prefabs = prefabH != null ? prefabH.gameObjects : null;
        spawnPoints = spawnH != null ? spawnH.transforms : null;

        if (prefabs == null || prefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        List<Transform> available = new List<Transform>(spawnPoints);
        Shuffle(available);

        int toSpawn = Mathf.Min(spawnCount, available.Count);

        for (int i = 0; i < toSpawn; i++)
        {
            Transform point = available[i];
            GameObject instance = SpawnRandom(point);
            spawnedEntries.Add(new SpawnEntry
            {
                instance = instance,
                spawnPoint = point,
                respawnTimer = 0f,
                waitingRespawn = false
            });
        }
    }

    public override TaskStatus OnUpdate()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            SpawnEntry entry = spawnedEntries[i];

            if (entry.waitingRespawn)
            {
                // Count down the respawn timer
                entry.respawnTimer -= Time.deltaTime;
                if (entry.respawnTimer <= 0f)
                {
                    entry.instance = SpawnRandom(entry.spawnPoint);
                    entry.waitingRespawn = false;
                }
            }
            else if (entry.instance == null)
            {
                // Just detected it was destroyed — start the timer
                entry.waitingRespawn = true;
                entry.respawnTimer = respawnDelay;
            }
        }

        return TaskStatus.RUNNING;
    }

    // public override void OnAbort()
    // {
    //     // Clean up all spawned objects
    //     foreach (SpawnEntry entry in spawnedEntries)
    //     {
    //         if (entry.instance != null)
    //             GameObject.Destroy(entry.instance);
    //     }
    //     spawnedEntries.Clear();
    // }

    private GameObject SpawnRandom(Transform point)
    {
        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        return GameObject.Instantiate(prefab, point.position, point.rotation);
    }

    private void Shuffle(List<Transform> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
