using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Atributos")] [SerializeField] private int damage = 20;
    [SerializeField] private float explosionRadius = 0f; // 0 = sin explosión, >0 = radio de explosión
    [SerializeField] private GameObject impactEffect; // Efecto visual de impacto (opcional)

    [Header("Configuración")] [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField] private bool destroyOnImpact = true;

    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si colisiona con un enemigo
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            // Aplicar daño directo si no hay explosión
            if (explosionRadius <= 0)
            {
                ApplyDamage(collision.gameObject);
            }
            else
            {
                // Aplicar daño por explosión
                Explode();
            }
        }

        // Crear efecto de impacto
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, transform.position, transform.rotation);
            Destroy(effect, 2f); // Destruir el efecto después de 2 segundos
        }

        // Destruir el proyectil al impactar
        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }

    void ApplyDamage(GameObject target)
    {
        // Intenta obtener el componente Health o similar en el enemigo
        Health health = target.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Alternativa si no usas un componente Health
        // Puedes enviar un mensaje al objeto para que maneje el daño a su manera
        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    void Explode()
    {
        // Encuentra todos los objetos en el radio de explosión
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);

        foreach (Collider collider in colliders)
        {
            // Aplica daño a cada objeto dentro del radio
            ApplyDamage(collider.gameObject);
        }
    }

    // Visualizar el radio de explosión en el editor (si existe)
    private void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}