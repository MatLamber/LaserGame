
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using UnityEditor.SceneManagement;

[CustomEditor(typeof(GridGenerator))]
public class GridGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Referencia al objeto GridGenerator
        GridGenerator gridGenerator = (GridGenerator)target;

        // Dibujar el inspector predeterminado
        DrawDefaultInspector();

        // Espacio visual
        EditorGUILayout.Space(10);

        // Botones para generar y limpiar la cuadrícula
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generar Cuadrícula", GUILayout.Height(30)))
        {
            // Registrar la operación para permitir deshacer
            Undo.RecordObject(gridGenerator, "Generate Grid");

            // Generar la cuadrícula
            gridGenerator.GenerateGrid();

            // Marcar la escena como sucia para que Unity pregunte si quieres guardar
            EditorUtility.SetDirty(gridGenerator);

            // Para actualizar la escena
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(gridGenerator.gameObject.scene);
        }

        if (GUILayout.Button("Limpiar Cuadrícula", GUILayout.Height(30)))
        {
            // Registrar la operación para permitir deshacer
            Undo.RecordObject(gridGenerator, "Clear Grid");

            // Limpiar la cuadrícula
            gridGenerator.ClearGrid();

            // Marcar la escena como sucia para que Unity pregunte si quieres guardar
            EditorUtility.SetDirty(gridGenerator);

            // Para actualizar la escena
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(gridGenerator.gameObject.scene);
        }

        EditorGUILayout.EndHorizontal();

        // Información de la cuadrícula
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"Elementos en la cuadrícula: {gridGenerator.instantiatedObjects.Count}",
            EditorStyles.boldLabel);
    }

}

#endif