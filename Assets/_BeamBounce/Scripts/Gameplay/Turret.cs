using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Referencias")] [SerializeField]
    private Transform turretRotationPart; // La pieza que rota para apuntar

    [SerializeField] private Transform firePoint; // Punto desde donde salen los disparos

    [Header("Atributos")] [SerializeField] private float range = 10f; // Rango de detección
    [SerializeField] private float fireRate = 1f; // Disparos por segundo
    [SerializeField] private float rotationSpeed = 5f; // Velocidad de rotación
    [SerializeField] private LayerMask enemyLayer; // Capa de enemigos
    [SerializeField] private bool isFacingRight = true; // Indica si el modelo mira inicialmente hacia la derecha

    [Header("Proyectil")] [SerializeField] private GameObject bulletPrefab; // Prefab del proyectil
    [SerializeField] private float bulletSpeed = 20f; // Velocidad del proyectil

    private Transform currentTarget;
    private float fireCountdown = 0f;
    private Quaternion bulletRotationOffset;
    private bool canShoot;

    private void Start()
    {
        // Configurar offset de rotación para el proyectil si es necesario
        bulletRotationOffset = isFacingRight ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
    }

    private void Update()
    {
        if(!canShoot) return;
        // Buscar el enemigo más cercano
        FindNearestEnemy();

        // Si hay un objetivo, apuntar y disparar
        if (currentTarget != null)
        {
            // Rotar hacia el objetivo
            RotateTowardsTarget();

            // Disparar cuando sea el momento
            if (fireCountdown <= 0f)
            {
                Shoot();
                fireCountdown = 1f / fireRate;
            }

            fireCountdown -= Time.deltaTime;
        }
    }

    void FindNearestEnemy()
    {
        // Encontrar todos los enemigos en el rango
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, range, enemyLayer);

        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        // Buscar el más cercano
        foreach (Collider enemyCollider in enemiesInRange)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemyCollider.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemyCollider.transform;
            }
        }

        currentTarget = nearestEnemy;
    }

    void RotateTowardsTarget()
    {
        if (turretRotationPart == null) return;

        // Calcular dirección al objetivo
        Vector3 direction = currentTarget.position - turretRotationPart.position;
        direction.y = 0f; // Mantener la rotación solo en el plano horizontal

        // Calcular la rotación deseada
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // Aplicar compensación si el modelo mira inicialmente hacia la derecha
        if (isFacingRight)
        {
            // Compensar 90 grados
            lookRotation *= Quaternion.Euler(0, -90, 0);
        }

        // Interpolación para rotación suave
        Vector3 rotation = Quaternion.Lerp(turretRotationPart.rotation, lookRotation, Time.deltaTime * rotationSpeed)
            .eulerAngles;

        // Aplicar rotación
        if (isFacingRight)
        {
            // Solo actualizamos la rotación Y manteniendo el offset inicial
            turretRotationPart.rotation = Quaternion.Euler(0f, rotation.y, 0f);
        }
        else
        {
            // Sin compensación
            turretRotationPart.rotation = Quaternion.Euler(0f, rotation.y, 0f);
        }
    }

    void Shoot()
    {
        // Calcular la dirección correcta para el disparo
        Vector3 shootDirection;

        if (isFacingRight)
        {
            // Si mira a la derecha, la dirección del disparo debe compensarse
            // Usamos la dirección hacia el objetivo como base
            Vector3 targetDirection = currentTarget.position - firePoint.position;
            targetDirection.y = 0;
            shootDirection = targetDirection.normalized;
        }
        else
        {
            // Si no mira a la derecha, usamos la dirección del firePoint
            shootDirection = firePoint.forward;
        }

        // Instanciar proyectil
        GameObject bulletObject =
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation * bulletRotationOffset);
        Rigidbody bulletRigidbody = bulletObject.GetComponent<Rigidbody>();

        // Añadir velocidad al proyectil
        if (bulletRigidbody != null)
        {
            if (isFacingRight)
            {
                // Si la torreta mira a la derecha, ajustamos manualmente la dirección
                bulletRigidbody.velocity = shootDirection * bulletSpeed;
            }
            else
            {
                // Usamos la dirección del firePoint
                bulletRigidbody.velocity = firePoint.forward * bulletSpeed;
            }
        }

        // Destruir el proyectil después de cierto tiempo si no colisiona con nada
        Destroy(bulletObject, 5f);
    }

    // Método para visualizar el rango en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    public void SetCanShoot(bool shootEnabled)
    {
        canShoot = shootEnabled;
    }
}