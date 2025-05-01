using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FireDirection
{
    Forward,
    Up,
    Right,
    ToTarget,
    ToMouse
}

public class Laser : MonoBehaviour
{
    public Camera cam;
    public LineRenderer lineRenderer;
    public Transform firePoint;
    public GameObject startVFX;
    public GameObject endVFX;
    public Transform targetTransform; // Transform objetivo para el láser

    [Header("Configuración de Dirección")]
    public FireDirection fireDirection = FireDirection.Forward; // Dirección predeterminada

    [Header("Configuración de Colisiones")]
    public int maxReflections = 5; // Número máximo de rebotes

    public float maxRayDistance = 100f; // Distancia máxima del rayo
    public LayerMask reflectiveLayers; // Capas que pueden reflejar el láser
    public LayerMask ignoreLayers; // Capas que serán ignoradas completamente

    [Header("Debug")] [SerializeField] private bool showDebugRays = true;

    [Header("Testing")] [SerializeField] private bool enableBeam;
    private bool isBeamEnabled;

    private Quaternion rotation;
    private List<ParticleSystem> startVFXPS = new List<ParticleSystem>();
    private List<ParticleSystem> endVFXPS = new List<ParticleSystem>();

    void Start()
    {
        FillLists();
        DisableLaser();

        // Aumenta el número de posiciones del LineRenderer para permitir rebotes
        lineRenderer.positionCount = maxReflections + 1;
    }

    void Update()
    {
        // Control de habilitación/deshabilitación del láser
        if (enableBeam && !isBeamEnabled)
        {
            EnableLaser();
        }
        else if (!enableBeam && isBeamEnabled)
        {
            DisableLaser();
        }

        // Solo actualizamos el láser si está habilitado
        if (isBeamEnabled)
        {
            UpdateLaserWithDirectionAndReflection();
        }
    }

    void EnableLaser()
    {
        isBeamEnabled = true;
        lineRenderer.enabled = true;

        for (int i = 0; i < startVFXPS.Count; i++)
        {
            startVFXPS[i].Play();
        }

        for (int i = 0; i < endVFXPS.Count; i++)
        {
            endVFXPS[i].Play();
        }
    }

    void DisableLaser()
    {
        isBeamEnabled = false;
        lineRenderer.enabled = false;

        for (int i = 0; i < startVFXPS.Count; i++)
        {
            startVFXPS[i].Stop();
        }

        for (int i = 0; i < endVFXPS.Count; i++)
        {
            endVFXPS[i].Stop();
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
            case FireDirection.ToTarget:
                if (targetTransform != null)
                {
                    direction = (targetTransform.position - firePoint.position).normalized;
                }
                else
                {
                    // Si no hay objetivo, usamos Forward por defecto
                    direction = firePoint.forward;
                }

                break;
            case FireDirection.ToMouse:
                Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                    firePoint.position.z));
                direction = (mouseWorldPos - firePoint.position).normalized;
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

        // Actualiza el LineRenderer con las nuevas posiciones
        lineRenderer.positionCount = laserPositions.Count;
        for (int i = 0; i < laserPositions.Count; i++)
        {
            lineRenderer.SetPosition(i, laserPositions[i]);
        }

        // Posiciona el VFX de fin en el último punto del láser
        if (laserPositions.Count > 0)
        {
            endVFX.transform.position = laserPositions[laserPositions.Count - 1];
        }
    }

    void RotateToMouse()
    {
        Vector3 mouseWorldPos =
            cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z));
        Vector3 direction = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle > 180) angle -= 360;
        rotation.eulerAngles = new Vector3(0, 0, angle);
        transform.rotation = rotation;
    }

    void FillLists()
    {
        for (int i = 0; i < startVFX.transform.childCount; i++)
        {
            var ps = startVFX.transform.GetChild(i).GetComponent<ParticleSystem>();
            if (ps != null)
                startVFXPS.Add(ps);
        }

        for (int i = 0; i < endVFX.transform.childCount; i++)
        {
            var ps = endVFX.transform.GetChild(i).GetComponent<ParticleSystem>();
            if (ps != null)
                endVFXPS.Add(ps);
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