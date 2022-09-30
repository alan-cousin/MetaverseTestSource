#define USE_LAYER
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public enum PieceType
{
    WALLPAPER,
    ONWALL,
    ONROOF,
    ONFLOOR,
    STUFF
}
//piece variables
[System.Serializable]
public class piece
{
    public GameObject prefab;
    public string name;
    public Sprite image;
    public bool floor;
    public PieceType type;
    public bool disableYSnapping;
    public bool furniture;
    public bool wallpaper;
    public string description;
    public bool requiresResources;
    public string resource;
    public float resourceAmount;

    //button UI of this piece
    [HideInInspector]
    public GameObject button;
}

//face, to detect floor snapping
public enum face
{
    none, up, down, east, west, north, south
}

public class uBuildManager : MonoBehaviour
{

    //variables visible in the inspector (under settings)
    public Color green;
    public Color red;
    public Color selected;
    public Color buttonHighlight;
    public GameObject button;
    public GameObject piecesList;
    public Behaviour[] characterScripts;
    public GameObject character;
    public string doorKey;

    public GameObject nftExplorer;

    //not visibible
    [HideInInspector]
    public List<piece> pieces;

    //variables visible in the inspector (under settings)

    public string buildModeKey;
    public string placeKey;
    public string switchFurnitureModeKey;

    [TextArea]
    public string helpTextDefault;
    [TextArea]
    public string helpTextPlacing;
    [TextArea]
    public string helpTextSelected;

    [Header("Mobile")]
    public Animator mobileButtons;
    public GameObject mobileSelectedButtons;
    public GameObject[] extraButtons;

    //not visibible
    private GameObject currentObject;
    private int selectedPiece = 0;
    private GameObject helpText;
    private face direction;
    //private GameObject doorLabel;
    private GameObject resourcesWarningLabel;
    private GameObject cameraObj;

    private float lastX;

    private int index;
    private Quaternion camRotation;

    public static bool buildMode = false;
    public static bool furnitureMode;
    public static uBuildManager Instance { get; set;}

    public bool isMouseClicked;
    public bool isPlacing;

    [HideInInspector]
    public GameObject pieceSelected;

    public bool textChatPlaying = false; 

    void Awake()
    {
        camRotation = Camera.main.gameObject.transform.localRotation;
        index = GameObject.FindObjectOfType<SaveAndLoad>().index;

        foreach (GameObject button in extraButtons)
        {
            button.SetActive(false);
        }
        Instance = this;
    }

