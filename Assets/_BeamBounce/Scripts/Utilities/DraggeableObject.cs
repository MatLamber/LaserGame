using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;

public enum DraggableType
{
    Energizer,
    Gun,
    GridWeapon, 
    Bouncer
}
public class DraggeableObject : MonoBehaviour
{
    [Header("Drag Settings")] [SerializeField]
    private float smoothSpeed = 10.0f; // Velocidad de suavizado

   // [SerializeField] private LayerMask dragPlane; // Capa para el plano de arrastre
    [SerializeField] private LayerMask gridCellLayer; // Capa para las celdas del grid
    [SerializeField] private DraggableType draggableType;

    [SerializeField] private MMF_Player pickedUpFeedback;
    [SerializeField] private MMF_Player putDownFeedback;

    private Camera mainCamera;
    private Vector3 dragOffset;
    private bool isDragging = false;
    private float originalY; // Almacena la posición Y original
    private CellGrid currentCell; // La celda actual donde está el objeto
    private Transform originalParent; // El padre original del objeto
    private CellGrid lastHighlightedCell;
    private float persistentY;

    private void Awake()
    {
        mainCamera = Camera.main;
        originalParent = transform.parent;
    }

    private void OnMouseDown()
    {
        isDragging = true;

        // Si el objeto estaba en una celda, notificar que ahora está libre
        if (currentCell != null)
        {
            currentCell.RemoveObject();
            currentCell = null;
        }

        // Separar el objeto de su padre durante el arrastre
        transform.SetParent(null);

        // Guardar la posición Y original
        originalY = transform.position.y;
        LeanTween.moveY(gameObject, originalY + 1f, 0.3f).setEaseOutQuad();
        persistentY = originalY + 1f;

        // Calcular el offset entre la posición del mouse y el objeto
        Vector3 mousePosition = GetMouseWorldPosition();
        dragOffset = transform.position - mousePosition;
        pickedUpFeedback?.PlayFeedbacks();
    }

    private void OnMouseUp()
    {
        isDragging = false;
        LeanTween.cancel(gameObject);
        putDownFeedback?.PlayFeedbacks();
        // Si teníamos una celda resaltada, restaurar su apariencia normal
        if (lastHighlightedCell != null)
        {
            lastHighlightedCell.ChangeToDefaultColor(); // Asumiendo que tienes este método
            lastHighlightedCell = null;
        }

        // Verificar si hay una celda de grid debajo
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, gridCellLayer))
        {
            CellGrid cellGrid = hit.collider.GetComponent<CellGrid>();
            if (cellGrid != null && cellGrid.IsCellFree())
            {
                // La celda está libre, colocar el objeto allí
                cellGrid.PlaceObject(this);
                currentCell = cellGrid;
                return;
            }
        }

        // Si no se coloca en una celda, volver a su padre original
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }

    }

    private void Update()
    {
        if (isDragging)
        {
            // Calcular la posición objetivo con el offset
            Vector3 mousePosition = GetMouseWorldPosition();
            Vector3 targetPosition = mousePosition + dragOffset;

            // Mantener la Y original
            targetPosition.y = persistentY;

            // Mover el objeto suavemente hacia la posición objetivo
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

            // Opcional: Resaltar la celda debajo del objeto mientras se arrastra
            HighlightCellUnderMouse();
        }
    }

    private void HighlightCellUnderMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        CellGrid currentHighlightedCell = null;
    
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, gridCellLayer))
        {
            currentHighlightedCell = hit.collider.GetComponent<CellGrid>();
            if (currentHighlightedCell != null)
            {
                // Resaltar la nueva celda
                currentHighlightedCell.ChangeToHighlightedColor();
            }
        }
    
        // Verificar si abandonamos una celda
        if (lastHighlightedCell != null && lastHighlightedCell != currentHighlightedCell)
        {
            // Aquí manejas el evento de "dejar de estar sobre la celda"
            OnCellExited(lastHighlightedCell);
        
            // Puedes también restaurar el color normal si tu clase CellGrid tiene un método para eso
            // lastHighlightedCell.RestoreOriginalColor();
        }
    
        // Verificar si entramos en una nueva celda
        if (currentHighlightedCell != null && lastHighlightedCell != currentHighlightedCell)
        {
            // Aquí manejas el evento de "entrar en una nueva celda"
            OnCellEntered(currentHighlightedCell);
        }
    
        // Actualizar referencia
        lastHighlightedCell = currentHighlightedCell;
    }
    
    private void OnCellExited(CellGrid cell)
    {

        // Aquí puedes añadir cualquier lógica que necesites cuando el objeto deja una celda
        // Por ejemplo, restaurar el color original de la celda
        cell.ChangeToDefaultColor(); // Asumiendo que tienes este método en tu clase CellGrid
    }

    private void OnCellEntered(CellGrid cell)
    {
        cell.ChangeToHighlightedColor();
        // Lógica adicional al entrar en una celda si es necesario
    }


    private Vector3 GetMouseWorldPosition()
    {
        // Crear un rayo desde la cámara hacia el mouse
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Proyectar el rayo en un plano para obtener la posición 3D
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue/*, dragPlane*/))
        {
            return hit.point;
        }

        // Si no hay colisión, proyectar en un plano imaginario a la altura del objeto
        Plane plane = new Plane(Vector3.up, new Vector3(0, originalY, 0));
        float distance;
        plane.Raycast(ray, out distance);
        return ray.GetPoint(distance);
    }

    public DraggableType GetType()
    {
        return draggableType;
    }
}