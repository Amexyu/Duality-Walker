using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject[] objPrefabs;
    [SerializeField] private float interval = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Spawn());
    }

    private IEnumerator Spawn()
    {
        for(int i =0;i< 20; i++)
        {
            Vector2 position = new Vector2(Random.Range(-10, 10), Random.Range(-10, 10));

            GameObject obj = objPrefabs[Random.Range(0, objPrefabs.Length)];
            Instantiate(obj, position, Quaternion.identity, transform);

            yield return new WaitForSeconds(interval);
        }
        
    }
}
