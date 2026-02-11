using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int amount = 50;

    void Awake()
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-20, 20),
                0,
                Random.Range(5, 45)
            );

            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }
}