    void Start()
    {
        //Find some objects and show the buttons to select pieces with
        helpText = GameObject.Find("help text");
        //doorLabel = GameObject.Find("Door open/close text");
        resourcesWarningLabel = GameObject.Find("Resources warning");
        resourcesWarningLabel.SetActive(false);//by mars
        cameraObj = GameObject.Find("TrakingCamera");
        addDefaultButtons();
        
        if (mobileButtons != null)
            mobileButtons.SetBool("build mode", buildMode);
    }
    void PlaceObject(RaycastHit f_hit)
    {
        Vector3 pos = Vector3.zero;
        Vector3 objectScale = currentObject.GetComponent<PieceTrigger>().scale;
        piece _sel_piece = pieces[selectedPiece];
        //if piece is floor...
        if (_sel_piece.floor)
        {
            //get face
            direction = GetHitFace(f_hit);

            //if there's a piece above this floor or underneath it, just move it normally
            if (direction == face.up || direction == face.down)
            {
                pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z);
            }
            else
            {
                //if object is not rotated, check where the other pieces are to move this floor accordingly
                if (currentObject.transform.rotation.y == 0 || currentObject.transform.rotation.y == -180 || currentObject.transform.rotation.y == 180)
                {
                    if (direction == face.north)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(0, 0, objectScale.z / 2);

                    if (direction == face.south)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(0, 0, -objectScale.z / 2);

                    if (direction == face.east)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(objectScale.x / 2, 0, 0);

                    if (direction == face.west)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(-objectScale.x / 2, 0, 0);
                }
                else
                {
                    //if object is rotated, still check where the other pieces are to move this floor accordingly
                    if (direction == face.north)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(0, 0, objectScale.x / 2);

                    if (direction == face.south)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(0, 0, -objectScale.x / 2);

                    if (direction == face.east)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(objectScale.z / 2, 0, 0);

                    if (direction == face.west)
                        pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z) + new Vector3(-objectScale.z / 2, 0, 0);
                }
            }
        }
        else
        {
            //if the current piece is not a floor, move it normally
            pos = new Vector3(f_hit.point.x, f_hit.point.y + objectScale.y / 2, f_hit.point.z);
        }

        //move piece with snapping
        pos -= Vector3.one;
        pos /= 0.5f;

        bool isWallpaper = pieces[currentObject.GetComponent<PieceTrigger>().type].wallpaper;

        if (!isWallpaper)
        {
            if (!pieces[currentObject.GetComponent<PieceTrigger>().type].disableYSnapping && !pieces[currentObject.GetComponent<PieceTrigger>().type].furniture)
            {
                //normally, use snapping for all directions
                pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
            }
            else if (pieces[currentObject.GetComponent<PieceTrigger>().type].disableYSnapping && !pieces[currentObject.GetComponent<PieceTrigger>().type].furniture)
            {
                //disable y snapping
                pos = new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
            }
        }
        else
        {
            if (currentObject.transform.rotation.y % 180 > 0.8f || currentObject.transform.rotation.y % 180 < -0.8f || currentObject.transform.rotation.y == 0)
            {
                pos = new Vector3(pos.x, Mathf.Round(pos.y), Mathf.Round(pos.z));
            }
            else
            {
                pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
            }
        }

        pos *= 0.5f;
        pos += Vector3.one;

        //apply position to current piece
        currentObject.transform.position = pos;

        GameObject closestWall = null;

        if (isWallpaper)
        {
            float closest = 0.1f;

            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("Piece"))
            {
                if (piece != currentObject && piece.name.Substring(0, 4) == "Wall" && Vector3.Distance(piece.transform.position, currentObject.transform.position) < closest)
                {
                    closest = Vector3.Distance(piece.transform.position, currentObject.transform.position);
                    closestWall = piece;
                }
            }
        }

        float yDistance = 0;

        if (closestWall != null)
            yDistance = Mathf.Abs(closestWall.transform.position.y - currentObject.transform.position.y);

        //if currentobject is not triggered by another object, make it green and placeable
        if (!currentObject.GetComponent<PieceTrigger>().triggered && (!isWallpaper || (isWallpaper && closestWall != null && yDistance < 0.1f)))
        {
            setRendererSettings(currentObject, null, green);

            //place piece on mouse click
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && mobileButtons == null)
            {
                StartCoroutine(place());
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended && mobileButtons != null)
            {
                if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && Input.mousePosition.x > lastX + 70)
                {
                    StartCoroutine(place());
                }
                else
                {
                    cancel();
                }
            }
        }
        else
        {
            //make it red when it's not placeable
            setRendererSettings(currentObject, null, red);

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended && mobileButtons != null)
                cancel();
        }

        //cancel placing when key is pressed
        if (Input.GetKey(KeyCode.R))
            cancel();

        //update piece rotation
        updateRotation();
    }
    void Update()
    {
        if (textChatPlaying || NftManager.Instance.nftBuildMode) return;
        if (EventSystem.current.IsPointerOverGameObject())
            cameraObj.GetComponent<CameraMovement>().zoomSpeed = 0;
        else 
            cameraObj.GetComponent<CameraMovement>().zoomSpeed = 150;

        if (!character)
        {
            Debug.LogWarning("Please add a character to the uBuildManager script [uBuildManager object -> settings panel]");
            return;
        }

        if (Input.GetMouseButtonDown(0))
            lastX = Input.mousePosition.x;

        //check if build mode key is pressed, mouse is not over UI and the remove layer warning is not active
        if (PlayerPrefs.GetInt("NftState") == 1 && !NftManager.Instance.nftBuildMode && Input.GetKeyDown(buildModeKey) && !EventSystem.current.IsPointerOverGameObject() && character.GetComponent<CharacterColiderControl>().stateFlag)
        {
            switchBuildMode();
        }
        //check if door label should be displayed
        checkDoorText();
     
        //check for buildmode
        if (buildMode)
        {
            //change help text
            updateHelpText();
            //third person camera mode is false and build camera mode is true
            changeCamera(false);

            if (NftManager.Instance.nftBuildMode == true) return;
            //if place key is pressed, instantiate new piece (selected one)
            if (Input.GetKeyDown(placeKey) && !isPlacing && !EventSystem.current.IsPointerOverGameObject() && (!pieces[selectedPiece].requiresResources
            || (pieces[selectedPiece].requiresResources && PlayerPrefs.GetFloat(index + " - " + pieces[selectedPiece].resource) >= pieces[selectedPiece].resourceAmount)))
            {
                createPiece();
            }
            else if (Input.GetKeyDown(placeKey) && !isPlacing && !EventSystem.current.IsPointerOverGameObject() && pieces[selectedPiece].requiresResources
            && PlayerPrefs.GetFloat(index + " - " + pieces[selectedPiece].resource) < pieces[selectedPiece].resourceAmount)
            {
                StartCoroutine(resourcesWarning(selectedPiece));
            }

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //ray from mouse position
            if (Physics.Raycast(ray, out hit))
            {
               // Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
               // Debug.Log("Did Hit");
            }
                //check if mouse is over object
                if (hit.collider != null)
                {
                    //check if we're placing a piece
                    if (isPlacing)
                    {
                        Vector3 pos = Vector3.zero;
                        Vector3 objectScale = currentObject.GetComponent<PieceTrigger>().scale;

                        //if piece is floor...
                        if (pieces[selectedPiece].floor)
                        {
                            print("piece is on the floor");
                            //get face
                            direction = GetHitFace(hit);

                            //if there's a piece above this floor or underneath it, just move it normally
                            if (direction == face.up || direction == face.down)
                            {
                                pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z);
                            }
                            else
                            {
                                //if object is not rotated, check where the other pieces are to move this floor accordingly
                                if (currentObject.transform.rotation.y == 0 || currentObject.transform.rotation.y == -180 || currentObject.transform.rotation.y == 180)
                                {
                                    if (direction == face.north)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, objectScale.z / 2);

                                    if (direction == face.south)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, -objectScale.z / 2);

                                    if (direction == face.east)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(objectScale.x / 2, 0, 0);

                                    if (direction == face.west)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(-objectScale.x / 2, 0, 0);
                                }
                                else
                                {
                                    //if object is rotated, still check where the other pieces are to move this floor accordingly
                                    if (direction == face.north)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, objectScale.x / 2);

                                    if (direction == face.south)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, -objectScale.x / 2);

                                    if (direction == face.east)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(objectScale.z / 2, 0, 0);

                                    if (direction == face.west)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(-objectScale.z / 2, 0, 0);
                                }
                            }
                        }
                        else
                        {
                            //if the current piece is not a floor, move it normally
                            pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2f, hit.point.z);
                        }

                        //move piece with snapping
                        pos -= Vector3.one;
                        pos /= 0.5f;

                        bool isWallpaper = pieces[currentObject.GetComponent<PieceTrigger>().type].wallpaper;

                        if (!isWallpaper)
                        {
                            
                            if (!pieces[currentObject.GetComponent<PieceTrigger>().type].disableYSnapping && !pieces[currentObject.GetComponent<PieceTrigger>().type].furniture)
                            {
                                //normally, use snapping for all directions
                                pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
                            }
                            else if (pieces[currentObject.GetComponent<PieceTrigger>().type].disableYSnapping && !pieces[currentObject.GetComponent<PieceTrigger>().type].furniture)
                            {
                                //disable y snapping
                                pos = new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
                            }
                        }
                        else
                        {
                            if (currentObject.transform.rotation.y % 180 > 0.8f || currentObject.transform.rotation.y % 180 < -0.8f || currentObject.transform.rotation.y == 0)
                            {
                                pos = new Vector3(pos.x, Mathf.Round(pos.y), Mathf.Round(pos.z));
                            }
                            else
                            {
                                pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
                            }
                        }

                        pos *= 0.5f;
                        pos += Vector3.one;

                        //apply position to current piece
                        currentObject.transform.position = pos;

                        GameObject closestWall = null;

                        if (isWallpaper)
                        {
                            float closest = 0.1f;

                            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("Piece"))
                            {
                                if (piece != currentObject && piece.name.Substring(0, 4) == "Wall" && Vector3.Distance(piece.transform.position, currentObject.transform.position) < closest)
                                {
                                    closest = Vector3.Distance(piece.transform.position, currentObject.transform.position);
                                    closestWall = piece;
                                }
                            }
                        }

                        float yDistance = 0;

                        if (closestWall != null)
                            yDistance = Mathf.Abs(closestWall.transform.position.y - currentObject.transform.position.y);

                        //if currentobject is not triggered by another object, make it green and placeable
                        if (!currentObject.GetComponent<PieceTrigger>().triggered && (!isWallpaper || (isWallpaper && closestWall != null && yDistance < 0.1f)))
                        {
                            setRendererSettings(currentObject, null, green);

                            //place piece on mouse click
                            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && mobileButtons == null)
                            {
                                isMouseClicked = true;
                                StartCoroutine(place());
                            }
                            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended && mobileButtons != null)
                            {
                                if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && Input.mousePosition.x > lastX + 70)
                                {
                                    StartCoroutine(place());
                                }
                                else
                                {
                                    cancel();
                                }
                            }
                            else
                            {
                                isMouseClicked = false;
                            }
                        }
                        else
                        {
                            //make it red when it's not placeable
                            setRendererSettings(currentObject, null, red);

                            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended && mobileButtons != null)
                                cancel();
                        }

                        //cancel placing when key is pressed
                        if (Input.GetKey(KeyCode.R))
                            cancel();

                        //update piece rotation
                        updateRotation();
                    }
                    else
                    {
                        if (mobileButtons == null)
                        {
                            if (Input.GetMouseButtonDown(0) && hit.collider.gameObject.CompareTag("Piece") && !EventSystem.current.IsPointerOverGameObject())
                            {
                                pieceSelected = hit.collider.gameObject;
                                //set piece color to selected color
                                setRendererSettings(hit.collider.gameObject, null, selected);
                            }
                        }
                        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                        {
                            //if not placing a piece, use mouse button to select pieces
                            if (hit.collider.gameObject.CompareTag("Piece"))
                            {
                                pieceSelected = hit.collider.gameObject;
                                //set piece color to selected color
                                setRendererSettings(hit.collider.gameObject, null, selected);
                            }
                            else
                            {
                                pieceSelected = null;
                            }
                        }
                    }
                    //if there is a selected piece
                    if (pieceSelected != null)
                        {
                            int type = pieceSelected.GetComponent<PieceTrigger>().type;

                    //check if we want to duplicate the piece
                    if (Input.GetKeyDown(KeyCode.LeftControl) && (!pieces[type].requiresResources
                            || (pieces[type].requiresResources && PlayerPrefs.GetFloat(index + " - " + pieces[type].resource) >= pieces[type].resourceAmount)))
                            {                                
                                PlayerPrefs.SetFloat(index + " - " + pieces[type].resource, PlayerPrefs.GetFloat(index + " - " + pieces[type].resource) - pieces[type].resourceAmount);

                                //instantiate selected piece to duplicate it
                                currentObject = Instantiate(pieceSelected, pieceSelected.transform.position, pieceSelected.transform.rotation) as GameObject;
                                //disable collider temporarily
                                currentObject.GetComponentInChildren<Collider>().enabled = false;
                                //make piece untagged
                                currentObject.tag = "Untagged";
                                if (currentObject.GetComponent<PieceTrigger>().layer != 0)
                                {
                                    //add the duplicate to it's layer
                                    GetComponent<Layers>().layers[currentObject.GetComponent<PieceTrigger>().layer - 1].layerPieces.Add(currentObject);
                                }
                                //if piece is door, disable door collider
                                if (currentObject.transform.Find("Hinge") != null)
                                {
                                    currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
                                }
                                //start placing duplicate
                                isPlacing = true;
                                pieceSelected = null;
                            }
                            else if (Input.GetKeyDown(KeyCode.LeftControl) && pieces[type].requiresResources
                            && PlayerPrefs.GetFloat(index + " - " + pieces[type].resource) < pieces[type].resourceAmount)
                            {
                                StartCoroutine(resourcesWarning(type));
                            }

                            //check if we want to move the piece
                            if (Input.GetKeyDown("m"))
                            {
                                //set piece to selected piece to move it
                                currentObject = pieceSelected;
                                //disable collider temporarily
                                currentObject.GetComponentInChildren<Collider>().enabled = false;
                                //if piece is door, disable door collider
                                if (currentObject.transform.Find("Hinge") != null)
                                {
                                    currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
                                }
                                //make piece untagged
                                currentObject.tag = "Untagged";
                                //start moving piece
                                isPlacing = true;
                                pieceSelected = null;
                            }
                            //check if we want to remove piece
                            if (Input.GetKey(KeyCode.R))
                                Delete();

                            //deselect piece with right mouse button
                            if (Input.GetMouseButtonDown(1))
                                pieceSelected = null;
                        }
                    }
                
            //switch furniture mode on key down   ---mantadreamer delete code 22.3.30---
            /*
            if (Input.GetKeyDown(switchFurnitureModeKey) && !EventSystem.current.IsPointerOverGameObject() && !Layers.removeLayerWarning.activeSelf)
            {
                switchButtons();
            }
            */
        }
        else
        {
            //if we're not in build mode, set camera to third person
            changeCamera(true);
        }

        if (currentObject != null)
        {
            //give current piece alpha shader to make it transparent
            setRendererSettings(currentObject, Shader.Find("Unlit/UnlitAlphaWithFade"), Color.black);
        }

        if (!isPlacing)
        {
            //if we're not placing a piece, check each piece
            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("Piece"))
            {
                if (piece == pieceSelected)
                {
                    //if this is the selected piece, give it the selected color and a transparent shader
                    setRendererSettings(piece, Shader.Find("Unlit/UnlitAlphaWithFade"), selected);
                }
                else
                {
                    //if this is not the selected piece, give it a white color and a diffuse shader			
                    setRendererSettings(piece, Shader.Find("Diffuse"), Color.white);
                }
            }
        }

        if (mobileButtons != null)
        {
            if (pieceSelected != null)
            {
                mobileSelectedButtons.SetActive(true);
            }
            else
            {
                mobileSelectedButtons.SetActive(false);
            }
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            showNFTExplorer(false);
        }
    }

    public void Delete()
    {
        pieceSelected.GetComponent<FurniturePiece>().DeleteFromDB();
        int type = pieceSelected.GetComponent<PieceTrigger>().type;
        PlayerPrefs.SetFloat(index + " - " + pieces[type].resource, PlayerPrefs.GetFloat(index + " - " + pieces[type].resource) + pieces[type].resourceAmount);

        //set piece to selected piece
        currentObject = pieceSelected;
        //remove piece from it's layer
        if (currentObject.GetComponent<PieceTrigger>().layer != 0)
        {
            GetComponent<Layers>().layers[currentObject.GetComponent<PieceTrigger>().layer - 1].layerPieces.Remove(currentObject);
        }

        //destroy current piece and set selected piece to null
        Destroy(currentObject);
        pieceSelected = null;
    }

    public void switchButtons()
    {
        furnitureMode = !furnitureMode;
        switchFurnitureMode(furnitureMode);
    }

    void createPiece()
    {
        currentObject = Instantiate(pieces[selectedPiece].prefab, Vector3.zero, Quaternion.identity) as GameObject;
        PlayerPrefs.SetFloat(index + " - " + pieces[selectedPiece].resource, PlayerPrefs.GetFloat(index + " - " + pieces[selectedPiece].resource) - pieces[selectedPiece].resourceAmount);

        //disable collider (temporarily) and set type and layer
        currentObject.GetComponentInChildren<Collider>().enabled = false;
        currentObject.GetComponent<PieceTrigger>().type = selectedPiece;
        currentObject.GetComponent<PieceTrigger>().layer = PlayerPrefs.GetInt(index + " - " + "defaultLayer");

        //add piece to the correct layer
        if (PlayerPrefs.GetInt(index + " - " + "defaultLayer") != 0)
        {
            GetComponent<Layers>().layers[PlayerPrefs.GetInt(index + " - " + "defaultLayer") - 1].layerPieces.Add(currentObject);
        }
        //if object is door, disable door collider
        if (currentObject.transform.Find("Hinge") != null)
        {
            currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
        }

        //start placing
        isPlacing = true;
        pieceSelected = null;
    }

    void updateRotation()
    {
        //if right mouse button gets pressed, rotate the piece 90 degrees
        //please do not change the rotation angle, this will disturb floor placement
        if ((mobileButtons != null && Input.GetMouseButtonDown(0) && Input.touchCount > 1) || Input.GetMouseButtonDown(1))
        {
            currentObject.transform.Rotate(Vector3.up, 90, Space.World);
        }
    }

    public void buildModeTrue()
    {
        //set build mode true
        buildMode = true;

        if (NftManager.Instance.nftBuildMode)
        {
            GameObject.Find("NFTCollectionList").GetComponent<Animator>().SetTrigger("Slide out");
            NftManager.Instance.nftBuildMode = false;
        }
        //show furniture placement
        GameObject.Find("FurnitureList").GetComponent<Animator>().SetTrigger("Slide in");
        textChatPlaying = false;
        ChatGUI.Instance.chatPanel.SetActive(false);

        foreach (GameObject button in extraButtons)
        {
            button.SetActive(true);
        }
    }

    public void buildModeFalse()
    {
        //set build mode false
        buildMode = false;

        //don't show build mode UI
        GameObject.Find("FurnitureList").GetComponent<Animator>().SetTrigger("Slide out");

        foreach (GameObject button in extraButtons)
        {
            button.SetActive(false);
        }
    }

    public void switchBuildMode()
    {
        Debug.Log("called switchBuildMode");
        //check if build mode key is pressed, mouse is not over UI and the remove layer warning is not active
#if USE_LAYER
        if (!Layers.removeLayerWarning.activeSelf && GameObject.FindObjectOfType<SaveAndLoad>().doneLoading)
#else
        if (GameObject.FindObjectOfType<SaveAndLoad>().doneLoading)
#endif
        {
            Debug.Log("buildMode in switchBuildMode is" + buildMode);
            //change buildmode
            if (!buildMode)
            {
                buildModeTrue();
                character.GetComponent<Rigidbody>().velocity = Vector3.zero;
                character.GetComponent<Animator>().SetFloat("InputMagnitude", 0);
            }
            else
            {
                if (!isMouseClicked && currentObject != null && isPlacing)
                {
                    Destroy(currentObject);
                    isPlacing = false;
                }
                buildModeFalse();
            }
        }

        if (mobileButtons != null)
            mobileButtons.SetBool("build mode", buildMode);
    }

    IEnumerator place()
    {
        Debug.Log("CheckPlace");
        //set piece color to white
        setRendererSettings(currentObject, null, Color.white);
        //enable piece collider
        currentObject.GetComponentInChildren<Collider>().enabled = true;
        //if piece is door, set door collider true
        if (currentObject.transform.Find("Hinge") != null)
        {
            currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = true;
        }

        //we're not placing anything anymore
        isPlacing = false;
        //wait a very small time
        yield return new WaitForSeconds(0.05f);
        //set piece tag to piece (also important to save it)
        currentObject.tag = "Piece";
        currentObject = null;
    }

    void cancel()
    {
        //remove piece from it's layer
        if (currentObject.GetComponent<PieceTrigger>().layer != 0)
        {
            GetComponent<Layers>().layers[currentObject.GetComponent<PieceTrigger>().layer - 1].layerPieces.Remove(currentObject);
        }
        //destroy piece
        Destroy(currentObject);
        //stop placing
        isPlacing = false;

        PlayerPrefs.SetFloat(index + " - " + pieces[selectedPiece].resource, PlayerPrefs.GetFloat(index + " - " + pieces[selectedPiece].resource) + pieces[selectedPiece].resourceAmount);
    }

    public void changeCamera(bool fp)
    {
        //if first person camera...
        if (fp)
        {
            //parent the camera to character, give it proper position & rotation and disable movement script

            if (Camera.main.GetComponent<FollowCharacter>())
                Camera.main.GetComponent<FollowCharacter>().enabled = true;

            if (Camera.main.GetComponent<CameraMovement>())
                Camera.main.GetComponent<CameraMovement>().enabled = false;


            if (Camera.main.GetComponent<vThirdPersonCamera>())
                Camera.main.GetComponent<vThirdPersonCamera>().enabled = true;

            if (GameObject.FindObjectOfType<SaveAndLoad>().doneLoading)
            {
                //set character scripts true so it can move around
                foreach (Behaviour script in characterScripts)
                {
                    script.enabled = true;
                }
            }
            else
            {
                //disable character script to make sure it won't move while building
                foreach (Behaviour script in characterScripts)
                {
                    script.enabled = false;
                }
            }
        }
        // camera movement while placing furniture
        else
        {
            if (Camera.main.GetComponent<vThirdPersonCamera>())
                Camera.main.GetComponent<vThirdPersonCamera>().enabled = false;

            if (Camera.main.GetComponent<CameraMovement>())
            {
                Camera.main.GetComponent<CameraMovement>().enabled = true;
            }
            else
            {
                Camera.main.gameObject.transform.localPosition = Vector3.zero;
                Camera.main.gameObject.transform.localRotation = camRotation;
            }


            if (Camera.main.GetComponent<FollowCharacter>())
                Camera.main.GetComponent<FollowCharacter>().enabled = false;

            //disable character script to make sure it won't move while building
            foreach (Behaviour script in characterScripts)
            {
                script.enabled = false;
            }
        }
    }

    void addDefaultButtons()
    {
        //for all piece types...
        for (int i = 0; i < pieces.Count; i++)
        {
            //add a button to the list of buttons
            GameObject newButton = Instantiate(button);
            RectTransform rectTransform = newButton.GetComponent<RectTransform>();
            rectTransform.SetParent(piecesList.transform, false);

            //set button outline
            newButton.GetComponent<Outline>().effectColor = buttonHighlight;

            //set the correct button sprite
            newButton.GetComponent<Image>().sprite = pieces[i].image;

            //only enable outline for the first button
            /*
            if (i == 0)
            {
                newButton.GetComponent<Outline>().enabled = true;
            }
            else
            {
                newButton.GetComponent<Outline>().enabled = false;
            } */

            //set piece description
            if (!pieces[i].requiresResources)
            {
                newButton.transform.Find("Description").gameObject.GetComponentInChildren<Text>().text = pieces[i].description;
            }
            else
            {
                newButton.transform.Find("Description").gameObject.GetComponentInChildren<Text>().text = pieces[i].description + "\n\n<b>Requires " + pieces[i].resourceAmount + " " + pieces[i].resource + "</b>";
            }

            //set button name to its position in the list(important for the button to work later on)
            newButton.name = "" + i;

            //show piece name
            newButton.GetComponentInChildren<Text>().text = "" + pieces[i].name;

            //this is the new button ui
            pieces[i].button = newButton;
        }

        //set furnituremode not active   ---mantadreamer delete code 22.3.30---
        switchFurnitureMode(true);  //furniture
    }

    //update the help text
    void updateHelpText()
    {
        //show normal help text
        if (pieceSelected == null && !isPlacing)
        {
            helpText.GetComponent<Text>().text = "" + helpTextDefault;
        }
        //show help text for placing a piece
        if (isPlacing)
        {
            helpText.GetComponent<Text>().text = "" + helpTextPlacing;
        }
        //show help text for selected piece
        if (pieceSelected != null)
        {
            helpText.GetComponent<Text>().text = "" + helpTextSelected;
        }
    }

    //return faces that were hit to place floors
    static face GetHitFace(RaycastHit hit)
    {
        Vector3 incoming = hit.normal - Vector3.up;
        //south
        if (incoming == new Vector3(0, -1, -1))
            return face.south;
        //north
        if (incoming == new Vector3(0, -1, 1))
            return face.north;
        //up
        if (incoming == new Vector3(0, 0, 0))
            return face.up;
        //down
        if (incoming == new Vector3(1, 1, 1))
            return face.down;
        //west
        if (incoming == new Vector3(-1, -1, 0))
            return face.west;
        //east
        if (incoming == new Vector3(1, -1, 0))
            return face.east;
        //no face
        return face.none;
    }

    void checkDoorText()
    {
        //display door label? (only when not in build mode)
        bool doorTextTrue = false;
        //check all doors
        foreach (Door door in GameObject.FindObjectsOfType(typeof(Door)) as Door[])
        {
            //if door is open/closeable set door label true
            if (door.possible)
            {
                doorTextTrue = true;
            }
        }

        /*
        if (!buildMode && doorTextTrue && GameObject.FindObjectOfType<SaveAndLoad>().doneLoading)
        {
            //set label active
            doorLabel.SetActive(true);

            //show proper text
            if (mobileButtons == null)
                doorLabel.GetComponentInChildren<Text>().text = "Press " + doorKey + " to open/close door";
        }
        else
        {
            //if buildmode or doors are out of reach, don't show door label
            doorLabel.SetActive(false);
        }
        */
    }

    public void checkDoors()
    {
        foreach (Door door in GameObject.FindObjectsOfType<Door>())
        {
            door.TryOpening();
        }
    }

    //switch between normal build mode & furniture mode
    void switchFurnitureMode(bool FurnitureMode)
    {
        //---mantadreamer delete code 22-3-31--- 
        //if (FurnitureMode)
        {
            //in furniture mode, only enable furniture buttons
            for (int i = 0; i < pieces.Count; i++)
            {
                  
                //if (pieces[i].furniture)
                //{
                    pieces[i].button.gameObject.SetActive(true);
                //}
                //else
                //{
                //    pieces[i].button.gameObject.SetActive(false);
                //}
            }

            //select first button after switching
            selectFirstPiece();
            //show the furniture label

            if (mobileButtons != null && GameObject.Find("furniture button label"))
                GameObject.Find("furniture button label").GetComponent<Text>().text = "Back";
        }
        /*
        else
        {
            //don't enable furniture buttons
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].furniture)
                {
                    GameObject.Find("pieces").transform.Find(i.ToString()).gameObject.SetActive(true);
                }
                else
                {
                    GameObject.Find("pieces").transform.Find(i.ToString()).gameObject.SetActive(false);
                }
            }

            //select first button after switching
            selectFirstPiece();
            //do not show furniture label
            furnitureLabel.SetActive(false);

            if (mobileButtons != null && GameObject.Find("furniture button label"))
                GameObject.Find("furniture button label").GetComponent<Text>().text = "Furniture";
        }
        */
    }

    void selectFirstPiece()
    {
        //first button that is currently active
        int firstActiveButton = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].button.gameObject.activeSelf)
            {
                //if this is the active button, set it to be the active one
                firstActiveButton = i;
                //break the loop since we've already got the active button
                break;
            }
        }
        //disable button outline...
        for (int i = 0; i < pieces.Count; i++)
        {
            pieces[i].button.GetComponent<Outline>().enabled = false;
        }
        //... and enable it for the first button
        pieces[firstActiveButton].button.gameObject.GetComponent<Outline>().enabled = true;
        //set the first active button to be the selected button
        selectedPiece = firstActiveButton;
    }

    //select a new piece
    public void selectPiece(int piece)
    {
        //disable all button outlines...
        for (int i = 0; i < pieces.Count; i++)
        {
            pieces[i].button.GetComponent<Outline>().enabled = false;
            pieces[i].button.GetComponent<Outline>().effectDistance = new Vector2(0, 0);
        }

        //... and enable it for the selected button
        pieces[piece].button.GetComponent<Outline>().enabled = true;
        pieces[piece].button.GetComponent<Outline>().effectDistance = new Vector2(2.5f, -2.5f);
        //set selected piece to piece
        selectedPiece = piece;

        if (mobileButtons != null && !isPlacing)
        {
            if ((!pieces[selectedPiece].requiresResources
            || (pieces[selectedPiece].requiresResources && PlayerPrefs.GetFloat(index + " - " + pieces[selectedPiece].resource) >= pieces[selectedPiece].resourceAmount)))
            {
                createPiece();
            }
            else if (pieces[selectedPiece].requiresResources
            && PlayerPrefs.GetFloat(index + " - " + pieces[selectedPiece].resource) < pieces[selectedPiece].resourceAmount)
            {
                StartCoroutine(resourcesWarning(selectedPiece));
            }
        }
    }

    void setRendererSettings(GameObject piece, Shader shader, Color color)
    {
        foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>())
        {
            if (shader != null && color != Color.black)
            {
                renderer.material.color = color;
                //renderer.material.shader = shader;
            }
            else if (shader != null)
            {
               // renderer.material.shader = shader;
            }
            else
            {
                renderer.material.color = color;
            }
        }
    }

    IEnumerator resourcesWarning(int i)
    {
        resourcesWarningLabel.GetComponent<Text>().text = "Requires " + pieces[i].resourceAmount + " " + pieces[i].resource + "!";
        resourcesWarningLabel.SetActive(true);
        yield return new WaitForSeconds(1);
        resourcesWarningLabel.SetActive(false);
    }

    public GameObject m_showNFT = null;        
    public void showNFTExplorer(bool bShowFalse, GameObject showObj = null)
    {
        m_showNFT = showObj;
        nftExplorer.SetActive(bShowFalse);
    }
    public void onFurnitureButtonClick()
    {
        Debug.Log("called onFurnitureButtonClick");
        if (PlayerPrefs.GetInt("NftState") == 1)
        {
            switchBuildMode();
        }
    }
}