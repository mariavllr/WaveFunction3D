using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class WaveFunction3D2 : MonoBehaviour
{
    [SerializeField] private int iterations = 0;

    [Header("Map generation")]
    [SerializeField] private int dimensionsX, dimensionsZ, dimensionsY;
    [SerializeField] Tile3D2 floorTile;                     //Tile for the floor
    [SerializeField] Tile3D2 emptyTile;                     //Tile for the ceiling
    [SerializeField] private Tile3D2[] tileObjects;         //All the tiles that can be used to generate the map
    [SerializeField] int cellSize;

    [Header("Grid")]
    [SerializeField] private List<Cell3D2> gridComponents;   //A list with all the cells inside the grid
    [SerializeField] private Cell3D2 cellObj;                //They can be collapsed or not. Tiles are their children.

    [Header("Optimization")]
    [SerializeField] private bool useOptimization;
    [SerializeField] private bool inOrderGeneration;

    //Events
    public delegate void OnRegenerate();
    public static event OnRegenerate onRegenerate;
    Stopwatch stopwatch;

    void Awake()
    {
        ClearNeighbours(ref tileObjects);
        CreateRemainingCells(ref tileObjects);
        DefineNeighbourTiles(ref tileObjects, ref tileObjects);

        gridComponents = new List<Cell3D2>();
        stopwatch = new Stopwatch();

        stopwatch.Start();
        InitializeGrid();
        CreateSolidFloor();
        CreateSolidCeiling();
        UpdateGeneration();
    }

    /// <summary>
    /// Clears all the tiles' neighbours
    /// </summary>
    /// <param name="tiLeArray"></param> Array of tiles that need to be cleared
    private void ClearNeighbours(ref Tile3D2[] tileArray)
    {
        foreach (Tile3D2 tile in tileArray)
        {
            tile.upNeighbours.Clear();
            tile.rightNeighbours.Clear();
            tile.downNeighbours.Clear();
            tile.leftNeighbours.Clear();
            tile.aboveNeighbours.Clear();
            tile.belowNeighbours.Clear();
        }
    }

    /// <summary>
    /// Generates a new tile variation based on a given tile
    /// </summary>
    /// <param name="tile"></param> Tile to be used as base
    /// <param name="nameVariation"></param> Suffix added to the new tile variation
    private Tile3D2 CreateNewTileVariation(Tile3D2 tile, string nameVariation)
    {
        string name = tile.gameObject.name + nameVariation;
        GameObject newTile = new GameObject(name);
        newTile.gameObject.tag = tile.gameObject.tag;
        newTile.SetActive(false);
        newTile.hideFlags = HideFlags.HideInHierarchy;

        MeshFilter meshFilter = newTile.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = tile.gameObject.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer meshRenderer = newTile.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = tile.gameObject.GetComponent<MeshRenderer>().sharedMaterials;

        Tile3D2 tileRotated = newTile.AddComponent<Tile3D2>();
        tileRotated.tileType = tile.tileType;
        tileRotated.probability = tile.probability;
        tileRotated.positionOffset = tile.positionOffset;

        return tileRotated;
    }

    /// <summary>
    /// Generates the tile variations needed to get the full set of possible tiles
    /// based of the initial set of tiles
    /// </summary>
    /// <param name="tileArray"></param> Array of all pre-existing tiles
    private void CreateRemainingCells(ref Tile3D2[] tileArray)
    {
        List<Tile3D2> newTiles = new List<Tile3D2>();
        foreach (Tile3D2 tile in tileArray)
        {
            //Clockwise by default
            if (tile.rotateRight)
            {
                Tile3D2 tileRotated = CreateNewTileVariation(tile, "_RotateRight");
                RotateBorders90(tile, tileRotated);
                tileRotated.rotation = new Vector3(0f, 90f, 0f);
                newTiles.Add(tileRotated);
            }

            if (tile.rotate180)
            {
                Tile3D2 tileRotated = CreateNewTileVariation(tile, "_Rotate180");
                RotateBorders180(tile, tileRotated);
                tileRotated.rotation = new Vector3(0f, 180f, 0f);
                newTiles.Add(tileRotated);
            }

            if (tile.rotateLeft)
            {
                Tile3D2 tileRotated = CreateNewTileVariation(tile, "_RotateLeft");
                RotateBorders270(tile, tileRotated);
                tileRotated.rotation = new Vector3(0f, 270f, 0f);
                newTiles.Add(tileRotated);
            }
        }

        if (newTiles.Count != 0)
        {
            Tile3D2[] aux = tileArray.Concat(newTiles.ToArray()).ToArray();
            tileArray = aux;
        }
    }

    /// <summary>
    /// Updates the sockets and excluded neighbours of a tile that has been rotated 90 degrees
    /// </summary>
    /// <param name="originalTile"></param> Non-rotated tile
    /// <param name="tileRotated"></param> Rotated tile
    private void RotateBorders90(Tile3D2 originalTile, Tile3D2 tileRotated)
    {
        tileRotated.rightSocket = originalTile.upSocket;
        tileRotated.leftSocket = originalTile.downSocket;
        tileRotated.upSocket = originalTile.leftSocket;
        tileRotated.downSocket = originalTile.rightSocket;

        tileRotated.aboveSocket = originalTile.aboveSocket;
        tileRotated.aboveSocket.rotationIndex = 90;
        tileRotated.belowSocket = originalTile.belowSocket;
        tileRotated.belowSocket.rotationIndex = 90;

        //excluded neighbours
        tileRotated.excludedNeighboursRight = originalTile.excludedNeighboursUp;
        tileRotated.excludedNeighboursLeft = originalTile.excludedNeighboursDown;
        tileRotated.excludedNeighboursUp = originalTile.excludedNeighboursLeft;
        tileRotated.excludedNeighboursDown = originalTile.excludedNeighboursRight;
    }

    /// <summary>
    /// Updates the sockets and excluded neighbours of a tile that has been rotated 180 degrees
    /// </summary>
    /// <param name="originalTile"></param> Non-rotated tile
    /// <param name="tileRotated"></param> Rotated tile
    private void RotateBorders180(Tile3D2 originalTile, Tile3D2 tileRotated)
    {
        tileRotated.rightSocket = originalTile.leftSocket;
        tileRotated.leftSocket = originalTile.rightSocket;
        tileRotated.upSocket = originalTile.downSocket;
        tileRotated.downSocket = originalTile.upSocket;
        tileRotated.aboveSocket = originalTile.aboveSocket;
        tileRotated.aboveSocket.rotationIndex = 180;
        tileRotated.belowSocket = originalTile.belowSocket;
        tileRotated.belowSocket.rotationIndex = 180;

        //excluded neighbours
        tileRotated.excludedNeighboursLeft = originalTile.excludedNeighboursRight;
        tileRotated.excludedNeighboursRight = originalTile.excludedNeighboursLeft;
        tileRotated.excludedNeighboursUp = originalTile.excludedNeighboursDown;
        tileRotated.excludedNeighboursDown = originalTile.excludedNeighboursUp;
    }

    /// <summary>
    /// Updates the sockets and excluded neighbours of a tile that has been rotated 270 degrees
    /// </summary>
    /// <param name="originalTile"></param> Non-rotated tile
    /// <param name="tileRotated"></param> Rotated tile
    private void RotateBorders270(Tile3D2 originalTile, Tile3D2 tileRotated) //O rotar a la izquierda
    {
        tileRotated.rightSocket = originalTile.downSocket;
        tileRotated.leftSocket = originalTile.upSocket;
        tileRotated.upSocket = originalTile.rightSocket;
        tileRotated.downSocket = originalTile.leftSocket;
        tileRotated.aboveSocket = originalTile.aboveSocket;
        tileRotated.aboveSocket.rotationIndex = 270;
        tileRotated.belowSocket = originalTile.belowSocket;
        tileRotated.belowSocket.rotationIndex = 270;

        //excluded neighbours
        tileRotated.excludedNeighboursRight = originalTile.excludedNeighboursDown;
        tileRotated.excludedNeighboursLeft = originalTile.excludedNeighboursUp;
        tileRotated.excludedNeighboursUp = originalTile.excludedNeighboursRight;
        tileRotated.excludedNeighboursDown = originalTile.excludedNeighboursLeft;
    }


    /// <summary>
    /// Defines the neighbour tiles of each tile in the array
    /// </summary>
    /// <param name="tileArray"></param> Array of tiles
    /// <param name="otherTileArray"></param> Array of tiles to compare with
    public void DefineNeighbourTiles(ref Tile3D2[] tileArray, ref Tile3D2[] otherTileArray)
    {
        foreach (Tile3D2 tile in tileArray)
        {
            foreach (Tile3D2 otherTile in otherTileArray)
            {
                //HORIZONTAL FACES: Same socket and be symmetric OR one flip and the other not
                //It also checks f the excluded list of each face does not include the other tile, and vice versa

                //Vecinos de arriba
                if (otherTile.downSocket.socket_name == tile.upSocket.socket_name && !tile.excludedNeighboursUp.Contains(otherTile.tileType) && !otherTile.excludedNeighboursDown.Contains(tile.tileType))
                {
                    if(tile.upSocket.isSymmetric || otherTile.downSocket.isSymmetric || (otherTile.downSocket.isFlipped && !tile.upSocket.isFlipped) || (!otherTile.downSocket.isFlipped && tile.upSocket.isFlipped))
                    tile.upNeighbours.Add(otherTile);
                }
                //Vecinos de abajo
                if (otherTile.upSocket.socket_name == tile.downSocket.socket_name && !tile.excludedNeighboursDown.Contains(otherTile.tileType) && !otherTile.excludedNeighboursUp.Contains(tile.tileType))
                {
                    if (otherTile.upSocket.isSymmetric || tile.downSocket.isSymmetric || (otherTile.upSocket.isFlipped && !tile.downSocket.isFlipped) || (!otherTile.upSocket.isFlipped && tile.downSocket.isFlipped))
                    tile.downNeighbours.Add(otherTile);
                }
                //Vecinos a la derecha
                if (otherTile.leftSocket.socket_name == tile.rightSocket.socket_name  && !tile.excludedNeighboursRight.Contains(otherTile.tileType) && !otherTile.excludedNeighboursLeft.Contains(tile.tileType))
                {
                    if (otherTile.leftSocket.isSymmetric || tile.rightSocket.isSymmetric || (otherTile.leftSocket.isFlipped && !tile.rightSocket.isFlipped) || (!otherTile.leftSocket.isFlipped && tile.rightSocket.isFlipped))
                    tile.rightNeighbours.Add(otherTile);
                }
                //Vecinos a la izquierda
                if (otherTile.rightSocket.socket_name == tile.leftSocket.socket_name && !tile.excludedNeighboursLeft.Contains(otherTile.tileType) && !otherTile.excludedNeighboursRight.Contains(tile.tileType))
                {
                    if (otherTile.rightSocket.isSymmetric || tile.leftSocket.isSymmetric || (otherTile.rightSocket.isFlipped && !tile.leftSocket.isFlipped) || (!otherTile.rightSocket.isFlipped && tile.leftSocket.isFlipped))
                    tile.leftNeighbours.Add(otherTile);
                }

                //VERTICAL FACES: Ambos deben ser rotacionalmente invariables O ambos deben tener el mismo indice de rotacion

                //Vecinos debajo
                if (otherTile.belowSocket.socket_name == tile.aboveSocket.socket_name)
                {
                    if((otherTile.belowSocket.rotationallyInvariant && tile.aboveSocket.rotationallyInvariant) || (otherTile.belowSocket.rotationIndex == tile.aboveSocket.rotationIndex))
                    tile.aboveNeighbours.Add(otherTile);
                }

                //Vecinos encima
                if (otherTile.aboveSocket.socket_name == tile.belowSocket.socket_name)
                {
                    if ((otherTile.aboveSocket.rotationallyInvariant && tile.belowSocket.rotationallyInvariant) || (otherTile.aboveSocket.rotationIndex == tile.belowSocket.rotationIndex))
                    tile.belowNeighbours.Add(otherTile);
                }
            }
        }
    }


    //---------CREATE THE GRID WITH CELLS-------------

    void InitializeGrid()
    {
        for (int y = 0; y < dimensionsY; y++)
        {
            for (int z = 0; z < dimensionsZ; z++)
            {
                for (int x = 0; x < dimensionsX; x++)
                {
                    Cell3D2 newCell = Instantiate(cellObj, new Vector3(x*cellSize, y * cellSize, z*cellSize), Quaternion.identity, gameObject.transform);
                    newCell.CreateCell(false, tileObjects, x + (z * dimensionsX) + (y * dimensionsX * dimensionsZ));
                    gridComponents.Add(newCell);
                }
            }
        }
    }

    //--------CREATE A SOLID FLOOR ON THE FIRST PLANT-----------

    void CreateSolidFloor()
    {
        int y = 0;
        for (int z = 0; z < dimensionsZ; z++)
        {
            for (int x = 0; x < dimensionsX; x++)
            {
                var index = x + (z * dimensionsX) + (y * dimensionsX * dimensionsZ);
                Cell3D2 cellToCollapse = gridComponents[index];
                cellToCollapse.tileOptions = new Tile3D2[] { floorTile };
                cellToCollapse.collapsed = true;
                if (cellToCollapse.transform.childCount != 0)
                {
                    foreach (Transform child in cellToCollapse.transform)
                    {
                        Destroy(child.gameObject);
                    }
                }

                Tile3D2 instantiatedTile = Instantiate(floorTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
                if (instantiatedTile.rotation != Vector3.zero)
                {
                    instantiatedTile.gameObject.transform.Rotate(floorTile.rotation, Space.Self);
                }

                instantiatedTile.gameObject.transform.position += instantiatedTile.positionOffset;
                instantiatedTile.gameObject.SetActive(true);
                iterations++;
            }
        }
    }

    void CreateSolidCeiling()
    {
        int y = dimensionsY-1;
        for (int z = 0; z < dimensionsZ; z++)
        {
            for (int x = 0; x < dimensionsX; x++)
            {
                var index = x + (z * dimensionsX) + (y * dimensionsX * dimensionsZ);
                Cell3D2 cellToCollapse = gridComponents[index];
                cellToCollapse.tileOptions = new Tile3D2[] { emptyTile };
                cellToCollapse.collapsed = true;
                if (cellToCollapse.transform.childCount != 0)
                {
                    foreach (Transform child in cellToCollapse.transform)
                    {
                        Destroy(child.gameObject);
                    }
                }

                Tile3D2 instantiatedTile = Instantiate(emptyTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
                if (instantiatedTile.rotation != Vector3.zero)
                {
                    instantiatedTile.gameObject.transform.Rotate(floorTile.rotation, Space.Self);
                }

                instantiatedTile.gameObject.transform.position += instantiatedTile.positionOffset;
                instantiatedTile.gameObject.SetActive(true);
                iterations++;
            }
        }
    }


    //--------GENERATE THE REST OF THE MAP-----------
    IEnumerator CheckEntropy()
    {
        List<Cell3D2> tempGrid = new List<Cell3D2>(gridComponents);

        tempGrid.RemoveAll(c => c.collapsed);


        //------------Para que elija el que tiene menos entropia-----------------
        //The result of this calculation determines the order of the elements in the sorted list.
        //If the result is negative, it means a should come before b; if positive, it means a should come after b;
        //and if zero, their order remains unchanged.
        if (!inOrderGeneration)
        {
            tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });


            //Dejar solo las celdas que tengan el menor número de posibilidades
            int arrLength = tempGrid[0].tileOptions.Length;
            int stopIndex = default;

            for (int i = 1; i < tempGrid.Count; i++)
            {
                if (tempGrid[i].tileOptions.Length > arrLength)
                {
                    stopIndex = i;
                    break;
                }
            }

            if (stopIndex > 0)
            {
                tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);
            }
        }

        yield return new WaitForSeconds(0f);

        //Para que vaya en orden, dejar solo esto
        CollapseCell(tempGrid);
    }

    void CollapseCell(List<Cell3D2> tempGrid)
    {
        Cell3D2 cellToCollapse;

        if (!inOrderGeneration)  cellToCollapse = tempGrid[Random.Range(0, tempGrid.Count)]; //Para que escoja una random
        else
        {
            //Para que vaya en orden
            cellToCollapse = tempGrid[0];
        }

        cellToCollapse.collapsed = true;

        //Hacer "visitables" los alrededores
        if (useOptimization) GetNeighboursCloseToCollapsedCell(cellToCollapse);

        //Si es la capa superior, comprobar exclusiones y eliminarlas
        /*if ((cellToCollapse.index / (dimensionsX * dimensionsZ)) == dimensionsY - 1)
        {
            cellToCollapse.tileOptions = cellToCollapse.tileOptions.Where(tile => !tile.excludeInTopLayer).ToArray();
        }*/

        //Elegir una tile para esa celda
        List<(Tile3D2 tile, int weight)> weightedTiles = cellToCollapse.tileOptions.Select(tile => (tile, tile.probability)).ToList();
        Tile3D2 selectedTile = ChooseTile(weightedTiles);

        if (selectedTile is null)
        {
            Debug.LogError("INCOMPATIBILITY!");
           // if (iterations > 20)
           // {
           //     BackTrackingHandler(cellToCollapse); //esto no va mucho
           //     UpdateGeneration();
           // }
           // else
           // {
                Regenerate();
           // }
            return;
        }

        cellToCollapse.tileOptions = new Tile3D2[] { selectedTile };
        Tile3D2 foundTile = cellToCollapse.tileOptions[0];

        if (cellToCollapse.transform.childCount != 0)
        {
            foreach (Transform child in cellToCollapse.transform)
            {
                Destroy(child.gameObject);
            }
        }

        Tile3D2 instantiatedTile = Instantiate(foundTile, cellToCollapse.transform.position, Quaternion.identity, cellToCollapse.transform);
        if (instantiatedTile.rotation != Vector3.zero)
        {
            instantiatedTile.gameObject.transform.Rotate(foundTile.rotation, Space.Self);
        }

        instantiatedTile.gameObject.transform.position += instantiatedTile.positionOffset;
        instantiatedTile.gameObject.SetActive(true);

        // CheckExtras(foundTile, cellToCollapse.transform);

        UpdateGeneration();
    }

    //This method choose what cells should be checked given a distance. This is for OPTIMIZATION (not always looking at every cell)
    private void GetNeighboursCloseToCollapsedCell(Cell3D2 cell)
    {
        int up, down, left, right, above, below;
        up = cell.index + dimensionsX;
        down = cell.index - dimensionsX;
        left = cell.index - 1;
        right = cell.index + 1;
        above = cell.index + (dimensionsX * dimensionsZ);
        below = cell.index - (dimensionsX * dimensionsZ);

        cell.visitable = true;

        //UP. no esta al final de una columna (en el eje z).
        if (((cell.index / dimensionsX) % dimensionsZ) != dimensionsZ - 1)
        {
            gridComponents[up].MakeVisitable();
        }

        //DOWN. no esta al comienzo de una columna (en el eje z).
        if (((cell.index / dimensionsX) % dimensionsZ) != 0)
        {
            gridComponents[down].MakeVisitable();
        }

        //LEFT. no esta al comienzo de una fila.
        if (cell.index % dimensionsX != 0)
        {
            gridComponents[left].MakeVisitable();
        }

        //RIGHT. no esta al final de una fila
        if ((cell.index + 1) % dimensionsX != 0)
        {
            gridComponents[right].MakeVisitable();
        }

        //ABOVE. No esta en la capa superior en y
        if ((cell.index / (dimensionsX * dimensionsZ)) != dimensionsY - 1)
        {
            gridComponents[right].MakeVisitable();
        }

        //BELOW. No esta en la capa inferior en y
        if ((cell.index / (dimensionsX * dimensionsZ)) != 0)
        {
            gridComponents[left].MakeVisitable();
        }

        //Además, también hay que comprobar las DIAGONALES para completar el contorno

        // Diagonales en 2D
        int upLeft = up - 1;    // Arriba e izquierda
        int upRight = up + 1;   // Arriba y derecha
        int downLeft = down - 1; // Abajo e izquierda
        int downRight = down + 1; // Abajo y derecha

        // Diagonales en 3D
        int aboveUp = above + dimensionsX;    // Arriba en Z y en Y
        int aboveDown = above - dimensionsX;  // Abajo en Z y en Y
        int belowUp = below + dimensionsX;    // Arriba en Z y por debajo en Y
        int belowDown = below - dimensionsX;  // Abajo en Z y por debajo en Y

        // Esquinas (diagonales)
        if (((cell.index / dimensionsX) % dimensionsZ) != dimensionsZ - 1 && cell.index % dimensionsX != 0)
        {
            gridComponents[upLeft].MakeVisitable(); // Up-Left
        }

        if (((cell.index / dimensionsX) % dimensionsZ) != dimensionsZ - 1 && (cell.index + 1) % dimensionsX != 0)
        {
            gridComponents[upRight].MakeVisitable(); // Up-Right
        }

        if (((cell.index / dimensionsX) % dimensionsZ) != 0 && cell.index % dimensionsX != 0)
        {
            gridComponents[downLeft].MakeVisitable(); // Down-Left
        }

        if (((cell.index / dimensionsX) % dimensionsZ) != 0 && (cell.index + 1) % dimensionsX != 0)
        {
            gridComponents[downRight].MakeVisitable(); // Down-Right
        }

        // Esquinas en 3D
        if ((cell.index / (dimensionsX * dimensionsZ)) != dimensionsY - 1)
        {
            if (((cell.index / dimensionsX) % dimensionsZ) != dimensionsZ - 1)
            {
                gridComponents[aboveUp].MakeVisitable(); // Above-Up
            }

            if (((cell.index / dimensionsX) % dimensionsZ) != 0)
            {
                gridComponents[aboveDown].MakeVisitable(); // Above-Down
            }
        }

        if ((cell.index / (dimensionsX * dimensionsZ)) != 0)
        {
            if (((cell.index / dimensionsX) % dimensionsZ) != dimensionsZ - 1)
            {
                gridComponents[belowUp].MakeVisitable(); // Below-Up
            }

            if (((cell.index / dimensionsX) % dimensionsZ) != 0)
            {
                gridComponents[belowDown].MakeVisitable(); // Below-Down
            }
        }

    }


    //-----NO FUNCIONA AHORA MISMO------
    /* void BackTrackingHandler(Cell3D errorCell)
     {
         Debug.Log("BACKTRACKING!");
         //Vamos a descolapsar un cuadrado alrededor del error

         int centerIndex = errorCell.index;
         gridComponents[centerIndex].collapsed = false;

         //verificar vecino derecha
         if ((centerIndex % dimensionsX) < (dimensionsX - 1))
         {
             Cell3D rightCell = gridComponents[centerIndex + 1];
             if (rightCell.collapsed)
             {
                 rightCell.collapsed = false;
                 rightCell.tileOptions = tileObjects;
                 Destroy(rightCell.transform.GetChild(0).gameObject);
                 iterations--;
             }
         }
         //vecino izquierda
         if ((centerIndex % dimensionsX) > 0)
         {
             Cell3D leftCell = gridComponents[centerIndex - 1];
             if (leftCell.collapsed)
             {
                 leftCell.collapsed = false;
                 leftCell.tileOptions = tileObjects;
                 Destroy(leftCell.transform.GetChild(0).gameObject);
                 iterations--;
             }

         }
         //vecino arriba
         if ((centerIndex / dimensionsX) % dimensionsZ < (dimensionsZ - 1))
         {
             Cell3D upCell = gridComponents[centerIndex + dimensionsX];

             if (upCell.collapsed)
             {
                 upCell.collapsed = false;
                 upCell.tileOptions = tileObjects;
                 Destroy(upCell.transform.GetChild(0).gameObject);
                 iterations--;
             }

         }
         //vecino abajo
         if ((centerIndex / dimensionsX) % dimensionsZ > 0)
         {
             Cell3D downCell = gridComponents[centerIndex - dimensionsX];
             if (downCell.collapsed)
             {
                 downCell.collapsed = false;
                 downCell.tileOptions = tileObjects;
                 Destroy(downCell.transform.GetChild(0).gameObject);
                 iterations--;
             }

         }

     }*/

    /*   void CheckExtras(Tile3D foundTile, Transform transform)
       {
           if(foundTile.gameObject.CompareTag("Hierba"))
           {
               float rand = UnityEngine.Random.Range(0, 100);

               if (rand > extrasDensity)
               {
                   return;
               }
               else
               {
                   int randomExtra = UnityEngine.Random.Range(0, extraObjects.Length);
                   Instantiate(extraObjects[randomExtra], transform.position, Quaternion.identity, transform);
               }

           }
       }*/

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
            if (randomNumber < weight)
                return tile;
            randomNumber -= weight;
        }
        return null; // This should not happen if the list is not empty
    }

    void UpdateGeneration()
    {
        List<Cell3D2> newGenerationCell = new List<Cell3D2>(gridComponents);


        for (int y = 0; y < dimensionsY; y++)
        {
            for (int z = 0; z < dimensionsZ; z++)
            {
                for (int x = 0; x < dimensionsX; x++)
                {
                    CheckNeighbours(x, y, z, ref newGenerationCell);
                }
            }

        }

        gridComponents = newGenerationCell;

        iterations++;
        if (iterations <= dimensionsX * dimensionsZ * dimensionsY)
        {
            StartCoroutine(CheckEntropy());
        }
        else
        {
            //FIN
            stopwatch.Stop();
            print($"Ha tardado {stopwatch.ElapsedMilliseconds} ms en acabar ({stopwatch.ElapsedMilliseconds/1000} s)");
        }

    }

    //This method looks and update the options in every cell of the given list looking at the neighbours
    void CheckNeighbours(int x, int y, int z, ref List<Cell3D2> newGenerationCell)
    {
        int up, down, left, right, above, below;
        var index = x + (z * dimensionsX) + (y * dimensionsX * dimensionsZ);
        right = (x + 1) + (z * dimensionsX) + (y * dimensionsX * dimensionsZ);
        left = (x - 1) + (z * dimensionsX) + (y * dimensionsX * dimensionsZ);
        up = x + ((z + 1) * dimensionsX) + (y * dimensionsX * dimensionsZ);
        down = x + ((z - 1) * dimensionsX) + (y * dimensionsX * dimensionsZ);
        above = x + (z * dimensionsX) + ((y + 1) * dimensionsX * dimensionsZ);
        below = x + (z * dimensionsX) + ((y - 1) * dimensionsX * dimensionsZ);

        if (gridComponents[index].collapsed || (!gridComponents[index].visitable && useOptimization))
        {
            newGenerationCell[index] = gridComponents[index];
        }

        else
        {
            gridComponents[index].haSidoVisitado = true;
            List<Tile3D2> options = new List<Tile3D2>();
            foreach (Tile3D2 t in tileObjects)
            {
                options.Add(t);
            }

            //Mira la celda de abajo
            if (z > 0)
            {
                List<Tile3D2> validOptions = new List<Tile3D2>();
                foreach (Tile3D2 possibleOptions in gridComponents[down].tileOptions)
                {
                    var valid = possibleOptions.upNeighbours;
                    validOptions = validOptions.Concat(valid).ToList();
                }
                CheckValidity(options, validOptions);

            }

            //Mirar la celda derecha
            if (x < dimensionsX - 1)
            {
                List<Tile3D2> validOptions = new List<Tile3D2>();
                foreach (Tile3D2 possibleOptions in gridComponents[right].tileOptions)
                {
                    var valid = possibleOptions.leftNeighbours;
                    validOptions = validOptions.Concat(valid).ToList();
                }

                CheckValidity(options, validOptions);
            }



            //Mira la celda de arriba
            if (z < dimensionsZ - 1)
            {
                List<Tile3D2> validOptions = new List<Tile3D2>();

                foreach (Tile3D2 possibleOptions in gridComponents[up].tileOptions)
                {


                    var valid = possibleOptions.downNeighbours;
                    validOptions = validOptions.Concat(valid).ToList();
                }

                CheckValidity(options, validOptions);

            }


            //Mirar la celda izquierda
            if (x > 0)
            {
                List<Tile3D2> validOptions = new List<Tile3D2>();

                foreach (Tile3D2 possibleOptions in gridComponents[left].tileOptions)
                {

                    var valid = possibleOptions.rightNeighbours;
                    validOptions = validOptions.Concat(valid).ToList();
                }

                CheckValidity(options, validOptions);

            }


            //Mirar la celda de debajo
            if (y > 0)
            {
                List<Tile3D2> validOptions = new List<Tile3D2>();

                foreach (Tile3D2 possibleOptions in gridComponents[below].tileOptions)
                {
                    var valid = possibleOptions.aboveNeighbours;
                    validOptions = validOptions.Concat(valid).ToList();
                }

                CheckValidity(options, validOptions);

            }

            //Mirar la celda de encima
            if (y < dimensionsY - 1)
            {
                List<Tile3D2> validOptions = new List<Tile3D2>();

                foreach (Tile3D2 possibleOptions in gridComponents[above].tileOptions)
                {

                    var valid = possibleOptions.belowNeighbours;
                    validOptions = validOptions.Concat(valid).ToList();
                }

                CheckValidity(options, validOptions);

            }

            Tile3D2[] newTileList = new Tile3D2[options.Count];

            for (int i = 0; i < options.Count; i++)
            {
                newTileList[i] = options[i];
            }

            newGenerationCell[index].RecreateCell(newTileList);
        }
    }

    void CheckValidity(List<Tile3D2> optionList, List<Tile3D2> validOption)
    {
        for (int x = optionList.Count - 1; x >= 0; x--)
        {
            var element = optionList[x];
            if (!validOption.Contains(element))
            {
                optionList.RemoveAt(x);
            }
        }
    }

    public void Regenerate()
    {
        if (onRegenerate != null)
        {
            onRegenerate();
        }
        StopAllCoroutines();
        //Borrar todas las celdas
        for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }

        iterations = 0;
        gridComponents.Clear();

        stopwatch.Reset();
        stopwatch.Start();

        InitializeGrid();
        CreateSolidFloor();
        CreateSolidCeiling();
        UpdateGeneration();
    }
}
