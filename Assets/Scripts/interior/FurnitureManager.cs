using Colyseus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;


[Serializable]
public class FurnitureResponse
{
    public PieceData[] pieceList;
}

public class FurnitureManager : MonoBehaviour
{
    // Start is called before the first frame update
    private PieceData[] pieceList; 

    private uBuildManager managerScript;

    public float loadEffectTime;
    public float maxLoadDelayPerPiece;
    public float delayAfterBuildEffect;
    public bool showWindowsDoorsFurnitureLoading;

    [HideInInspector]
    public bool doneLoading;

    [HideInInspector]
    private Vector3 center;

    [HideInInspector]
    public GameObject[] doors = new GameObject[3];

    public static FurnitureManager Instance { get; private set; }

    void Start()
    {
        Load();
        managerScript = GameObject.Find("uBuildManager").GetComponent<uBuildManager>();

        for(int i = 0; i < 3; i++)
            doors[i] = GameObject.Find("Door" + (i + 1));

        Instance = this;
    }
    //set furnitures transform.
    public void PutFurniture(PieceData fData)
    {
        int pieceType = fData.pieceType;
        //get piece layer
        int pieceLayer = fData.pieceLayer;
        //get piece rotation
        Quaternion pieceRotation = Quaternion.Euler(fData.rotX, fData.rotY, fData.rotZ);
        //get piece position
        Vector3 piecePosition = new Vector3(fData.posX, fData.posY, fData.posZ);

        //actually add the piece based on its type

        GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");
        GameObject loadedPiece;

        int index = -1;
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].GetComponent<FurniturePiece>().uniquePieceKey == fData.pieceKey)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
            loadedPiece = Instantiate(managerScript.pieces[pieceType].prefab, piecePosition, pieceRotation) as GameObject;
        else loadedPiece = pieces[index];

        loadedPiece.transform.localPosition = piecePosition;
        loadedPiece.transform.rotation = pieceRotation;

        //set piece type
        loadedPiece.GetComponent<PieceTrigger>().type = pieceType;
        //set piece layer
        loadedPiece.GetComponent<PieceTrigger>().layer = pieceLayer;

        loadedPiece.GetComponent<FurniturePiece>().uniquePieceKey = fData.pieceKey;

        //check if piece has a layer
        if (pieceLayer != 0)
        {
            //add piece to the layer list
            GameObject.Find("uBuildManager").GetComponent<Layers>().layers[pieceLayer - 1].layerPieces.Add(loadedPiece);
        }

        //give piece the correct tag
        loadedPiece.tag = "Piece";
    }

    //load all furnitures from backend 
    private async void Load()
    {
        string roomId = LobbyController.Instance.myRoomId;

        Dictionary<string, object> reqData = new Dictionary<string, object>();
        reqData.Add("roomId", roomId);

        string response =
               await ColyseusRequest.Request("POST", $"api/furniture/load", reqData, null);

        string str = "{\"pieceList\":" + response + "}";
        FurnitureResponse resF = JsonUtility.FromJson<FurnitureResponse>(str);
        pieceList = resF.pieceList;
        if (pieceList.Length != 0) InitializeBuildEffect();
        StartCoroutine(LoadPieces());
    }

    private void InitializeBuildEffect()
    {
        float greatestDistance = 0;

        for (int i = 0; i < pieceList.Length; i++)
        {
            center += new Vector3(pieceList[i].posX, pieceList[i].posY, pieceList[i].posZ);

            for (int j = 0; j < pieceList.Length; j++)
            {
                Vector3 pos1 = new Vector3(pieceList[i].posX, pieceList[i].posY, pieceList[i].posZ);
                Vector3 pos2 = new Vector3(pieceList[j].posX, pieceList[j].posY, pieceList[j].posZ);
                if (Vector3.Distance(pos1, pos2) > greatestDistance)
                    greatestDistance = Vector3.Distance(pos1, pos2);
            }
        }

        center /= pieceList.Length;
        GameObject centerObject = new GameObject();
        centerObject.transform.position = center;
        if (Camera.main.GetComponent<FollowCharacter>())
        {
            Camera.main.GetComponent<FollowCharacter>().camTarget = centerObject.transform;
            Camera.main.GetComponent<FollowCharacter>().Height = greatestDistance * 1.5f;
        }
    }

    IEnumerator LoadPieces()
    {
        for (int i = 0; i < pieceList.Length; i++)
        {
            //get the piece type
            int pieceType = pieceList[i].pieceType;
            //get piece layer
            int pieceLayer = pieceList[i].pieceLayer;
            //get piece rotation
            Quaternion pieceRotation = Quaternion.Euler(pieceList[i].rotX, pieceList[i].rotY, pieceList[i].rotZ);
            //get piece position
            Vector3 piecePosition = new Vector3(pieceList[i].posX, pieceList[i].posY, pieceList[i].posZ);
                
            //actually add the piece based on its type

            if (managerScript.pieces.Count <= pieceType || managerScript.pieces[pieceType] == null)
            {
                Debug.Log("PieceType = " + pieceType);
                continue;
            }
            GameObject loadedPiece = Instantiate(managerScript.pieces[pieceType].prefab, piecePosition, pieceRotation) as GameObject;
            //set piece type
            loadedPiece.GetComponent<PieceTrigger>().type = pieceType;
            //set piece layer
            loadedPiece.GetComponent<PieceTrigger>().layer = pieceLayer;

            loadedPiece.GetComponent<FurniturePiece>().uniquePieceKey = pieceList[i].pieceKey;
                
            //check if piece has a layer
            if (pieceLayer != 0)
            {
                //add piece to the layer list
                GameObject.Find("uBuildManager").GetComponent<Layers>().layers[pieceLayer - 1].layerPieces.Add(loadedPiece);
            }

            //give piece the correct tag
            loadedPiece.tag = "Piece";

            if (loadEffectTime > 0 && (showWindowsDoorsFurnitureLoading || (!showWindowsDoorsFurnitureLoading && !(managerScript.pieces[pieceType].disableYSnapping || managerScript.pieces[pieceType].furniture))))
                yield return new WaitForSeconds((loadEffectTime / (float)pieceList.Length < maxLoadDelayPerPiece) ? loadEffectTime / (float)pieceList.Length : maxLoadDelayPerPiece);
                

            yield return new WaitForSeconds(delayAfterBuildEffect);
        }

        doneLoading = true;
        if (Camera.main.GetComponent<FollowCharacter>())
            Camera.main.GetComponent<FollowCharacter>().camTarget = Camera.main.GetComponent<FollowCharacter>().player;

        initializeLayers();
    }
    void initializeLayers()
    {
        Layers layers = GameObject.Find("uBuildManager").GetComponent<Layers>();

        //set all pieces active
        for (int i = 0; i < layers.layers.Count; i++)
        {
            foreach (GameObject piece in layers.layers[i].layerPieces)
            {
                piece.SetActive(true);
            }
        }
    }
}
