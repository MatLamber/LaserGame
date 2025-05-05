using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalTween : MonoBehaviour
{
     [SerializeField] private float targetVerticalPosition;

     public void Tween()
     {
          LeanTween.moveLocalY(gameObject, targetVerticalPosition, 0.3f).setEaseOutBounce().setDelay(0.53f);
     }
}
