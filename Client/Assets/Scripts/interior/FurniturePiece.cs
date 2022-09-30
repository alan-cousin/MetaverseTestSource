using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Colyseus;
using System;

[Serializable]

//structure for management of furniure
public class PieceData
{
    public string id;
    public string pieceKey;
    public string roomId;
    public float posX;
    public float posY;
    public float posZ;
    public float rotX;
    public float rotY;
    public float rotZ;
    public int pieceType;
    public int pieceLayer;
}

//manage each furnitures
public class FurniturePiece : MonoBehaviour
{
    public string uniquePieceKey = "";
    //public bool saveOnQuit;
    private GameObject buildManager;

    void Start()
    {
        buildManager = GameObject.Find("uBuildManager");    
    }
    bool isOnceSave = false;
    void Update()
    {
        if (PlayerPrefs.GetInt("NftState") == 1)
        {
            if (!buildManager.GetComponent<uBuildManager>().isMouseClicked && buildManager.GetComponent<uBuildManager>().isPlacing)
            {
                isOnceSave = true;
            }
            if (buildManager.GetComponent<uBuildManager>().isMouseClicked && isOnceSave == true)
            {
                SaveStateToDB();
                isOnceSave = false;
            }
        }
    }

    //save furniture data to backend
    private async void SaveStateToDB()
    {
        if(uniquePieceKey == "")
            uniquePieceKey = System.DateTime.Now.Year.ToString() +
                             System.DateTime.Now.Month.ToString() +
                             System.DateTime.Now.Day.ToString() +
                             System.DateTime.Now.Hour.ToString() +
                             System.DateTime.Now.Minute.ToString() +
                             System.DateTime.Now.Second.ToString();
       
        Dictionary<string, object> saveData = new Dictionary<string, object>();
        saveData.Add("posX", gameObject.transform.position.x);
        saveData.Add("posY", gameObject.transform.position.y);
        saveData.Add("posZ", gameObject.transform.position.z);
        saveData.Add("rotX", gameObject.transform.localEulerAngles.x);
        saveData.Add("rotY", gameObject.transform.localEulerAngles.y);
        saveData.Add("rotZ", gameObject.transform.localEulerAngles.z);
        saveData.Add("pieceKey", uniquePieceKey);
        saveData.Add("roomId", LobbyController.Instance.myRoomId);
        saveData.Add("pieceType", GetComponent<PieceTrigger>().type);
        saveData.Add("pieceLayer", GetComponent<PieceTrigger>().layer);

        //------------------------communication
        PieceData sendData = new PieceData();
        sendData.pieceKey = saveData["pieceKey"].ToString();
        sendData.posX = gameObject.transform.position.x;
        sendData.posY = gameObject.transform.position.y;
        sendData.posZ = gameObject.transform.position.z;
        sendData.rotX = gameObject.transform.localEulerAngles.x;
        sendData.rotY = gameObject.transform.localEulerAngles.y;
        sendData.rotZ = gameObject.transform.localEulerAngles.z;
        sendData.pieceType = GetComponent<PieceTrigger>().type;
        sendData.pieceLayer = GetComponent<PieceTrigger>().layer;
        sendData.roomId = saveData["roomId"].ToString();
        ExampleManager.SendFurnitureData(sendData);
        //--------------------------------------

        var json =
            await ColyseusRequest.Request("POST", $"api/furniture/save", saveData, null);

        Debug.Log(json);
    }

    //delete from backend(mongose)
    public async void DeleteFromDB()
    {
        Dictionary<string, object> reqData = new Dictionary<string, object>();
        reqData.Add("pieceKey", uniquePieceKey);
        reqData.Add("roomId", LobbyController.Instance.myRoomId);
        string json =
           await ColyseusRequest.Request("POST", $"api/furniture/remove", reqData, null);
    }
}
