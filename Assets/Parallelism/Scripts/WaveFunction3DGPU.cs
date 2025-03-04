using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Diagnostics;
using System;
using UnityEditor;
using System.Reflection;

public class WaveFunction3DGPU : MonoBehaviour
{
    [SerializeField] public const int MAX_NEIGHBOURS = 44;

    [Header("Shader")]
    [SerializeField] private ComputeShader shader;

    [Header("Map generation")]
    [SerializeField] int cellSize;
    [SerializeField] private int dimensionsX, dimensionsZ, dimensionsY;
    [SerializeField] Tile3D2 floorTile;                     // Tile for the floor
    [SerializeField] Tile3D2 emptyTile;                     // Tile for the ceiling
    [SerializeField] private Tile3D2[] tileObjects;         // All the tiles that can be used to generate the map

    [Header("Grid")]
    [SerializeField] private List<Cell3D2> gridComponents;   // A list with all the cells inside the grid
    [SerializeField] private Cell3D2 cellObj;                // They can be collapsed or not. Tiles are their children.

    // Events
    public delegate void OnRegenerate();
    public static event OnRegenerate onRegenerate;
    Stopwatch stopwatch;

    // Structs for the shader
    unsafe struct Cell3DStruct
    {
        public uint colapsed;
        // Number of tiles that can be placed in the cell
        // (array lenghts are fixed we can't use .lenght)
        public uint entropy;
        /* Possible tiles
        |-------------------------------------------------------------------------------|
        | The possible tiles are stored in a uint array, each uint containing the index |
        | of a tile in the tileObjects array.                                           |
        |-------------------------------------------------------------------------------|
        */
        public fixed int tileOptions[MAX_NEIGHBOURS];
    };

    unsafe struct Tile3DStruct
    {
        /*
        |-------------------------------------------------------------------------------|
        | In order to be able to send data to the buffer, all the data within the struct|
        | must be blitable, that means that the size in memory for c# is exactly the    |
        | the same as in HLSL, for uint arrays we only need to ensure that they have    |
        | the a fixed size.                                                             |
        |-------------------------------------------------------------------------------|
        */
        public int probability;
        public Vector3 rotation;

        // Neighbours (these are the indexes of the tiles in the tileObjects array)
        public fixed int upNeighbours[MAX_NEIGHBOURS];
        public fixed int rightNeighbours[MAX_NEIGHBOURS];
        public fixed int downNeighbours[MAX_NEIGHBOURS];
        public fixed int leftNeighbours[MAX_NEIGHBOURS];
        public fixed int aboveNeighbors[MAX_NEIGHBOURS];
        public fixed int belowNeighbours[MAX_NEIGHBOURS];
    };

