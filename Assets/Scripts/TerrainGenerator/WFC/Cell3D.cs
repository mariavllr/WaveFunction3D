using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell3D : MonoBehaviour
{
    public bool collapsed;
    public Tile3D[] tileOptions;
    public bool haSidoVisitado; //debug
    public bool visitable = false;

    public int index; //debug
    public bool showDebugVisitableCells;
    public int entropy;
    public bool artificialEntropy = false;

    public void CreateCell(bool collapseState, Tile3D[] tiles, int cellIndex)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
        index = cellIndex;
        entropy = tileOptions.Length;

        if (!showDebugVisitableCells) Destroy(transform.GetChild(0).gameObject);
    }

    public void RecreateCell(Tile3D[] tiles)
    {
        tileOptions = tiles;
        if (!artificialEntropy) entropy = tileOptions.Length;
    }

    public void MakeVisitable()
    {
        visitable = true;
        if (!collapsed && showDebugVisitableCells) GetComponentInChildren<MeshRenderer>().material.color = new Color32(255, 30, 0, 50); 
    }
}
