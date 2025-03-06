using System;
using UnityEngine;

public class DragObject : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private float objectZ;

    public static event Action<GameObject> OnTileReleased;

    void Start()
    {
        mainCamera = Camera.main;
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
                objectZ = mainCamera.WorldToScreenPoint(transform.position).z;
                offset = transform.position - GetWorldMousePosition();
            }
        }

        if (isDragging)
        {
            transform.position = GetWorldMousePosition() + offset;
        }

        if (Input.GetMouseButtonUp(0)) // Suelta el click
        {
            isDragging = false;
            OnTileReleased?.Invoke(this.gameObject); // Disparamos el evento
        }
    }

    private Vector3 GetWorldMousePosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = objectZ;
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }
}

