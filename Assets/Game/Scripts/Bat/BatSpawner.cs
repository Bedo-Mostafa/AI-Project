using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BatSpawner : MonoBehaviour
{
    [SerializeField] private GameObject batPrefab;
    [SerializeField] private int batsPerSpawn = 2;
    [SerializeField] private float spawnInterval = 60f;
    [SerializeField] private int maxBats = 10;

    private BoxCollider spawnBox;
    private Transform player;

    public List<BatController> activeBats = new List<BatController>();

    private void Awake()
    {
        spawnBox = GetComponent<BoxCollider>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (activeBats.Count < maxBats)
                SpawnBats();
        }
    }

    private void SpawnBats()
    {
        for (int i = 0; i < batsPerSpawn; i++)
        {
            Vector3 spawnPos = GetRandomPointInBounds();

            GameObject batObj = Instantiate(batPrefab, spawnPos, Quaternion.identity);

            BatController bat = batObj.GetComponent<BatController>();

            if (bat != null)
            {
                bat.PlayerTarget = player;
                bat.WanderBounds = spawnBox;

                activeBats.Add(bat);
                if (activeBats.Count >= maxBats)
                {
                    break;
                }
            }
        }
    }

    private Vector3 GetRandomPointInBounds()
    {
        Bounds bounds = spawnBox.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(x, y, z);
    }
}