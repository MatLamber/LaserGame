using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissolverEffectManager : MonoBehaviour
{
    [SerializeField] private List<SpriteRenderer> spriteRenderers;
    private List<Material> materials = new List<Material>();

    private int dissolveAmount = Shader.PropertyToID("_DissolveAmount");


    private void Start()
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            materials.Add(sr.material);
        }
    }

    public void Vanish()
    {
        LeanTween.value(gameObject, 0f, 1f, 0.5f)
            .setOnUpdate((float value) =>
            {
                foreach (Material material in materials)
                {
                    material.SetFloat(dissolveAmount, value);
                }
            });
    }
    
}
