using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NFTPrefab : MonoBehaviour
{
    public Vector3 scale;
    [HideInInspector]
    public int layer;
    [HideInInspector]
    public bool triggered;
    float delayTime = 0;
    [HideInInspector]
    public int type;
    public string tokenKey;
    public enum FurnitureType { Picture, None }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        if (delayTime > 10) delayTime = 0;
        delayTime += Time.deltaTime;
        //normaly piece is not triggered
        triggered = false;
    }

    //when a collider stays in the trigger, the piece is triggered and not placeable
    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag != "Ground" && !other.isTrigger && !other.gameObject.transform.IsChildOf(transform))
        {
            triggered = true;
        }
    }
}
