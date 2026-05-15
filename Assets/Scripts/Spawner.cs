using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("NPC Spawns")]
    [SerializeField] GameObject[] toSpawn;
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] float spawnYPos;
    [SerializeField] float spwanXPos;
    [SerializeField] float spawnZPos;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating(nameof(NpcSpawn), spawnInterval, spawnInterval);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void NpcSpawn()
    {
        float randomX = Random.Range(-spwanXPos, spwanXPos);
        float randomY = Random.Range(-spawnYPos, spawnYPos);
        float randomZ = Random.Range(-spawnZPos, spawnZPos);
        Vector3 RandomSpawnPos = transform.position + new Vector3(randomX, randomY, randomZ);
        int randomIndex = Random.Range(0, toSpawn.Length);
        Instantiate(toSpawn[Random.Range(0, toSpawn.Length)], RandomSpawnPos, transform.rotation, transform);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(spwanXPos * 2f, spawnYPos * 2f, spawnZPos * 2f));
    }
}
