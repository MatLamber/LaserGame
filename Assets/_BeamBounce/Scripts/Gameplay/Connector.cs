using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public class Connector : MonoBehaviour
{

    [SerializeField] private MMF_Player onHitFeedback;
    [SerializeField] private MMF_Player onIdleFeedback;
    [SerializeField] private Turret turret;

    
    public void PlayOnHitFeedback()
    {
        onHitFeedback?.PlayFeedbacks();
        turret.SetCanShoot(true);
    }
    public void PlayOnIdleFeedback()
    {
        onIdleFeedback?.PlayFeedbacks();
        turret.SetCanShoot(false);
    }

    public void AddEmision()
    {
        GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.3124381f, 1, 0, 1) * 1.5f );
    }
    
    public void RemoveEmision()
    {
        GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
    }
}
