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
    [Foldout("Laser Settings")] [SerializeField] private LayerMask connectorLayer;
    [Foldout("Laser Settings")] [SerializeField] private LayerMask enemyLayers; // Nueva capa para identificar enemigos
    [Foldout("Laser Settings")] [SerializeField] private int laserPower = 10; // Poder del láser (daño por segundo)
    
    [BoxGroup("Testing & Debug")] [SerializeField] private bool showDebugRays = true;
    [BoxGroup("Testing & Debug")] [SerializeField] private bool enableBeam;

    
    private Camera mainCamera;
    private bool isBeamEnabled;
    private List<ParticleSystem> endVfxps = new List<ParticleSystem>();
    private List<ParticleSystem> startVfxps = new List<ParticleSystem>();
    private Connector lastConnectorHit;
    private Health lastEnemyHit; // Referencia al último enemigo alcanzado
    private float damageTimer; // Temporizador para aplicar daño por segundo
    private bool hasAppliedInitialDamage; // Flag para verificar si ya se aplicó el daño inicial
    #endregion


    void Start()
    {
        FillLists();
        primaryLineRenderer.positionCount = maxReflections + 1;
        secondaryLineRenderer.positionCount = maxReflections + 1;
        damageTimer = 0f;
        hasAppliedInitialDamage = false;
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
            
            // Aplicar daño a los enemigos
            if (lastEnemyHit != null)
            {
                // Aplicar daño instantáneo en el primer impacto
                if (!hasAppliedInitialDamage)
                {
                    lastEnemyHit.TakeDamage(laserPower);
                    hasAppliedInitialDamage = true;
                    damageTimer = 0f; // Reiniciar el temporizador después del primer impacto
                }
                // Luego aplicar daño por segundo
                else
                {
                    damageTimer += Time.deltaTime;
                    if (damageTimer >= 1f)
                    {
                        lastEnemyHit.TakeDamage(laserPower);
                        damageTimer = 0f;
                    }
                }
            }
            else
            {
                // Si ya no golpeamos al enemigo, reiniciar variables
                damageTimer = 0f;
                hasAppliedInitialDamage = false;
            }
        }
    }

    void EnableLaser()
    {
        laserPrefab.SetActive(true);
        isBeamEnabled = true;
        primaryLineRenderer.enabled = true;
        secondaryLineRenderer.enabled = true;

        StartLaserStartVfx();
        StartLaserEndVfx();
        
        // Resetear variables de daño al activar el láser
        hasAppliedInitialDamage = false;
        damageTimer = 0f;
    }

    private void StartLaserEndVfx()
    {
        foreach (var t in endVfxps)
        {
            t.Play();
        }
    }

    private void StartLaserStartVfx()
    {
        foreach (var t in startVfxps)
        {
            t.Play();
        }
    }

    void DisableLaser()
    {
        isBeamEnabled = false;
        primaryLineRenderer.enabled = false;
        secondaryLineRenderer.enabled = false;

        StopLaserStartVfx();
        StopLaserEndVfx();
        
        // Resetear referencia al enemigo al desactivar el láser
        lastEnemyHit = null;
        hasAppliedInitialDamage = false;
    }

    private void StopLaserEndVfx()
    {
        foreach (var t in endVfxps)
        {
            t.Stop();
        }
    }

    private void StopLaserStartVfx()
    {
        foreach (var t in startVfxps)
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

        // Variable para rastrear si el láser impacta con un connector
        bool hitConnector = false;
        
        // Resetear la referencia al enemigo en cada frame
        Health currentEnemyHit = null;
        bool hitEnemy = false;

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
                // Verificamos si el objeto golpeado está en la capa connector
                bool isConnector = ((1 << hit.collider.gameObject.layer) & connectorLayer.value) != 0;
                
                // Verificamos si el objeto golpeado está en la capa de enemigos
                bool isEnemy = ((1 << hit.collider.gameObject.layer) & enemyLayers.value) != 0;

                // Si golpeamos un connector, actualizamos la bandera
                if (isConnector)
                {
                    if (lastConnectorHit == null)
                    {
                        lastConnectorHit = hit.collider.gameObject.GetComponent<Connector>();
                        lastConnectorHit.PlayOnHitFeedback();
                    }

                    hitConnector = true;
                    // Detener el efecto visual del final cuando impacta con el connector
                    StopLaserEndVfx();
                }
                else
                {
                    if (lastConnectorHit != null)
                    {
                        Debug.Log($"Last Connector Disconnect: {lastConnectorHit.name}");
                        lastConnectorHit.PlayOnIdleFeedback();
                        lastConnectorHit = null;
                    }
                }
                
                // Si golpeamos un enemigo, obtenemos su componente Health
                if (isEnemy)
                {
                    hitEnemy = true;
                    currentEnemyHit = hit.collider.gameObject.GetComponent<Health>();
                    
                    // Si es un nuevo enemigo (diferente al último), debemos aplicar daño inicial
                    if (lastEnemyHit != currentEnemyHit)
                    {
                        hasAppliedInitialDamage = false;
                    }
                }

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
                if (lastConnectorHit != null)
                {
                    Debug.Log($"Last Connector Disconnect: {lastConnectorHit.name}");
                    lastConnectorHit.PlayOnIdleFeedback();
                    lastConnectorHit = null;
                }
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

        // Si no golpeamos un connector, aseguramos que el efecto visual del final esté activo
        if (!hitConnector)
        {
            StartLaserEndVfx();
        }
        
        // Actualizar la referencia al enemigo golpeado
        if (!hitEnemy)
        {
            // Si perdimos contacto con cualquier enemigo, reseteamos las variables
            if (lastEnemyHit != null)
            {
                lastEnemyHit = null;
                hasAppliedInitialDamage = false;
            }
        }
        else
        {
            lastEnemyHit = currentEnemyHit;
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