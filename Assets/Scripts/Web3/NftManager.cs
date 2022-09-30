#define USE_LAYER
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AllArt.Solana.Nft;

//piece variables
[System.Serializable]
public class NftPiece
{
    public GameObject prefab;
    public Nft m_nft;
    public bool floor;
    public string tokenKey;
    public bool disableYSnapping;
    public bool furniture;
    public bool wallpaper;
    public string description;
    public bool requiresResources;
    public string resource;
    public float resourceAmount;
}

public class NftManager : MonoBehaviour
{
    public Color green;
    public Color red;
    public Color selected;
    public Color buttonHighlight;

    public GameObject button;
    public GameObject piecesList;
    public GameObject character;
    public string doorKey;

    //not visibible
    //[HideInInspector]
    public List<NftPiece> pieces;

    //variables visible in the inspector (under settings)
    //public string buildModeKey;
    public string placeKey;
    public string switchFurnitureModeKey;
    public bool nftBuildMode = false;

    [TextArea]
    public string helpTextDefault;
    [TextArea]
    public string helpTextPlacing;
    [TextArea]
    public string helpTextSelected;


    //not visibible
    private GameObject _currentObject;
    private bool _isPlacing;
    private int _selectedPiece = 0;
    private GameObject _helpText;
    private face _direction;
    private GameObject _doorLabel;
    private GameObject _cameraObj;

    private float _lastX;

    private int _index;
    private Quaternion _camRotation;

    public GameObject nftPanel;
    public static bool furnitureMode;
    public bool isMouseClicked;
    public bool textChatPlaying = false;

    [HideInInspector]
    public GameObject pieceSelected;

    public static NftManager Instance { get; set; }

    void Awake()
    {
        _camRotation = Camera.main.gameObject.transform.localRotation;
        _index = GameObject.FindObjectOfType<SaveAndLoad>().index;
    }

    void Start()
    {
        //Find main camera
        _cameraObj = GameObject.Find("TrakingCamera");

        Instance = this;
    }

