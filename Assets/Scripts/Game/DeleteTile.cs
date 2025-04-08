using System;
using UnityEngine;
using UnityEngine.EventSystems; // Necesario para eventos de UI

public class DeleteTile : MonoBehaviour, IPointerEnterHandler
{
    public static event Action OnDeleteTile;
    public void OnPointerEnter(PointerEventData eventData)
    {

        if (Input.GetMouseButton(0)) //comprobar que tienes un objeto en la mano!
        {
            Debug.Log("Borrar tile");
            OnDeleteTile?.Invoke();
        }

    }
}
