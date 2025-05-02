using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Configuration")] public int width = 10;
    public int depth = 10; // Cambiado de "height" a "depth" para mejor claridad

    [Tooltip("El espacio adicional entre celdas (0 = sin espacio extra)")]
    public float spacing = 0f;

    public GameObject prefab;

    [Header("Grid Offset")] public Vector3 startOffset = Vector3.zero;

    [HideInInspector] public List<GameObject> instantiatedObjects = new List<GameObject>();

    // Esta función será llamada desde el editor
    public void GenerateGrid()
    {
        // Validar que haya un prefab asignado
        if (prefab == null)
        {
            Debug.LogError("¡No hay un prefab asignado! Por favor asigna un prefab.");
            return;
        }

        // Verificar si el prefab tiene un BoxCollider
        BoxCollider boxCollider = prefab.GetComponent<BoxCollider>();
        BoxCollider2D boxCollider2D = prefab.GetComponent<BoxCollider2D>();

        // Calcular el tamaño de la celda basado en el collider
        Vector3 cellSize = Vector3.one;

        if (boxCollider != null)
        {
            // Usar el tamaño del BoxCollider 3D
            cellSize = boxCollider.size;
            // Si el prefab tiene escala, aplicarla al tamaño
            cellSize.x *= prefab.transform.localScale.x;
            cellSize.y *= prefab.transform.localScale.y;
            cellSize.z *= prefab.transform.localScale.z;
        }
        else if (boxCollider2D != null)
        {
            // Usar el tamaño del BoxCollider 2D
            cellSize = new Vector3(
                boxCollider2D.size.x * prefab.transform.localScale.x,
                boxCollider2D.size.y * prefab.transform.localScale.y,
                1f
            );
        }
        else
        {
            Debug.LogWarning("El prefab no tiene un BoxCollider o BoxCollider2D. Se usará el tamaño 1 por defecto.");
        }

        // Limpiar la cuadrícula anterior
        ClearGrid();

        // Crear la nueva cuadrícula en los ejes X y Z
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                // Calcular la posición del elemento usando el tamaño del collider más el espaciado
                Vector3 position = new Vector3(
                    startOffset.x + x * (cellSize.x + spacing),
                    startOffset.y, // La altura Y es fija para todas las celdas
                    startOffset.z + z * (cellSize.z + spacing)
                );

                // Instanciar el prefab manteniendo la conexión con el prefab original
                GameObject instance;

#if UNITY_EDITOR
                // En el editor, usamos PrefabUtility para mantener la conexión con el prefab
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance != null)
                {
                    instance.transform.position = position;
                }
#else
                // En tiempo de ejecución, usamos el método Instantiate normal
                instance = Instantiate(prefab, position, Quaternion.identity);
#endif

                if (instance != null)
                {
                    instance.name = $"{prefab.name}_{x}_{z}";

                    // Hacer al prefab hijo de este objeto
                    instance.transform.SetParent(transform, false);

                    // Guardar la referencia
                    instantiatedObjects.Add(instance);
                }
            }
        }

        Debug.Log($"Cuadrícula generada con {instantiatedObjects.Count} elementos. Tamaño de celda: {cellSize}");
    }

    // Esta función limpia todos los objetos de la cuadrícula
    public void ClearGrid()
    {
        // Eliminar objetos antiguos
        foreach (GameObject obj in instantiatedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }

        // Limpiar la lista
        instantiatedObjects.Clear();
        Debug.Log("Cuadrícula eliminada.");
    }
}