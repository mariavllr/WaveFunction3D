using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell3D2 : MonoBehaviour
{
    public bool collapsed;
    public Tile3D2[] tileOptions;
    public bool haSidoVisitado; //debug
    public bool visitable = false;
    public bool tieneCiudad;
    public int index; //debug
    public bool blocked = false; //If is bloq, no tile can be spawned here
    public bool showDebugVisitableCells;

    public void CreateCell(bool collapseState, Tile3D2[] tiles, int cellIndex)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
        tieneCiudad = false;
        index = cellIndex;

        if (!showDebugVisitableCells) Destroy(transform.GetChild(0).gameObject);
    }

    public void RecreateCell(Tile3D2[] tiles)
    {
        tileOptions = tiles;
    }

    public void MakeVisitable()
    {
        visitable = true;
        if (!collapsed && showDebugVisitableCells) GetComponentInChildren<MeshRenderer>().material.color = new Color32(255, 30, 0, 50);
    }
}