    unsafe void Start()
    {
        ClearNeighbours(ref tileObjects);
        CreateRemainingCells(ref tileObjects);
        DefineNeighbourTiles(ref tileObjects, ref tileObjects);

        gridComponents = new List<Cell3D2>();
        stopwatch = new Stopwatch();

        stopwatch.Start();
        InitializeGrid();

        // Create the structs
        Tile3DStruct[] tileObjectsStructs = CreateTile3DStructs();
        Cell3DStruct[] gridComponentsStructs = CreateCell3DStructs();
        CreateSolidFloor(gridComponentsStructs);
        CreateEmptyCeiling(gridComponentsStructs);

        // Initialize buffers
        ComputeBuffer tileObjectsBuffer = new ComputeBuffer(tileObjectsStructs.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Tile3DStruct)));
        ComputeBuffer outputBuffer = new ComputeBuffer(gridComponentsStructs.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Cell3DStruct)));
        ComputeBuffer stateBuffer = new ComputeBuffer(1, sizeof(int));

        // Set data
        tileObjectsBuffer.SetData(tileObjectsStructs);

        // Data to buffers
        shader.SetBuffer(0, "tileObjects", tileObjectsBuffer);
        shader.SetBuffer(0, "output", outputBuffer);
        shader.SetBuffer(0, "state", stateBuffer);
        shader.SetInt("MAX_NEIGHBOURS", MAX_NEIGHBOURS);
        shader.SetInt("gridDimensionsX", dimensionsX);
        shader.SetInt("gridDimensionsY", dimensionsY);
        shader.SetInt("gridDimensionsZ", dimensionsZ);
        shader.SetInt("floorTile", Array.IndexOf(tileObjects, floorTile));
        shader.SetInt("emptyTile", Array.IndexOf(tileObjects, emptyTile));
        shader.SetVector("offset", new Vector3(0, 1, 0));

        // Loop until the grid is fully collapsed without any incomatibilities
        int[] incompatibilities = {1};
        int attempts = 1;

        while(incompatibilities[0] != 0)
        {
            // Reset buffers
            outputBuffer.SetData(gridComponentsStructs);
            stateBuffer.SetData(new int[] { 0 });

            // Set different seed between attemps
            shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * attempts);

            // Dispatch 1/5 of the grid
            shader.Dispatch(shader.FindKernel("CSMain"), dimensionsX / 6 + (dimensionsX % 6), 1, dimensionsZ / 6 + (dimensionsZ % 6));

            // Dispatch 2/5 of the grid
            shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode());
            shader.SetVector("offset", new Vector3(1, 1, 1));
            shader.Dispatch(shader.FindKernel("CSMain"), dimensionsX / 6 + (dimensionsX % 6), 1, dimensionsZ / 6 + (dimensionsZ % 6));

            // Dispatch 3/5 of the grid
            shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * 3);
            shader.SetVector("offset", new Vector3(-1, 1, 1));
            shader.Dispatch(shader.FindKernel("CSMain"), dimensionsX / 6 + (dimensionsX % 6), 1, dimensionsZ / 6 + (dimensionsZ % 6));

            // Dispatch 4/5 of the grid
            shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * 4);
            shader.SetVector("offset", new Vector3(-1, 1, -1));
            shader.Dispatch(shader.FindKernel("CSMain"), dimensionsX / 6 + (dimensionsX % 6), 1, dimensionsZ / 6 + (dimensionsZ % 6));

            // Dispatch 5/5 of the grid
            shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * 5);
            shader.SetVector("offset", new Vector3(1, 1, -1));
            shader.Dispatch(shader.FindKernel("CSMain"), dimensionsX / 6 + (dimensionsX % 6), 1, dimensionsZ / 6 + (dimensionsZ % 6));

            stateBuffer.GetData(incompatibilities);
            attempts++;
        }

        // Get data
        outputBuffer.GetData(gridComponentsStructs);

        // Recreate the grid based on the data received by the shader
        for (int i = 0; i < gridComponentsStructs.Length; i++)
        {
            if(gridComponentsStructs[i].colapsed == 0) continue; // Testing
            Cell3D2 cell = gridComponents[i];
            cell.name = "Cell " + i;
            cell.collapsed = gridComponentsStructs[i].colapsed == 1;
            //cell.RecreateCell(tileObjects[gridComponentsStructs[i].tileOptions[0]]);

            // Uncomment this to recreate the cell with all the possible tiles
            List<Tile3D2> newOptions = new List<Tile3D2>();
            for (int j = 0; j < MAX_NEIGHBOURS; j++)
            {
                if (gridComponentsStructs[i].tileOptions[j] != -1) newOptions.Add(tileObjects[gridComponentsStructs[i].tileOptions[j]]);
            }
            cell.RecreateCell(newOptions.ToArray());

            if (cell.transform.childCount != 0)
            {
                foreach (Transform child in cell.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            Tile3D2 instantiatedTile = Instantiate(cell.tileOptions[0], cell.transform.position, Quaternion.identity, cell.transform);
            if (instantiatedTile.rotation != Vector3.zero)
            {
                instantiatedTile.gameObject.transform.Rotate(cell.tileOptions[0].rotation, Space.Self);
            }

            instantiatedTile.gameObject.transform.position += instantiatedTile.positionOffset;
            instantiatedTile.gameObject.SetActive(true);
        }

        // Release memory buffers to avoid leaks
        tileObjectsBuffer.Release();
        outputBuffer.Release();
        stopwatch.Stop();
        Debug.Log("Time elapsed: " + stopwatch.ElapsedMilliseconds + "ms");
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
    /// Creates the Tile3DStructs needed for the shader
    /// </summary>
    /// <returns></returns> Array of Tile3DStructs
    unsafe private Tile3DStruct[] CreateTile3DStructs()
    {
        Tile3DStruct[] tileStructs = new Tile3DStruct[tileObjects.Length];

        for(int i = 0; i < tileObjects.Length; i++)
        {
            Tile3DStruct tileStruct = new Tile3DStruct();
            tileStruct.probability = tileObjects[i].probability;
            tileStruct.rotation = tileObjects[i].rotation;

            // Initialize neighbours
            for (int j = 0; j < MAX_NEIGHBOURS; j++)
            {
                tileStruct.upNeighbours[j] = -1;
                tileStruct.rightNeighbours[j] = -1;
                tileStruct.downNeighbours[j] = -1;
                tileStruct.leftNeighbours[j] = -1;
                tileStruct.aboveNeighbors[j] = -1;
                tileStruct.belowNeighbours[j] = -1;
            }

            // Copy neighbours (transforming them to indexes)
            for (int j = 0; j < tileObjects[i].upNeighbours.Count; j++)
            {
                tileStruct.upNeighbours[j] = Array.IndexOf(tileObjects, tileObjects[i].upNeighbours[j]);
            }

            for (int j = 0; j < tileObjects[i].rightNeighbours.Count; j++)
            {
                tileStruct.rightNeighbours[j] = Array.IndexOf(tileObjects, tileObjects[i].rightNeighbours[j]);
            }

            for (int j = 0; j < tileObjects[i].downNeighbours.Count; j++)
            {
                tileStruct.downNeighbours[j] = Array.IndexOf(tileObjects, tileObjects[i].downNeighbours[j]);
            }

            for (int j = 0; j < tileObjects[i].leftNeighbours.Count; j++)
            {
                tileStruct.leftNeighbours[j] = Array.IndexOf(tileObjects, tileObjects[i].leftNeighbours[j]);
            }

            for (int j = 0; j < tileObjects[i].aboveNeighbours.Count; j++)
            {
                tileStruct.aboveNeighbors[j] = Array.IndexOf(tileObjects, tileObjects[i].aboveNeighbours[j]);
            }

            for (int j = 0; j < tileObjects[i].belowNeighbours.Count; j++)
            {
                tileStruct.belowNeighbours[j] = Array.IndexOf(tileObjects, tileObjects[i].belowNeighbours[j]);
            }
            tileStructs[i] = tileStruct;
        }
        return tileStructs;
    }

    /// <summary>
    /// Creates the Cell3DStructs needed for the shader
    /// </summary>
    /// <returns></returns>
    unsafe Cell3DStruct[] CreateCell3DStructs()
    {
        int[] tileObjectIndexes = new int[tileObjects.Length];

        for (int i = 0; i < tileObjects.Length; i++)
        {
            // Initially all the tiles are possible,
            // so the indexes are the same as the array indexes
            tileObjectIndexes[i] = i;
        }

        Cell3DStruct[] cell3DStructs = new Cell3DStruct[gridComponents.Count];

        for(int i = 0; i < gridComponents.Count; i++)
        {
            Cell3DStruct cellStruct = new Cell3DStruct();
            cellStruct.colapsed = gridComponents[i].collapsed? (uint) 1 : (uint) 0;
            for (int j = 0; j < tileObjectIndexes.Length; j++)
            {
                cellStruct.tileOptions[j] = tileObjectIndexes[j];
            }
            cellStruct.entropy = MAX_NEIGHBOURS;
            cell3DStructs[i] = cellStruct;
        }
        return cell3DStructs;
    }

    unsafe void CreateSolidFloor(Cell3DStruct[] cell3DStructs)
    {
        int y = 0;
        for (int z = 0; z < dimensionsZ; z++)
        {
            for (int x = 0; x < dimensionsX; x++)
            {
                int index = x + (z * dimensionsX) + (y * dimensionsX * dimensionsZ);
                cell3DStructs[index].colapsed = 1;
                cell3DStructs[index].entropy = 1;
                for(int i = 1; i < MAX_NEIGHBOURS; i++)
                {
                    cell3DStructs[index].tileOptions[i] = -1;
                }
                cell3DStructs[index].tileOptions[0] = Array.IndexOf(tileObjects, floorTile);
            }
        }
    }

    unsafe void CreateEmptyCeiling(Cell3DStruct[] cell3DStructs)
    {
        int y = dimensionsY-1;
        for (int z = 0; z < dimensionsZ; z++)
        {
            for (int x = 0; x < dimensionsX; x++)
            {
                int index = x + (z * dimensionsX) + (y * dimensionsX * dimensionsZ);
                cell3DStructs[index].colapsed = 1;
                cell3DStructs[index].entropy = 1;
                for(int i = 1; i < MAX_NEIGHBOURS; i++)
                {
                    cell3DStructs[index].tileOptions[i] = -1;
                }
                cell3DStructs[index].tileOptions[0] = Array.IndexOf(tileObjects, emptyTile);
            }
        }
    }

    public unsafe void Regenerate() // TODO
    {
        if (onRegenerate != null)
        {
            onRegenerate();
        }

        // Clear the grid
        for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }

        ClearNeighbours(ref tileObjects);
        CreateRemainingCells(ref tileObjects);
        DefineNeighbourTiles(ref tileObjects, ref tileObjects);

        gridComponents = new List<Cell3D2>();
        stopwatch = new Stopwatch();

        stopwatch.Start();
        InitializeGrid();

        // Create the structs
        Tile3DStruct[] tileObjectsStructs = CreateTile3DStructs();
        Cell3DStruct[] gridComponentsStructs = CreateCell3DStructs();
        CreateSolidFloor(gridComponentsStructs);
        CreateEmptyCeiling(gridComponentsStructs);

        // Initialize buffers
        ComputeBuffer gridComponentsBuffer = new ComputeBuffer(gridComponentsStructs.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Cell3DStruct)));
        ComputeBuffer tileObjectsBuffer = new ComputeBuffer(tileObjectsStructs.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Tile3DStruct)));
        ComputeBuffer outputBuffer = new ComputeBuffer(gridComponentsStructs.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Cell3DStruct)));

        // Set data
        gridComponentsBuffer.SetData(gridComponentsStructs);
        outputBuffer.SetData(gridComponentsStructs);
        tileObjectsBuffer.SetData(tileObjectsStructs);

        // Data to buffers
        shader.SetBuffer(0, "gridComponents", gridComponentsBuffer);
        shader.SetBuffer(0, "tileObjects", tileObjectsBuffer);
        shader.SetBuffer(0, "output", outputBuffer);
        shader.SetInt("MAX_NEIGHBOURS", MAX_NEIGHBOURS);
        shader.SetInt("gridDimensionsX", dimensionsX);
        shader.SetInt("gridDimensionsY", dimensionsY);
        shader.SetInt("gridDimensionsZ", dimensionsZ);
        shader.SetInt("floorTile", Array.IndexOf(tileObjects, floorTile));
        shader.SetInt("emptyTile", Array.IndexOf(tileObjects, emptyTile));
        shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode());//DateTime.Now.Ticks.GetHashCode());
        shader.SetVector("offset", new Vector3(0, 1, 0));

        // Dispatch 1/5 of the grid
        shader.Dispatch(0, dimensionsX / 2, 1, dimensionsZ / 2);

        // // Dispatch 2/5 of the grid
        // shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * 2);
        // shader.SetVector("offset", new Vector3(-1, 1, -1));
        // shader.Dispatch(0, dimensionsX / 4, 1, dimensionsZ / 4);

        // // Dispatch 3/5 of the grid
        // shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * 3);
        // shader.SetVector("offset", new Vector3(1, 1, -1));
        // shader.Dispatch(0, dimensionsX / 4, 1, dimensionsZ / 4);

        // // Dispatch 4/5 of the grid
        // shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * 4);
        // shader.SetVector("offset", new Vector3(-1, 1, 1));
        // shader.Dispatch(0, dimensionsX / 4, 1, dimensionsZ / 4);

        // // Dispatch 5/5 of the grid
        // shader.SetInt("seed", DateTime.Now.Ticks.GetHashCode() * 5);
        // shader.SetVector("offset", new Vector3(1, 1, 1));
        // shader.Dispatch(0, dimensionsX / 4, 1, dimensionsZ / 4);

        // Get data
        Cell3DStruct[] output = new Cell3DStruct[gridComponentsStructs.Length];
        outputBuffer.GetData(output);

        // Recreate the grid based on the data received by the shader
        for (int i = 0; i < output.Length; i++)
        {
            //if(output[i].colapsed == 0) continue; // Testing
            Cell3D2 cell = gridComponents[i];
            cell.name = "Cell " + i;
            cell.collapsed = output[i].colapsed == 1;
            //cell.RecreateCell(tileObjects[output[i].tileOptions[0]]);

            // Uncomment this to recreate the cell with all the possible tiles

            List<Tile3D2> newOptions = new List<Tile3D2>();
            for (int j = 0; j < MAX_NEIGHBOURS; j++)
            {
                if (output[i].tileOptions[j] != -1) newOptions.Add(tileObjects[output[i].tileOptions[j]]);
            }
            cell.RecreateCell(newOptions.ToArray());

            if (cell.transform.childCount != 0)
            {
                foreach (Transform child in cell.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            Tile3D2 instantiatedTile = Instantiate(cell.tileOptions[0], cell.transform.position, Quaternion.identity, cell.transform);
            if (instantiatedTile.rotation != Vector3.zero)
            {
                instantiatedTile.gameObject.transform.Rotate(cell.tileOptions[0].rotation, Space.Self);
            }

            instantiatedTile.gameObject.transform.position += instantiatedTile.positionOffset;
            instantiatedTile.gameObject.SetActive(true);
        }

        // Release memory buffers to avoid leaks
        gridComponentsBuffer.Release();
        tileObjectsBuffer.Release();
        outputBuffer.Release();
        stopwatch.Stop();
        Debug.Log("Time elapsed: " + stopwatch.ElapsedMilliseconds + "ms");
    }

    /// <summary>
    /// This method is used to trigger the RenderDoc capture (RenderDoc must be installed, launched and linked to Unity)
    /// </summary>
    private void TriggerRenderDocCapture()
    {
        Assembly asm = typeof(UnityEditor.EditorWindow).Assembly;
        Type GameViewType = asm.GetType("UnityEditor.GameView");
        Type HostViewType = asm.GetType("UnityEditor.HostView");
        Type GUIViewType = asm.GetType("UnityEditor.GUIView");
        EditorWindow window = EditorWindow.GetWindow(GameViewType);
        FieldInfo m_ParentFieldInfo = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
        var m_Parent = m_ParentFieldInfo.GetValue(window);
        MethodInfo CaptureRenderDocFullContentInfo = GUIViewType.GetMethod("CaptureRenderDocFullContent", BindingFlags.Public | BindingFlags.Instance);
        CaptureRenderDocFullContentInfo.Invoke(m_Parent, null);
    }
}