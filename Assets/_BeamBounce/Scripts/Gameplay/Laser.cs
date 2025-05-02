using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public enum FireDirection
{
    Forward,
    Up,
    Right,
}

public class Laser : MonoBehaviour
{
    #region Declarations

    [Foldout("Line Renderer Settings")] [SerializeField] private LineRenderer primaryLineRenderer;// Primer LineRenderer
    [Foldout("Line Renderer Settings")] [SerializeField] private LineRenderer secondaryLineRenderer; // Segundo LineRenderer
    [Foldout("Line Renderer Settings")] [SerializeField] private Transform firePoint;
    [Foldout("Line Renderer Settings")] [SerializeField] private GameObject startVFX;
    [Foldout("Line Renderer Settings")] [SerializeField] private GameObject endVFX;
    [Foldout("Line Renderer Settings")][SerializeField] private float secondaryLineOffset = 0.1f; // Desplazamiento entre líneas
    
    [Foldout("Laser Settings")] [SerializeField] private GameObject laserPrefab;
    [Foldout("Laser Settings")] [SerializeField] private FireDirection fireDirection = FireDirection.Forward; // Dirección predeterminada
    [Foldout("Laser Settings")] [SerializeField] private int maxReflections = 5;
    [Foldout("Laser Settings")] [SerializeField] private float maxRayDistance = 100f; // Distancia máxima del rayo
    [Foldout("Laser Settings")] [SerializeField] private LayerMask reflectiveLayers; // Capas que pueden reflejar el láser
    [Foldout("Laser Settings")] [SerializeField] private LayerMask ignoreLayers; // Capas que serán ignoradas completamente
    
    [BoxGroup("Testing & Debug")] [SerializeField] private bool showDebugRays = true;
    [BoxGroup("Testing & Debug")] [SerializeField] private bool enableBeam;

    
    private Camera mainCamera;
    private bool isBeamEnabled;
    private List<ParticleSystem> endVfxps = new List<ParticleSystem>();
    private List<ParticleSystem> startVfxps = new List<ParticleSystem>();

    #endregion


    void Start()
    {
        FillLists();
        DisableLaser();
        primaryLineRenderer.positionCount = maxReflections + 1;
        secondaryLineRenderer.positionCount = maxReflections + 1;
    }

    void Update()
    {

        if (enableBeam && !isBeamEnabled)
        {
            EnableLaser();
        }
        else if (!enableBeam && isBeamEnabled)
        {
            DisableLaser();
        }


        if (isBeamEnabled)
        {
            UpdateLaserWithDirectionAndReflection();
        }
    }

    void EnableLaser()
    {
        laserPrefab.SetActive(true);
        isBeamEnabled = true;
        primaryLineRenderer.enabled = true;
        secondaryLineRenderer.enabled = true;

        foreach (var t in startVfxps)
        {
            t.Play();
        }

        foreach (var t in endVfxps)
        {
            t.Play();
        }
    }

    void DisableLaser()
    {
        isBeamEnabled = false;
        primaryLineRenderer.enabled = false;
        secondaryLineRenderer.enabled = false;

        foreach (var t in startVfxps)
        {
            t.Stop();
        }

        foreach (var t in endVfxps)
        {
            t.Stop();
        }
    }

    // Método para obtener la dirección inicial del láser según la configuración
    Vector3 GetLaserDirection()
    {
        Vector3 direction;

        switch (fireDirection)
        {
            case FireDirection.Forward:
                direction = firePoint.forward;
                break;
            case FireDirection.Up:
                direction = firePoint.up;
                break;
            case FireDirection.Right:
                direction = firePoint.right;
                break;
            default:
                direction = firePoint.forward;
                break;
        }

        return direction;
    }

