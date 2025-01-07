using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathSpawner : MonoBehaviour
{
    public GameObject pathPrefab;   // Prefab del camino
    public int numPaths = 7;        // Número de segmentos activos
    public float pathLength = 10f; // Longitud de cada segmento

    private Queue<GameObject> pathsQueue = new Queue<GameObject>();

    void Start()
    {
        // Crear y posicionar los segmentos iniciales
        for (int i = 0; i < numPaths; i++)
        {
            Vector3 spawnPosition = new Vector3(0, 0, (i - 1) * pathLength); // -1 asegura una tile detrás del jugador
            GameObject path = Instantiate(pathPrefab, spawnPosition, Quaternion.identity);
            pathsQueue.Enqueue(path);
        }
    }

    void Update()
    {
        GameObject firstPath = pathsQueue.Peek();
        GameObject lastPath = pathsQueue.Last();

        // Verificar si la primera tile está demasiado lejos detrás del jugador
        if (firstPath.transform.position.z + pathLength < Camera.main.transform.position.z - pathLength)
        {
            // Reubicar el primer segmento al frente del último
            pathsQueue.Dequeue();

            Vector3 newSpawnPosition = lastPath.transform.position + new Vector3(0, 0, pathLength);
            firstPath.transform.position = newSpawnPosition;

            pathsQueue.Enqueue(firstPath);
        }
    }
}
