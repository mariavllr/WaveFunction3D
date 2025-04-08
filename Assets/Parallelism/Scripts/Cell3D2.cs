using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell3D2 : MonoBehaviour
{
    public bool collapsed;
    public Tile3D2[] tileOptions;
    public bool haSidoVisitado; //debug
    public bool visitable = false; //optimization
    public int index; //debug
    public bool showDebugVisitableCells;
    public bool centerCubeCell;

    MeshRenderer meshRenderer;



    public void CreateCell(bool collapseState, Tile3D2[] tiles, int cellIndex)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        haSidoVisitado = false;
        index = cellIndex;
        centerCubeCell = false;

        meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (!showDebugVisitableCells) Destroy(transform.GetChild(0).gameObject);
    }

    public void RecreateCell(Tile3D2[] tiles)
    {
        tileOptions = tiles;
    }

    public void MakeVisitable()
    {
        visitable = true;
        //    if (!collapsed && showDebugVisitableCells) MakeVisible(true);
    }

    public void MakeVisible(bool visibility)
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.enabled = visibility;
    }

    public void ChangeAlpha(float alpha)
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            Color color = meshRenderer.material.color;
            color.a = alpha;
            meshRenderer.material.color = color;
        }

    }
}