    void Update()
    {
        if (textChatPlaying) return;

        if (EventSystem.current.IsPointerOverGameObject())
            _cameraObj.GetComponent<CameraMovement>().zoomSpeed = 0;
        else
            _cameraObj.GetComponent<CameraMovement>().zoomSpeed = 150;

        if (!character || uBuildManager.buildMode)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
            _lastX = Input.mousePosition.x;

        if (PlayerPrefs.GetInt("NftState") == 1 && Input.GetKeyDown(KeyCode.LeftControl) && !EventSystem.current.IsPointerOverGameObject())
        {
            nftBuildMode = !nftBuildMode;

            ToggleNFTCollectionView();
        }

        if (nftBuildMode)
        {
            uBuildManager.Instance.changeCamera(false);

            //if you press placeKey, a new piece gameobject is created.
            if (Input.GetKeyDown(placeKey) && !_isPlacing && !EventSystem.current.IsPointerOverGameObject() && (!pieces[_selectedPiece].requiresResources
            || (pieces[_selectedPiece].requiresResources && PlayerPrefs.GetFloat("nft" + _index + " - " + pieces[_selectedPiece].resource) >= pieces[_selectedPiece].resourceAmount)))
            {
                CreatePiece();
            }

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //ray from mouse position
            if (Physics.Raycast(ray, out hit))

                //check if mouse is over object
                if (hit.collider != null)
                {
                    //check if we're placing a piece
                    if (_isPlacing)
                    {
                        Vector3 pos = Vector3.zero;
                        Vector3 objectScale = _currentObject.GetComponent<NFTPrefab>().scale;

                        //if piece is floor...
                        if (pieces[_selectedPiece].floor)
                        {
                            print("piece is on the floor");
                            //get face
                            _direction = GetHitFace(hit);

                            //if there's a piece above this floor or underneath it, just move it normally
                            if (_direction == face.up || _direction == face.down)
                            {
                                pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z);
                            }
                            else
                            {
                                //if object is not rotated, check where the other pieces are to move this floor accordingly
                                if (_currentObject.transform.rotation.y == 0 || _currentObject.transform.rotation.y == -180 || _currentObject.transform.rotation.y == 180)
                                {
                                    if (_direction == face.north)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, objectScale.z / 2);

                                    if (_direction == face.south)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, -objectScale.z / 2);

                                    if (_direction == face.east)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(objectScale.x / 2, 0, 0);

                                    if (_direction == face.west)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(-objectScale.x / 2, 0, 0);
                                }
                                else
                                {
                                    //if object is rotated, still check where the other pieces are to move this floor accordingly
                                    if (_direction == face.north)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, objectScale.x / 2);

                                    if (_direction == face.south)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(0, 0, -objectScale.x / 2);

                                    if (_direction == face.east)
                                        pos = new Vector3(hit.point.x, hit.point.y + objectScale.y / 2, hit.point.z) + new Vector3(objectScale.z / 2, 0, 0);

                                    if (_direction == face.west)
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

                        bool isWallpaper = pieces[_currentObject.GetComponent<NFTPrefab>().type].wallpaper;

                        if (!isWallpaper)
                        {

                            if (!pieces[_currentObject.GetComponent<NFTPrefab>().type].disableYSnapping && !pieces[_currentObject.GetComponent<NFTPrefab>().type].furniture)
                            {
                                //normally, use snapping for all directions
                                pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
                            }
                            else if (pieces[_currentObject.GetComponent<NFTPrefab>().type].disableYSnapping && !pieces[_currentObject.GetComponent<NFTPrefab>().type].furniture)
                            {
                                //disable y snapping
                                pos = new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
                            }
                        }
                        else
                        {
                            if (_currentObject.transform.rotation.y % 180 > 0.8f || _currentObject.transform.rotation.y % 180 < -0.8f || _currentObject.transform.rotation.y == 0)
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
                        _currentObject.transform.position = pos;

                        GameObject closestWall = null;

                        if (isWallpaper)
                        {
                            float closest = 0.1f;

                            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("Nft"))
                            {
                                if (piece != _currentObject && piece.name.Substring(0, 4) == "Wall" && Vector3.Distance(piece.transform.position, _currentObject.transform.position) < closest)
                                {
                                    closest = Vector3.Distance(piece.transform.position, _currentObject.transform.position);
                                    closestWall = piece;
                                }
                            }
                        }

                        float yDistance = 0;

                        if (closestWall != null)
                            yDistance = Mathf.Abs(closestWall.transform.position.y - _currentObject.transform.position.y);

                        //if currentobject is not triggered by another object, make it green and placeable
                        if (!_currentObject.GetComponent<NFTPrefab>().triggered && (!isWallpaper || (isWallpaper && closestWall != null && yDistance < 0.1f)))
                        {
                            setRendererSettings(_currentObject, null, green);

                            //place piece on mouse click
                            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                            {
                                isMouseClicked = true;
                                StartCoroutine(place());
                            }
                            else
                            {
                                isMouseClicked = false;
                            }
                        }
                        else
                        {
                            //make it red when it's not placeable
                            setRendererSettings(_currentObject, null, red);
                        }

                        //cancel placing when key is pressed
                        if (Input.GetKey(KeyCode.R))
                            cancel();

                        //update piece rotation
                        updateRotation();
                    }
                    else
                    {
                        if (Input.GetMouseButtonDown(0) && hit.collider.gameObject.CompareTag("Nft") && !EventSystem.current.IsPointerOverGameObject())
                        {
                            pieceSelected = hit.collider.gameObject;
                            //set piece color to selected color
                            setRendererSettings(hit.collider.gameObject, null, selected);
                        }

                        //if there is a selected piece

                        if (pieceSelected != null)
                        {
                            int type = pieceSelected.GetComponent<NFTPrefab>().type;

                            //check if we want to duplicate the piece
                            if (Input.GetKeyDown(KeyCode.LeftControl) && (!pieces[type].requiresResources
                            || (pieces[type].requiresResources && PlayerPrefs.GetFloat("nft" + _index + " - " + pieces[type].resource) >= pieces[type].resourceAmount)))
                            {

                                PlayerPrefs.SetFloat("nft" + _index + " - " + pieces[type].resource, PlayerPrefs.GetFloat("nft" + _index + " - " + pieces[type].resource) - pieces[type].resourceAmount);

                                //instantiate selected piece to duplicate it
                                _currentObject = Instantiate(pieceSelected, pieceSelected.transform.position, pieceSelected.transform.rotation) as GameObject;
                                //make piece untagged
                                _currentObject.tag = "Untagged";
                                if (_currentObject.GetComponent<NFTPrefab>().layer != 0)
                                {
                                    //add the duplicate to it's layer
                                    GetComponent<Layers>().layers[_currentObject.GetComponent<NFTPrefab>().layer - 1].layerPieces.Add(_currentObject);
                                }
                                //if piece is door, disable door collider
                                if (_currentObject.transform.Find("Hinge") != null)
                                {
                                    _currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
                                }
                                //start placing duplicate
                                _isPlacing = true;
                                pieceSelected = null;
                            }

                            //check if we want to move the piece
                            if (Input.GetKeyDown("m"))
                            {
                                //set piece to selected piece to move it
                                _currentObject = pieceSelected;
                                //disable collider temporarily
                                _currentObject.GetComponentInChildren<Collider>().enabled = false;
                                //if piece is door, disable door collider
                                if (_currentObject.transform.Find("Hinge") != null)
                                {
                                    _currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
                                }
                                //make piece untagged
                                _currentObject.tag = "Untagged";
                                //start moving piece
                                _isPlacing = true;
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
                }

        }
        else
        {
            uBuildManager.Instance.changeCamera(true);
        }

        if (_currentObject != null)
        {
            //give current piece alpha shader to make it transparent
            setRendererSettings(_currentObject, Shader.Find("Unlit/UnlitAlphaWithFade"), Color.black);
        }

        if (!_isPlacing)
        {
            //if we're not placing a piece, check each piece
            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("Nft"))
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


    }

    public void Delete()
    {
        int type = pieceSelected.GetComponent<NFTPrefab>().type;
        PlayerPrefs.SetFloat("nft" + _index + " - " + pieces[type].resource, PlayerPrefs.GetFloat("nft" + _index + " - " + pieces[type].resource) + pieces[type].resourceAmount);

        //set piece to selected piece
        _currentObject = pieceSelected;
        //remove piece from it's layer
        if (_currentObject.GetComponent<NFTPrefab>().layer != 0)
        {
            GameObject.Find("uBuildManager").GetComponent<Layers>().layers[_currentObject.GetComponent<NFTPrefab>().layer - 1].layerPieces.Remove(_currentObject);
        }

        //destroy current piece and set selected piece to null
        Destroy(_currentObject);
        pieceSelected = null;
    }

    public void SwitchButtons()
    {
        furnitureMode = !furnitureMode;
        switchFurnitureMode(furnitureMode);
    }

    void CreatePiece()
    {
        _currentObject = Instantiate(pieces[_selectedPiece].prefab, Vector3.zero, Quaternion.identity) as GameObject;
        _currentObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().sprite = Sprite.Create(pieces[_selectedPiece].m_nft.metaplexData.nftImage.file, new Rect(0, 0, pieces[_selectedPiece].m_nft.metaplexData.nftImage.file.width, pieces[_selectedPiece].m_nft.metaplexData.nftImage.file.height), new Vector2(0.5f, 0.5f));
        _currentObject.GetComponent<NFTPrefab>().tokenKey = pieces[_selectedPiece].tokenKey;

        PlayerPrefs.SetFloat(_index + " - " + pieces[_selectedPiece].resource, PlayerPrefs.GetFloat("nft" + _index + " - " + pieces[_selectedPiece].resource) - pieces[_selectedPiece].resourceAmount);

        //disable collider (temporarily) and set type and layer
        _currentObject.GetComponentInChildren<Collider>().enabled = false;
        _currentObject.GetComponent<NFTPrefab>().layer = PlayerPrefs.GetInt("nft" + _index + " - " + "defaultLayer");

        //add piece to the correct layer
        if (PlayerPrefs.GetInt("nft" + _index + " - " + "defaultLayer") != 0)
        {
            GameObject.Find("uBuildManager").GetComponent<Layers>().layers[PlayerPrefs.GetInt("nft" + _index + " - " + "defaultLayer") - 1].layerPieces.Add(_currentObject);
        }
        //if object is door, disable door collider
        if (_currentObject.transform.Find("Hinge") != null)
        {
            _currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
        }

        //start placing
        _isPlacing = true;
        pieceSelected = null;
    }

    void updateRotation()
    {
        //if right mouse button gets pressed, rotate the piece 90 degrees
        if (Input.GetMouseButtonDown(1)) _currentObject.transform.Rotate(Vector3.up, 90, Space.World);
    }

    IEnumerator place()
    {
        //set piece color to white
        setRendererSettings(_currentObject, null, Color.white);
        //enable piece collider
        _currentObject.GetComponentInChildren<Collider>().enabled = true;
        //if piece is door, set door collider true
        if (_currentObject.transform.Find("Hinge") != null)
        {
            _currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = true;
        }

        //we're not placing anything anymore
        _isPlacing = false;
        //wait a very small time
        yield return new WaitForSeconds(0.05f);
        //set piece tag to piece (also important to save it)
        _currentObject.tag = "Nft";
        _currentObject = null;
    }

    void cancel()
    {
        //remove piece from it's layer
        if (_currentObject.GetComponent<NFTPrefab>().layer != 0)
        {
            GetComponent<Layers>().layers[_currentObject.GetComponent<NFTPrefab>().layer - 1].layerPieces.Remove(_currentObject);
        }
        //destroy piece
        Destroy(_currentObject);
        //stop placing
        _isPlacing = false;

        PlayerPrefs.SetFloat("nft" + _index + " - " + pieces[_selectedPiece].resource, PlayerPrefs.GetFloat("nft" + _index + " - " + pieces[_selectedPiece].resource) + pieces[_selectedPiece].resourceAmount);
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
            if (incoming == new Vector3(1, -1, 0))
                return face.east;
        //no face
        return face.none;
    }

    void CheckDoorText()
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
        //if (FurnitureMode)
        {
            //select first button after switching
            SelectFirstPiece();
        }
    }

    public void SelectFirstPiece()
    {
        //first button that is currently active
        int firstActiveButton = 0;
        _selectedPiece = firstActiveButton;
    }

    //select a new piece
    public void selectPiece(int piece)
    {
        _selectedPiece = piece;

        if (!_isPlacing)
        {
            if ((!pieces[_selectedPiece].requiresResources
            || (pieces[_selectedPiece].requiresResources && PlayerPrefs.GetFloat("nft" + _index + " - " + pieces[_selectedPiece].resource) >= pieces[_selectedPiece].resourceAmount)))
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                    CreatePiece();
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
            }
            else
            {
                renderer.material.color = color;
            }
        }
    }

    public void OnNftButtonClicked()
    {
        if (PlayerPrefs.GetInt("NftState") == 1)
        {
            nftBuildMode = !nftBuildMode;

            ToggleNFTCollectionView();
        }
    }

    public void ToggleNFTCollectionView()
    {
        if (nftBuildMode) character.GetComponent<Animator>().SetFloat("InputMagnitude", 0);

        if (nftBuildMode)
        {
            if (uBuildManager.buildMode)
            {
                GameObject.Find("FurnitureList").GetComponent<Animator>().SetTrigger("Slide out");
                uBuildManager.buildMode = false;
            }
            nftPanel.GetComponent<Animator>().SetTrigger("Slide in");
            ChatGUI.Instance.chatPanel.SetActive(false);
            textChatPlaying = false;
        }
        else
        {
            nftPanel.GetComponent<Animator>().SetTrigger("Slide out");
            if (!isMouseClicked && _currentObject != null && _isPlacing)
            {
                Destroy(_currentObject);
                _isPlacing = false;
            }
        }
    }
}
