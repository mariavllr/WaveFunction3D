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
            // Clockwise by default
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
    private void RotateBorders270(Tile3D2 originalTile, Tile3D2 tileRotated)
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
                // HORIZONTAL FACES: Same socket and be symmetric OR one flip and the other not
                // It also checks f the excluded list of each face does not include the other tile, and vice versa

                // Up neighbours
                if (otherTile.downSocket.socket_name == tile.upSocket.socket_name
                    && !tile.excludedNeighboursUp.Contains(otherTile.tileType)
                    && !otherTile.excludedNeighboursDown.Contains(tile.tileType))
                {
                    if (tile.upSocket.isSymmetric || otherTile.downSocket.isSymmetric
                    || (otherTile.downSocket.isFlipped && !tile.upSocket.isFlipped)
                    || (!otherTile.downSocket.isFlipped && tile.upSocket.isFlipped))
                    tile.upNeighbours.Add(otherTile);
                }
                // Down neighbours
                if (otherTile.upSocket.socket_name == tile.downSocket.socket_name
                    && !tile.excludedNeighboursDown.Contains(otherTile.tileType)
                    && !otherTile.excludedNeighboursUp.Contains(tile.tileType))
                {
                    if (otherTile.upSocket.isSymmetric || tile.downSocket.isSymmetric
                    || (otherTile.upSocket.isFlipped && !tile.downSocket.isFlipped)
                    || (!otherTile.upSocket.isFlipped && tile.downSocket.isFlipped))
                    tile.downNeighbours.Add(otherTile);
                }
                // Right neighbours
                if (otherTile.leftSocket.socket_name == tile.rightSocket.socket_name
                    && !tile.excludedNeighboursRight.Contains(otherTile.tileType)
                    && !otherTile.excludedNeighboursLeft.Contains(tile.tileType))
                {
                    if (otherTile.leftSocket.isSymmetric || tile.rightSocket.isSymmetric
                    || (otherTile.leftSocket.isFlipped && !tile.rightSocket.isFlipped)
                    || (!otherTile.leftSocket.isFlipped && tile.rightSocket.isFlipped))
                    tile.rightNeighbours.Add(otherTile);
                }
                // Left neighbours
                if (otherTile.rightSocket.socket_name == tile.leftSocket.socket_name
                    && !tile.excludedNeighboursLeft.Contains(otherTile.tileType)
                    && !otherTile.excludedNeighboursRight.Contains(tile.tileType))
                {
                    if (otherTile.rightSocket.isSymmetric || tile.leftSocket.isSymmetric
                        || (otherTile.rightSocket.isFlipped && !tile.leftSocket.isFlipped)
                        || (!otherTile.rightSocket.isFlipped && tile.leftSocket.isFlipped))
                    tile.leftNeighbours.Add(otherTile);
                }

                // VERTICAL FACES: both faces must have invariable rotation or the same rotation index

                // Below neighbours
                if (otherTile.belowSocket.socket_name == tile.aboveSocket.socket_name)
                {
                    if((otherTile.belowSocket.rotationallyInvariant
                        && tile.aboveSocket.rotationallyInvariant)
                        || (otherTile.belowSocket.rotationIndex == tile.aboveSocket.rotationIndex))
                    tile.aboveNeighbours.Add(otherTile);
                }

                // Above neighbours
                if (otherTile.aboveSocket.socket_name == tile.belowSocket.socket_name)
                {
                    if ((otherTile.aboveSocket.rotationallyInvariant
                        && tile.belowSocket.rotationallyInvariant)
                        || (otherTile.aboveSocket.rotationIndex == tile.belowSocket.rotationIndex))
                    tile.belowNeighbours.Add(otherTile);
                }
            }
        }
    }

    /// <summary>
    /// Creates the grid full of cells
    /// </summary>
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

    /// <summary>
    /// Fills the first layer of the map with a solid tile to avoid empty spaces
    /// </summary>
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

    /// <summary>
    /// Fills the last layer of the map with a solid tile to avoid empty spaces
    /// </summary>
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


    /// <summary>
    /// Reorders the grid based on the entropy of the cells, collapsing the one with less entropy
    /// </summary>
    IEnumerator CheckEntropy()
    {
        List<Cell3D2> tempGrid = new List<Cell3D2>(gridComponents);

        tempGrid.RemoveAll(c => c.collapsed);


        //------------This is done to ensure that the cell with less entropy is selected-----------------
        // The result of this calculation determines the order of the elements in the sorted list.
        // If the result is negative, it means a should come before b; if positive, it means a should come after b;
        // and if zero, their order remains unchanged.
        int stopIndex = default;
        if (!inOrderGeneration)
        {
            tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });

            // Removes all the cells with more options than the first one
            // This is done to ensure that only the cells with less entropy are selected
            int arrLength = tempGrid[0].tileOptions.Length;

            for (int i = 1; i < tempGrid.Count; i++)
            {
                if (tempGrid[i].tileOptions.Length > arrLength)
                {
                    stopIndex = i;
                    break;
                }
            }
        }

        yield return new WaitForSeconds(0f); // Debugging purposes

        CollapseCell(ref tempGrid, stopIndex);
    }

    /// <summary>
    /// Collapses a cell and updates the grid
    /// </summary>
    /// <param name="tempGrid"></param>
    /// <param name="stopIndex"></param>
    void CollapseCell(ref List<Cell3D2> tempGrid, int stopIndex)
    {
        Cell3D2 cellToCollapse;

        // If non-ordered generation, select a random cell, if not, select the first one
        if (!inOrderGeneration)  cellToCollapse = tempGrid[Random.Range(0, stopIndex)];
        else cellToCollapse = tempGrid[0];

        cellToCollapse.collapsed = true;

        // Make the neighbours of the collapsed cell visitable for optimization purposes
        if (useOptimization) GetNeighboursCloseToCollapsedCell(cellToCollapse);

        // Choose a tile for that cell
        List<(Tile3D2 tile, int weight)> weightedTiles = cellToCollapse.tileOptions.Select(tile => (tile, tile.probability)).ToList();
        Tile3D2 selectedTile = ChooseTile(weightedTiles);

        if (selectedTile is null)
        {
            Debug.LogError("INCOMPATIBILITY!");
            Regenerate();
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

        UpdateGeneration();
    }

    /// <summary>
    /// Makes the neighbours wiithin a given distance og the collapsed cell visitable for optimization purposes
    /// (not always looking at every cell)
    /// </summary>
    /// <param name="cell"></param> Collapsed cell
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

        // UP. not at the end of a column (on z axis).
        if (((cell.index / dimensionsX) % dimensionsZ) != dimensionsZ - 1)
        {
            gridComponents[up].MakeVisitable();
        }

        // DOWN. not at the start of a column (on z axis).
        if (((cell.index / dimensionsX) % dimensionsZ) != 0)
        {
            gridComponents[down].MakeVisitable();
        }

        // LEFT. not at the start of a row
        if (cell.index % dimensionsX != 0)
        {
            gridComponents[left].MakeVisitable();
        }

        // RIGHT. not at the end of a row
        if ((cell.index + 1) % dimensionsX != 0)
        {
            gridComponents[right].MakeVisitable();
        }

        // ABOVE. Not at the top layer in y
        if ((cell.index / (dimensionsX * dimensionsZ)) != dimensionsY - 1)
        {
            gridComponents[right].MakeVisitable();
        }

        // BELOW. Not at the bottom layer in y
        if ((cell.index / (dimensionsX * dimensionsZ)) != 0)
        {
            gridComponents[left].MakeVisitable();
        }

        // Diagonal neighbours must be checked to complete the contour

        // Diagonals in 2D
        int upLeft = up - 1;    // Up and left
        int upRight = up + 1;   // Up and right
        int downLeft = down - 1; // Down and left
        int downRight = down + 1; // Down and right

        // Diagonales en 3D
        int aboveUp = above + dimensionsX;    // Above in Z and Y
        int aboveDown = above - dimensionsX;  // Below in Z and Y
        int belowUp = below + dimensionsX;    // Above in Z and below in Y
        int belowDown = below - dimensionsX;  // Abajo in Z and below in Y

        // Corners (diagonals)
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

        // Corners in 3D
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

    /// <summary>
    /// Chooses a tile based on the weights of the tiles
    /// </summary>
    /// <param name="weightedTiles"></param> List of tiles with their corresponding weights
    /// <returns></returns> The chosen tile
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

    /// <summary>
    /// Updates all the cells in the grid
    /// </summary>
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
            //END
            stopwatch.Stop();
            print($"Ha tardado {stopwatch.ElapsedMilliseconds} ms en acabar ({stopwatch.ElapsedMilliseconds/1000} s)");
        }

    }

    /// <summary>
    /// looks and update the options in every cell of the given list looking at the neighbours
    /// </summary>
    /// <param name="x"></param> x coordinate of the cell
    /// <param name="y"></param> y coordinate of the cell
    /// <param name="z"></param> z coordinate of the cell
    /// <param name="newGenerationCell"></param> List of cells to be updated
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

            // Checks the down cell
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

            // Checks the right cell
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

            // Checks the up cell
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


            // Checks the left cell
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


            // Cheecks the cell below
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

            // Checks the cell above
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

    /// <summary>
    /// Removes all the options from the optionList that are not in the validOption list
    /// </summary>
    /// <param name="optionList"></param> List of options to be checked
    /// <param name="validOption"></param> List of valid options
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

    /// <summary>
    /// Regenerates the map
    /// </summary>
    public void Regenerate()
    {
        if (onRegenerate != null)
        {
            onRegenerate();
        }

        StopAllCoroutines();

        // Clear the grid
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