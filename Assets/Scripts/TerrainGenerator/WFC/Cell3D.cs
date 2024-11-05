using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell3D : MonoBehaviour
{
    public bool collapsed;
    public Tile3D[] tileOptions;
    public bool haSidoVisitado; //debug
    public bool tieneCiudad;
    public int index; //debug
    public bool blocked = false; //If is bloq, no tile can be spawned here
    

    public void CreateCell(bool collapseState, Tile3D[] tiles, int cellIndex)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
        tieneCiudad = false;
        index = cellIndex;
    }

    public void RecreateCell(Tile3D[] tiles)
    {
        tileOptions = tiles;
    }
}
