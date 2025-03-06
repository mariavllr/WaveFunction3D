using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class CardGenerator : MonoBehaviour
{
    [SerializeField] List<Tile3D> tilesList;
    public Queue<Tile3D> tileQueue;
    public int queueSize;
    public float distance;
    private float offset;

    private void Start()
    {
        tileQueue = new Queue<Tile3D>();
        offset = 0;

        InicializeTileQueue();
    }

    private void OnEnable()
    {
        DragObject.OnTileReleased += OnTileRemoved; //  Suscribimos el evento
    }

    private void OnDestroy()
    {
     //   DragObject.OnTileReleased -= OnTileRemoved; //  Desuscribimos para evitar errores
    }

    private void InicializeTileQueue()
    {
        int index;
        Tile3D tileToEnqueue;


        for (int i = 0; i < queueSize; i++)
        {
            index = UnityEngine.Random.Range(0, tilesList.Count);
            tileToEnqueue = tilesList[index];
            

            Vector3 offsetVector = new Vector3(0, offset, 0);
            Tile3D instantiatedTile = Instantiate(tileToEnqueue, transform.position - offsetVector,  Quaternion.identity, transform);

            tileQueue.Enqueue(instantiatedTile);
            offset += distance;
        }

        tileQueue.First().gameObject.AddComponent<DragObject>();

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintStack();
        }
    }

   private void OnTileRemoved(GameObject removedTile)
    {
        Destroy(removedTile.GetComponent<DragObject>());
    }


    private void PrintStack()
    {
        print("PRINTING QUEUE:");
        foreach (Tile3D tile in tileQueue)
        {
            print(tile.name);
        }
    }
}
