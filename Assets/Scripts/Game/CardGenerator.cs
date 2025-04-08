using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class CardGenerator : MonoBehaviour
{
    [SerializeField] public List<Tile3D2> tilesList;
    public Queue<Tile3D2> tileQueue;
    public int queueSize;
    public float distance;
    private float offset = 0;

    private void Start()
    {
        tileQueue = new Queue<Tile3D2>();
        InicializeTileQueue();
    }

    private void OnEnable()
    {
        DragObject.OnTileReleased += OnTileRemoved; //  Suscribimos el evento
        DeleteTile.OnDeleteTile += OnDeleteTile;
    }

    private void OnDestroy()
    {
        DragObject.OnTileReleased -= OnTileRemoved; //  Desuscribimos para evitar errores
    }

    private void InicializeTileQueue()
    {  
        for (int i = 0; i < queueSize; i++)
        {
            EnqueueTile();
        }

        tileQueue.First().gameObject.AddComponent<DragObject>();
    }

    private Tile3D2 GetRandomTile()
    {
        //De manera random completamente
        //return tilesList[Random.Range(0, tilesList.Count)];

        //Con pesos
        // Choose a tile for that cell
        List<(Tile3D2 tile, int weight)> weightedTiles = tilesList.Select(tile => (tile, tile.probability)).ToList();
        return ChooseTile(weightedTiles);
    }

    Tile3D2 ChooseTile(List<(Tile3D2 tile, int weight)> weightedTiles)
    {
        // Calculate the total weight
        int totalWeight = weightedTiles.Sum(item => item.weight);

        // Generate a random number between 0 and totalWeight - 1
        System.Random random = new System.Random();
        int randomNumber = random.Next(0, totalWeight);

        // Iterate through the tiles and find the one corresponding to the random number
        foreach (var (tile, weight) in weightedTiles)
        {
            if (randomNumber < weight) return tile;
            randomNumber -= weight;
        }
        return null; // This should not happen if the list is not empty
    }

    private void EnqueueTile()
    {
        Tile3D2 tileToEnqueue = GetRandomTile();

        // Si la cola no está vacía, colocar la nueva tile debajo de la última
        Vector3 newTilePosition;
        if (tileQueue.Count > 0)
        {
            Tile3D2 lastTile = tileQueue.Last();
            newTilePosition = lastTile.transform.position - new Vector3(0, distance, 0);
        }
        else
        {
            // Si la cola está vacía, colocarla en la posición base
            newTilePosition = transform.position;
        }

        Tile3D2 instantiatedTile = Instantiate(tileToEnqueue, newTilePosition, Quaternion.identity, transform);
        if (instantiatedTile.rotation != Vector3.zero)
        {
            instantiatedTile.gameObject.transform.Rotate(instantiatedTile.rotation, Space.Self);
        }
        tileQueue.Enqueue(instantiatedTile);


        //TO DO: Que salgan solo tiles que se puedan colocar, o por lo menos con probabilidades para que no salga todo el rato esquinas y caminos
    }

    private void MoveUpQueue()
    {

        foreach (Tile3D2 tile in tileQueue)
        {
            tile.transform.position += new Vector3(0, distance, 0);
        }
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
       // Destroy(removedTile.GetComponent<DragObject>());
        tileQueue.Dequeue();

        MoveUpQueue();
        EnqueueTile();
        tileQueue.First().gameObject.AddComponent<DragObject>();
    }

    private void OnDeleteTile()
    {
        tileQueue.Dequeue();

        MoveUpQueue();
        EnqueueTile();
        tileQueue.First().gameObject.AddComponent<DragObject>();
    }


    private void PrintStack()
    {
        print("PRINTING QUEUE:");
        foreach (Tile3D2 tile in tileQueue)
        {
            print(tile.name);
        }
    }
}
