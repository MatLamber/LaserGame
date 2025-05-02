using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventsManager : MonoBehaviour
{
    
    
    public static EventsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public event Action<bool> ActionOnDragging;
    
    public void OnDragging(bool isDragging) => ActionOnDragging?.Invoke(isDragging);
}
