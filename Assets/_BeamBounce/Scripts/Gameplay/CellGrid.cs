using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellGrid : MonoBehaviour
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private bool isEnergized = false;
    [SerializeField] private bool hasEnergySource = false;

    [Header("Position Offsets")] [SerializeField]
    private float yOffset = 0f; // Adjustable Y offset for placed objects

    [SerializeField] private float zOffset = 0f; // Adjustable Z offset for placed objects
    
    [Header("Cell plate Settings")]
    
    [SerializeField] private GameObject cellPlate;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color occupiedColor;
    [SerializeField] private Color energizedColor;
    //[SerializeField] private float energizedColorIntensity = 1.5f;
    private Material cellPlateMaterial;
    private DraggeableObject placedObject;
     [SerializeField] float energizedColorIntensity;

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
            placedObject = draggableObject;
            SetColor(draggableObject);
            switch (placedObject.GetType())
            {
                case DraggableType.Energizer:
                    OnEnergizerPlaced();
                    EnergizeAdjacentCells();
                    break;
                case DraggableType.Gun:
                    OnGunPlaced(draggableObject);
                    break;
                case DraggableType.GridWeapon:
                    OnGridWeaponPlaced();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    /// <summary>
    /// Called when a draggable object is removed from this cell
    /// </summary>
    public void RemoveObject()
    {
        isOccupied = false;

        switch (placedObject.GetType())
        {
            case DraggableType.Energizer:
                OnEnergizerRemoved();
                break;
            case DraggableType.Gun:
                OnGunRemoved();
                break;
            case DraggableType.GridWeapon:
                OnGridWeaponRemoved();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        placedObject = null;
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
       // SetColor();
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
            }
            else
            {
                if(!isEnergized)
                    ChangeToOccupiedColor();
            }
        }
        else
        {
            ChangeToDefaultColor();
        }
    }



    public void ChangeToEnergizedColor()
    {
        isEnergized = true;
        cellPlateMaterial.SetColor(EmissionColor, energizedColor * energizedColorIntensity);
        OnGunPlaced(placedObject);
    }
    
    public void ChangeToDefaultColor()
    {
        isEnergized = false;
        cellPlateMaterial.SetColor(EmissionColor, defaultColor * energizedColorIntensity);
        OnGunPlaced(placedObject);
    }
    
    public void ChangeToOccupiedColor()
    {
        isEnergized = false;
        OnGunPlaced(placedObject);
        cellPlateMaterial.SetColor(EmissionColor, occupiedColor);
    }


    private void DrainAdjacentCells()
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
                    if(adjacentCell.isOccupied)
                        adjacentCell.ChangeToOccupiedColor();
                    else
                        adjacentCell.ChangeToDefaultColor();
                }
            }
        }
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
        if(placedObject == null || placedObject.GetType() != DraggableType.Gun ) return;
        if(placedObject.GetType() == DraggableType.Gun && isEnergized)
            placedObject.GetComponent<Laser>().EnableBeam();
        else if (!isEnergized)
            placedObject.GetComponent<Laser>().DisableBeam();
    }

    private void OnGunRemoved()
    {
        if(placedObject == null) return;
        placedObject.GetComponent<Laser>().DisableBeam();
        if(!isEnergized)
            ChangeToDefaultColor();
    }
    private void OnEnergizerPlaced()
    {
        hasEnergySource = true;
        ChangeToEnergizedColor();
    }

    private void OnEnergizerRemoved()
    {
        if (hasEnergySource)
        {
            hasEnergySource = false;
            ChangeToDefaultColor();
            DrainAdjacentCells();
        }
    }

    private void OnGridWeaponPlaced()
    {
        if(!isEnergized)
            ChangeToOccupiedColor();
    }

    private void OnGridWeaponRemoved()
    {
        if(!isEnergized)
            ChangeToDefaultColor();
    }
}