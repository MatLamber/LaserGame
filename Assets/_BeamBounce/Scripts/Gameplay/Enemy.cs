using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private bool useLocalDirection = true;
    
    [Header("Collision Settings")]
    [SerializeField] private LayerMask ignoreLayers; // Capas que serán ignoradas completamente
    [SerializeField] private bool showDebugRays = false;
    
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    
    private void Awake()
    {
        // Obtener los componentes necesarios
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        // Configurar el Rigidbody para movimiento constante
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Configurar el Rigidbody para ignorar colisiones con ciertas capas
        ConfigureCollisionIgnore();
    }
    
    private void ConfigureCollisionIgnore()
    {
        // Recorrer todas las capas y configurar las que deben ser ignoradas
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & ignoreLayers.value) != 0)
            {
                // Ignorar colisiones con esta capa
                Physics.IgnoreLayerCollision(gameObject.layer, i, true);
            }
        }
    }
    
    private void FixedUpdate()
    {
        MoveInDirection();
        
        // Dibujar rayos de debug si está habilitado
        if (showDebugRays)
        {
            Vector3 directionToMove = GetMovementDirection();
            Debug.DrawRay(transform.position, directionToMove * 2f, Color.red);
        }
    }
    
    private Vector3 GetMovementDirection()
    {
        return useLocalDirection 
            ? transform.TransformDirection(moveDirection.normalized) 
            : moveDirection.normalized;
    }
    
    private void MoveInDirection()
    {
        Vector3 directionToMove = GetMovementDirection();
            
        // Aplicar velocidad constante, sin importar colisiones
        rb.velocity = directionToMove * moveSpeed;
    }
    
    // Método opcional para interactuar con objetos que no ignoramos
    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si la capa del objeto con el que colisionamos está en nuestra LayerMask de ignorados
        if (((1 << collision.gameObject.layer) & ignoreLayers.value) == 0)
        {
            // Solo procesamos las colisiones con objetos que NO estamos ignorando
            // Puedes agregar aquí lógica específica si necesitas hacer algo al colisionar
            // con ciertos objetos, como dañar al jugador, activar efectos, etc.
        }
    }
    
    // Método público para cambiar la dirección externamente si es necesario
    public void SetDirection(Vector3 newDirection, bool useLocal = true)
    {
        moveDirection = newDirection.normalized;
        useLocalDirection = useLocal;
    }


    public void StopMovement()
    {
        moveSpeed = 0;
    }
}