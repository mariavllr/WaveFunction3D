using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;
using System;
using UnityEngine.Tilemaps;
public class CardGenerator : MonoBehaviour
{
    [SerializeField] public List<Tile3D2> tilesList;
    public Queue<Tile3D2> tileQueue;
    public int queueSize;
    public float distance;
    private float offset = 0;

    public static event Action<Vector3, Tile3D2> OnTileRotated;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RotateTile();
        }
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
        tileToEnqueue.gameObject.SetActive(true);

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

        //EFECTO REBOTE
        float delayBetweenBounces = 0.1f;
        int index = 0;

        foreach (Tile3D2 tile in tileQueue)
        {
            float delay = index * delayBetweenBounces;

            tile.transform
                .DOJump(tile.transform.position, jumpPower: 0.25f, numJumps: 1, duration: 0.3f)
                .SetEase(Ease.InOutFlash)
                .SetDelay(delay);

            index++;
        }
    }

    private void MoveUpQueue()
    {

        foreach (Tile3D2 tile in tileQueue)
        {
            tile.transform.position += new Vector3(0, distance, 0);
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

    //Cuando rote, queremos que busque su tile rotada en la tile list. Siempre rotará +90 grados.
    private void RotateTile()
    {
        Tile3D2 actualTile = tileQueue.First();
        string tileName = actualTile.name;

        //Dividimos entre el nombre de la tile y su rotacion
        string currentTileType = actualTile.tileType;
        float currentRotation = actualTile.rotation.y;

        // Calcular nueva rotación
        float newRotation = (currentRotation + 90) % 360;

        // Buscar la nueva tile en la lista
        Tile3D2 newTile = tilesList.Find(tile => tile.tileType == currentTileType && tile.rotation.y == newRotation);

        if (newTile != null)
        {
            //Ya tenemos la tile rotada. Hay que sustituirla
            actualTile.name = newTile.name;
            actualTile.tileType = newTile.tileType;
            actualTile.probability = newTile.probability;
            actualTile.rotation = newTile.rotation;

            actualTile.upNeighbours = newTile.upNeighbours;
            actualTile.rightNeighbours = newTile.rightNeighbours;
            actualTile.downNeighbours = newTile.downNeighbours;
            actualTile.leftNeighbours = newTile.leftNeighbours;
            actualTile.aboveNeighbours = newTile.aboveNeighbours;
            actualTile.belowNeighbours = newTile.belowNeighbours;

            actualTile.gameObject.transform.Rotate(new Vector3(0, 90, 0), Space.Self);

        }
        else
        {
            Debug.LogError($"ROTATING TILE: Tile with name {newTile.name} not found.");
        }



        OnTileRotated?.Invoke(actualTile.rotation, actualTile);
    }

    private void OnDeleteTile()
    {
        tileQueue.Dequeue();

        MoveUpQueue();
        EnqueueTile();
        tileQueue.First().gameObject.AddComponent<DragObject>();
    }

    //DEBUG
    private void PrintStack()
    {
        print("PRINTING QUEUE:");
        foreach (Tile3D2 tile in tileQueue)
        {
            print(tile.name);
        }
    }
}