    // Método unificado que incluye la reflexión solo en capas reflectivas y detiene el láser en otras capas
    void UpdateLaserWithDirectionAndReflection()
    {
        Vector3 startPos = firePoint.position;
        Vector3 direction = GetLaserDirection();

        // Posiciona el VFX de inicio
        startVFX.transform.position = startPos;

        // Lista para almacenar las posiciones de los puntos de reflexión
        List<Vector3> laserPositions = new List<Vector3>();
        laserPositions.Add(startPos);

        // Inicializa variables para el cálculo de rebotes
        Vector3 currentPos = startPos;
        Vector3 currentDir = direction;

        // Calculamos la capa de colisión que incluye todo excepto las capas ignoradas
        int collisionMask = ~ignoreLayers.value;

        // Calcula los rebotes
        for (int i = 0; i < maxReflections; i++)
        {
            // Dibuja rayos de debug si está habilitado
            if (showDebugRays)
            {
                Debug.DrawRay(currentPos, currentDir * maxRayDistance, Color.red, Time.deltaTime);
            }

            // Usamos Physics.Raycast con la máscara que excluye las capas ignoradas
            RaycastHit hit;
            if (Physics.Raycast(currentPos, currentDir, out hit, maxRayDistance, collisionMask))
            {
                // Verificamos si el objeto golpeado está en una capa reflectiva
                bool isReflective = ((1 << hit.collider.gameObject.layer) & reflectiveLayers.value) != 0;

                // Añadimos el punto de impacto a las posiciones
                Vector3 hitPoint = hit.point;
                laserPositions.Add(hitPoint);

                if (isReflective)
                {
                    // Si es reflectivo, calculamos la dirección reflejada (rebote)
                    if (showDebugRays)
                    {
                        Debug.DrawRay(hitPoint, hit.normal, Color.green, Time.deltaTime);
                    }

                    currentDir = Vector3.Reflect(currentDir, hit.normal);
                    currentPos =
                        hitPoint + (currentDir * 0.01f); // Pequeño offset para evitar colisionar con el mismo punto
                }
                else
                {
                    // Si no es reflectivo, terminamos aquí
                    break;
                }
            }
            else
            {
                // Si no golpeamos nada, el rayo se extiende hasta su máxima distancia
                laserPositions.Add(currentPos + (currentDir * maxRayDistance));
                break;
            }
        }

        // Actualiza los LineRenderer con las nuevas posiciones
        UpdateLineRenderers(laserPositions, direction);

        // Posiciona el VFX de fin en el último punto del láser
        if (laserPositions.Count > 0)
        {
            endVFX.transform.position = laserPositions[laserPositions.Count - 1];
        }
    }

    // Nuevo método para actualizar ambos LineRenderer
    void UpdateLineRenderers(List<Vector3> positions, Vector3 direction)
    {
        // Actualiza el número de posiciones en ambos LineRenderer
        primaryLineRenderer.positionCount = positions.Count;
        secondaryLineRenderer.positionCount = positions.Count;

        // Calcula la dirección perpendicular para el desplazamiento de la segunda línea
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular.magnitude < 0.1f) // Si la dirección es casi paralela a Vector3.up
        {
            perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
        }

        // Aplicar posiciones a las líneas
        for (int i = 0; i < positions.Count; i++)
        {
            // Primera línea - posición exacta
            primaryLineRenderer.SetPosition(i, positions[i]);

            // Segunda línea - posición con desplazamiento perpendicular
            secondaryLineRenderer.SetPosition(i, positions[i] + perpendicular * secondaryLineOffset);
        }
    }


    void FillLists()
    {
        for (int i = 0; i < startVFX.transform.childCount; i++)
        {
            var ps = startVFX.transform.GetChild(i).GetComponent<ParticleSystem>();
            if (ps != null)
                startVfxps.Add(ps);
        }

        for (int i = 0; i < endVFX.transform.childCount; i++)
        {
            var ps = endVFX.transform.GetChild(i).GetComponent<ParticleSystem>();
            if (ps != null)
                endVfxps.Add(ps);
        }
    }

    public void EnableBeam()
    {
        enableBeam = true;
    }

    public void DisableBeam()
    {
        enableBeam = false;
    }
}