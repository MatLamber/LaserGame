using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellGrid : MonoBehaviour
{
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private bool isEnergized = false;

    [Header("Position Offsets")] [SerializeField]
    private float yOffset = 0f; // Adjustable Y offset for placed objects

    [SerializeField] private float zOffset = 0f; // Adjustable Z offset for placed objects
    
    [Header("Cell plate Settings")]
    
    [SerializeField] private GameObject cellPlate;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color occupiedColor;
    [SerializeField] private Color energizedColor;
    [SerializeField] private float energizedColorIntensity = 1.5f;
    private Material cellPlateMaterial;

    /// <summary>
    /// Checks if this grid cell is available for a draggable object
    /// </summary>
    /// <returns>True if the cell is free, false if occupied</returns>
    public bool IsCellFree()
    {
        // The cell is considered free if it has 0 or 1 child (the first child could be a visual marker or background)
        return transform.childCount <= 1;
    }

    /// <summary>
    /// Called when a draggable object is placed in this cell
    /// </summary>
    /// <param name="draggableObject">The draggable object being placed</param>
    public void PlaceObject(DraggeableObject draggableObject)
    {
        if (IsCellFree())
        {
            // Set the draggable object as a child of this cell
            draggableObject.transform.SetParent(transform);

            // Position the object at x=0, with the specified y and z offsets
            draggableObject.transform.localPosition = new Vector3(0f, yOffset, zOffset);

            isOccupied = true;
            SetColor(draggableObject);
            OnGunPlaced(draggableObject);
        }
    }

    /// <summary>
    /// Called when a draggable object is removed from this cell
    /// </summary>
    public void RemoveObject()
    {
        isOccupied = false;
        SetColor();
    }

    /// <summary>
    /// Sets the Y offset for object placement
    /// </summary>
    /// <param name="offset">The Y offset value</param>
    public void SetYOffset(float offset)
    {
        yOffset = offset;
    }

    /// <summary>
    /// Gets the current Y offset value
    /// </summary>
    /// <returns>The Y offset value</returns>
    public float GetYOffset()
    {
        return yOffset;
    }

    /// <summary>
    /// Sets the Z offset for object placement
    /// </summary>
    /// <param name="offset">The Z offset value</param>
    public void SetZOffset(float offset)
    {
        zOffset = offset;
    }

    /// <summary>
    /// Gets the current Z offset value
    /// </summary>
    /// <returns>The Z offset value</returns>
    public float GetZOffset()
    {
        return zOffset;
    }

    // Optional: visual feedback when the mouse hovers over the cell
    private void OnMouseEnter()
    {
        if (IsCellFree())
        {
            // You could add visual feedback here (e.g., highlight the cell)
        }
    }

    private void OnMouseExit()
    {
        SetColor();
    }
    
    private void Start()
    {
        cellPlateMaterial = cellPlate.GetComponent<Renderer>().material;
    }

    private void SetColor(DraggeableObject placedObject = null)
    {
        if (placedObject != null)
        {
            if (placedObject.GetType() == DraggableType.Energizer)
            {
                ChangeToEnergizedColor();
                EnergizeAdjacentCells();
            }
            else
            {
                if(!isEnergized)
                    cellPlateMaterial.SetColor("_BaseColor",occupiedColor);
            }
        }
        else
        {
            cellPlateMaterial.color = defaultColor;
        }
    }


    public void ChangeToEnergizedColor()
    {
        isEnergized = true;
        cellPlateMaterial.SetColor("_BaseColor", energizedColor * energizedColorIntensity);
    }
    
    private void EnergizeAdjacentCells()
    {
        Vector3 currentPos = transform.position;
        float cellSize = 1f; // Adjust this value based on your grid cell size
    
        // Check adjacent cells (up, down, left, right)
        Vector3[] adjacentPositions = new Vector3[]
        {
            currentPos + Vector3.forward * cellSize,  // Forward
            currentPos - Vector3.forward * cellSize,  // Back
            currentPos + Vector3.right * cellSize,    // Right
            currentPos - Vector3.right * cellSize     // Left
        };
    
        foreach (Vector3 pos in adjacentPositions)
        {
            Collider[] hitColliders = Physics.OverlapSphere(pos, 0.1f);
            foreach (var hitCollider in hitColliders)
            {
                CellGrid adjacentCell = hitCollider.GetComponent<CellGrid>();
                if (adjacentCell != null)
                {
                    adjacentCell.ChangeToEnergizedColor();
                }
            }
        }
    }

    private void OnGunPlaced(DraggeableObject placedObject)
    {
        if(placedObject.GetType() == DraggableType.Gun && isEnergized)
            placedObject.GetComponent<Laser>().EnableBeam();
    }
}