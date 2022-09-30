using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ReadyPlayerMe;

public class CharacterColiderControl : MonoBehaviour
{

    // Start is called before the first frame update
    private float delayTime = 0;
    private GameObject furnitureObj = null;
    private GameObject mainBody;
    public bool stateFlag = true;
    private GameObject cameraObj;
    public GameObject interactObj = null;
    public string ownerFurnitureKey = "";

    void Start()
    {
        mainBody = transform.GetChild(0).gameObject;
        cameraObj = GameObject.Find("TrakingCamera");
    }

    /*
    public void Interactable(bool state, GameObject obj)
    {
        stateFlag = !state;
      
        if (stateFlag == false)
            mainBody.SetActive(false);
        else
        {
            if (furnitureObj != obj) mainBody.SetActive(false);
            else {
                if (cameraObj.GetComponent<vThirdPersonCamera>().firstPerson) mainBody.SetActive(false);
                else mainBody.SetActive(true);
            } 
        }
        furnitureObj = obj;
    }
    */

    public void Interactable(bool state, GameObject obj)
    {
        stateFlag = !state;
       
        if (stateFlag == false)
        {
            mainBody.SetActive(false);
            mainBody.transform.parent.gameObject.GetComponent<Rigidbody>().useGravity = false;
            mainBody.transform.parent.gameObject.GetComponent<CapsuleCollider>().enabled = false;
            GetComponent<PlayerController>().usingPieceKey = obj.GetComponent<FurniturePiece>().uniquePieceKey + "-sit";
            interactObj = obj;
        }
        else
        {
            if (!cameraObj.GetComponent<vThirdPersonCamera>().firstPerson)
            {
                mainBody.SetActive(true);
                mainBody.transform.parent.gameObject.GetComponent<CapsuleCollider>().enabled = true;
                mainBody.transform.parent.gameObject.GetComponent<Rigidbody>().useGravity = true;
                GetComponent<PlayerController>().usingPieceKey = obj.GetComponent<FurniturePiece>().uniquePieceKey + "-stand";
            }
            interactObj = null;
        }
    }

}

