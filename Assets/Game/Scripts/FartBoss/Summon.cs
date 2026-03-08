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

    public override void OnStart()
    {
        Debug.Log("[Summon Action] OnStart triggered!");

        Holder prefabH = prefabHolder != null ? prefabHolder.GetComponent<Holder>() : null;
        Holder spawnH = spawnPointHolder != null ? spawnPointHolder.GetComponent<Holder>() : null;

        GameObject[] prefabs = prefabH != null ? prefabH.gameObjects : null;
        Transform[] spawnPoints = spawnH != null ? spawnH.transforms : null;

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[Summon Action] FAILED: No prefabs found in Prefab Holder!");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Summon Action] FAILED: No spawn points found in Spawn Point Holder!");
            return;
        }

        List<Transform> available = new List<Transform>(spawnPoints);
        Shuffle(available);

        int toSpawn = Mathf.Min(spawnCount, available.Count);
        Debug.Log($"[Summon Action] Successfully spawning {toSpawn} zombies.");

        for (int i = 0; i < toSpawn; i++)
        {
            Transform point = available[i];
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject.Instantiate(prefab, point.position, point.rotation);
        }
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
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