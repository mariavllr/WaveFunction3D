using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class DragObject : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private float objectZ;
    private Tile3D2 tile;

    public static event Action<Tile3D2> OnTileDragged;
    public static event Action<GameObject> OnTileReleased;


    //Para mostrar las celdas validas y mostrar una preview del objeto colocado
    private List<Cell3D2> validCells = new List<Cell3D2>(); // para acceder a las celdas válidas, se actualiza desde WaveFunctionGame

    private Cell3D2 currentPreviewCell = null;
    private GameObject currentPreviewInstance = null;
    private WaveFunctionGame wfc;

    Material previewMaterial;
    private void Awake()
    {
        DeleteTile.OnDeleteTile += OnTileDeleted;
        CardGenerator.OnTileRotated += OnTileRotated;
        wfc = FindAnyObjectByType<WaveFunctionGame>();
    }
    private void OnDestroy()
    {
        DeleteTile.OnDeleteTile -= OnTileDeleted;
        CardGenerator.OnTileRotated -= OnTileRotated;
    }
    public void SetValidCells(List<Cell3D2> cells)
    {
        validCells = cells;
    }

    void Start()
    {
        mainCamera = Camera.main;
        tile = GetComponent<Tile3D2>();
        previewMaterial = wfc.previewMaterial;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Click izquierdo
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.transform == transform)
            {
                isDragging = true;
                OnTileDragged?.Invoke(tile);
                objectZ = mainCamera.WorldToScreenPoint(transform.position).z;
                offset = transform.position - GetWorldMousePosition();
            }
        }

        if (isDragging)
        {
            transform.position = GetWorldMousePosition() + offset;

            //RotateTile();

            //Muestra las celdas donde se puede colocar
            if (validCells != null && validCells.Count > 0)
            {
                Cell3D2 closest = FindClosestCell(transform.position, validCells);

                if (closest != currentPreviewCell)
                {
                    if (currentPreviewInstance != null)
                    {
                        Destroy(currentPreviewInstance);
                    }

                    currentPreviewCell = closest;
                    // Instanciar nuevo preview
                    currentPreviewInstance = CreatePreviewAtCell(currentPreviewCell);
                    //Sonido de cambiar de cell
                    wfc.audioSource.PlayOneShot(wfc.changeCellSound, 0.5f);
                }
            }


            if (Input.GetMouseButtonUp(0)) // Suelta el click
            {
                isDragging = false;
                OnTileReleased?.Invoke(this.gameObject); // Disparamos el evento

                if (currentPreviewInstance != null)
                {
                    Destroy(currentPreviewInstance);
                    currentPreviewInstance = null;
                    currentPreviewCell = null;
                }

                wfc.audioSource.PlayOneShot(wfc.collapseCellSound, 0.5f);
            }
        }


    }

    private Vector3 GetWorldMousePosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = objectZ;
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    void OnTileRotated(Vector3 rotation, Tile3D2 tile)
    {
        OnTileDeleted();
    }

    void OnTileDeleted()
    {
        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
            currentPreviewCell = null;
        }
    }

    private Cell3D2 FindClosestCell(Vector3 origin, List<Cell3D2> cells)
    {
        Cell3D2 closest = null;
        float minDistSq = Mathf.Infinity;

        foreach (Cell3D2 cell in cells)
        {
            float distSq = (cell.transform.position - origin).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = cell;
            }
        }
        return closest;
    }

 

    private GameObject CreatePreviewAtCell(Cell3D2 cell)
    {
        // Instanciar el objeto de vista previa y desactivar la celda transparente
        GameObject preview = Instantiate(gameObject);


        // Limpiar componentes innecesarios
        DestroyImmediate(preview.GetComponent<DragObject>());
        DestroyImmediate(preview.GetComponent<Tile3D2>());

        foreach (var col in preview.GetComponentsInChildren<Collider>())
            Destroy(col);

        // Crear y aplicar material URP personalizado
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();

        if(previewMaterial != null)
        {
            foreach (Renderer rend in renderers)
            {
                Material[] newMats = new Material[rend.materials.Length];
                for (int i = 0; i < newMats.Length; i++)
                {
                    newMats[i] = previewMaterial;
                }
                rend.materials = newMats;
            }

            preview.transform.position = cell.transform.position;
        }

        else
        {
            Debug.LogError("Preview material not found. Please assign a material in the inspector.");
        }
        

        // Rotación y offset opcional
        Tile3D2 originalTile = GetComponent<Tile3D2>();
        if (originalTile != null)
        {
            preview.transform.rotation = Quaternion.Euler(originalTile.rotation);
            preview.transform.position += originalTile.positionOffset;
        }

        preview.name = "PreviewTile";

        // Efecto de rebote con DOTween
        Vector3 originalPosition = preview.transform.position;
        preview.transform.position = new Vector3(originalPosition.x, originalPosition.y - 0.5f, originalPosition.z); // Empujar hacia abajo un poco
        preview.transform.DOJump(originalPosition, jumpPower: 0.25f, numJumps: 1, duration: 0.3f).SetEase(Ease.OutBounce); // Rebotar hacia arriba

        return preview;
    }
}

