using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum DraggableType
{
    Energizer,
    Gun
}
public class DraggeableObject : MonoBehaviour
{
    [Header("Drag Settings")] [SerializeField]
    private float smoothSpeed = 10.0f; // Velocidad de suavizado

    [SerializeField] private LayerMask dragPlane; // Capa para el plano de arrastre
    [SerializeField] private LayerMask gridCellLayer; // Capa para las celdas del grid
    [SerializeField] private DraggableType draggableType;

    private Camera mainCamera;
    private Vector3 dragOffset;
    private bool isDragging = false;
    private float originalY; // Almacena la posición Y original
    private CellGrid currentCell; // La celda actual donde está el objeto
    private Transform originalParent; // El padre original del objeto

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

        // Calcular el offset entre la posición del mouse y el objeto
        Vector3 mousePosition = GetMouseWorldPosition();
        dragOffset = transform.position - mousePosition;
    }

    private void OnMouseUp()
    {
        isDragging = false;

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
            targetPosition.y = originalY;

            // Mover el objeto suavemente hacia la posición objetivo
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

            // Opcional: Resaltar la celda debajo del objeto mientras se arrastra
            HighlightCellUnderMouse();
        }
    }

    private void HighlightCellUnderMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, gridCellLayer))
        {
            CellGrid cellGrid = hit.collider.GetComponent<CellGrid>();
            if (cellGrid != null)
            {
                // Aquí podrías implementar lógica para resaltar visualmente la celda
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        // Crear un rayo desde la cámara hacia el mouse
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Proyectar el rayo en un plano para obtener la posición 3D
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, dragPlane))
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