using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private MMF_Player deathFeedback;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Lógica para cuando el enemigo muere
        DisableColliders();
        deathFeedback?.PlayFeedbacks();
        //Destroy(gameObject);
    }

    private void DisableColliders()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }
}
