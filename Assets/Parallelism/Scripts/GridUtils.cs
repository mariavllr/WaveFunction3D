using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cell3DStruct = WaveFunction3DGPUChunks.Cell3DStruct;

public class GridUtils : MonoBehaviour
{
    public static Cell3DStruct[] ExtractSubGrid(Vector3Int startCoords, Vector3Int subGridDimensions, Cell3DStruct[] ogGrid, Vector3Int ogGridDimensions)
    {
        // Clamp to matrix bounds
        startCoords.x = Mathf.Max(0, startCoords.x);
        startCoords.z = Mathf.Max(0, startCoords.z);
        subGridDimensions.x = Mathf.Min(subGridDimensions.x, ogGridDimensions.x - startCoords.x);
        subGridDimensions.z = Mathf.Min(subGridDimensions.z, ogGridDimensions.z - startCoords.z);

        // Extract the subgrid
        List<Cell3DStruct> subGrid = new List<Cell3DStruct>();
        for (int y = startCoords.y; y < startCoords.y + subGridDimensions.y; y++)
        {
            for (int z = startCoords.z; z < startCoords.z + subGridDimensions.z; z++)
            {
                for (int x = startCoords.x; x < startCoords.x + subGridDimensions.x; x++)
                {
                    subGrid.Add(ogGrid[x + z * ogGridDimensions.x + y * ogGridDimensions.x * ogGridDimensions.z]);
                }
            }
        }

        return subGrid.ToArray();
    }

    public static int GetIndexFromCoords(Vector3Int coords, Vector3Int gridDimensions)
    {
        // Clamp to matrix bounds
        coords.x = Mathf.Max(0, coords.x);
        coords.y = Mathf.Max(0, coords.y);
        coords.z = Mathf.Max(0, coords.z);
        coords.x = Mathf.Min(coords.x, gridDimensions.x - 1);
        coords.y = Mathf.Min(coords.y, gridDimensions.y - 1);
        coords.z = Mathf.Min(coords.z, gridDimensions.z - 1);

        return coords.x + coords.z * gridDimensions.x + coords.y * gridDimensions.x * gridDimensions.z;
    }

    public static void CombineGridWithSubgrid(Cell3DStruct[] grid, Cell3DStruct[] subGrid, int startIndex)
    {
        for (int i = 0; i < subGrid.Length; i++) grid[startIndex + i] = subGrid[i];
    }
}
