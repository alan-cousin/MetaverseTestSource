using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvataIntent : MonoBehaviour
{
    public void ReceivedAvata(GameObject avatar)
    {
        avatar.transform.position = transform.GetChild(0).GetChild(0).position;
        avatar.transform.rotation = transform.GetChild(0).GetChild(0).rotation;
        Destroy(transform.GetChild(0).GetChild(0).gameObject);
        avatar.transform.SetParent(transform.GetChild(0).transform);
        if (gameObject.tag == "ReadyAvatar")
        {
            PlayerPrefs.SetInt("loading", 1);
            Debug.Log("Received Avatar");
        }
        else
        {
            gameObject.GetComponent<Animator>().avatar = avatar.GetComponent<Animator>().avatar;
            gameObject.GetComponent<Animator>().runtimeAnimatorController = avatar.GetComponent<Animator>().runtimeAnimatorController;
            if (gameObject.tag == "OtherPlayer") avatar.GetComponent<Animator>().enabled = false; 
        }
    }
}
