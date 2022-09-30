using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvataControl : MonoBehaviour
{
    public void SetAnimType(int type)
    {
       GetComponent<Animator>().SetInteger("Stay", type);

        transform.position = transform.parent.position;
        transform.rotation = transform.parent.rotation;
    }
}
