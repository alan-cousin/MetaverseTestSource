using Invector.vCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PieceTrigger : MonoBehaviour
{

    //visible in the inspector
    public Vector3 scale;
    public enum FurnitureType { Bed, Chair, Watch, Door, GameSite, None }

    //not visible in the inspector
    [HideInInspector]
    public bool triggered;
    [HideInInspector]
    public int type;
    [HideInInspector]
    public int layer;
    public FurnitureType pieceType;
    public GameObject mainBodyPrefab;  //newly spawned character's prefab 
    public GameObject videoPlane;
    public string gameName;

    private GameObject _character;  //player = character 
    private GameObject _mainBody;   //newly spawned character
    private GameObject _videoPane;
    private GameObject _player;

    private bool _showFlag = true;   //if flag is true, main character is shown, else second character is shown
    private bool _isShownNotify = false;   //if notify appears, it's true else false 
    private float _delayTime = 0;
    private bool _doorOpen = false;
    private GameObject _notify = null;

    void Start()
    {
        _notify = GameObject.Find("AlertNotify");
        _character = null;
    }
    void Update()
    {
        if (_delayTime < 0.2f) return;
        if (_player == null || (pieceType != FurnitureType.Door && _player.GetComponent<CharacterColiderControl>().ownerFurnitureKey != "" && _player.GetComponent<CharacterColiderControl>().ownerFurnitureKey != GetComponent<FurniturePiece>().uniquePieceKey)) return;
        if (Input.GetKey(KeyCode.E))
        {
            if (_character != null && pieceType == FurnitureType.Watch)
            {
                if (transform.childCount == 1)
                {
                    _videoPane = Instantiate(videoPlane, transform.position, transform.rotation) as GameObject;
                    _videoPane.transform.SetParent(transform);
                    _player.GetComponent<PlayerController>().usingPieceKey = GetComponent<FurniturePiece>().uniquePieceKey + "-watchon";
                }
                else

                {
                    Destroy(transform.GetChild(1).gameObject);
                    _player.GetComponent<PlayerController>().usingPieceKey = GetComponent<FurniturePiece>().uniquePieceKey + "-watchoff";
                }
            }
            else if (_character != null && pieceType == FurnitureType.Door)
            {
                if (!_doorOpen)
                {
                    GetComponent<Animator>().SetTrigger(tag + "_in");
                    _doorOpen = true;
                    _player.GetComponent<PlayerController>().usingPieceKey = tag + "-doorin";
                }
                else
                {
                    GetComponent<Animator>().SetTrigger(tag + "_out");
                    _doorOpen = false;
                    _player.GetComponent<PlayerController>().usingPieceKey = tag + "-doorout";
                }
            }
            else if(_character != null && pieceType == FurnitureType.GameSite)
            {
                //game is not exist    
            }
            else if (_character != null && _showFlag == true && transform.GetChild(1).childCount == 0)
            {
                _showFlag = false;
                _notify.GetComponent<Animator>().SetTrigger("Slide out");
                _isShownNotify = false;

                GameObject originObj = Instantiate(mainBodyPrefab) as GameObject;
                _mainBody = Instantiate(_character.transform.GetChild(0).GetChild(0).gameObject, transform.GetChild(1).position, transform.GetChild(1).rotation);
                _mainBody.GetComponent<Animator>().runtimeAnimatorController = originObj.GetComponent<Animator>().runtimeAnimatorController;
                _mainBody.transform.SetParent(transform.GetChild(1));
                _mainBody.GetComponent<Animator>().enabled = true;
                Destroy(originObj);
                _mainBody.AddComponent<AvataControl>();
                _mainBody.GetComponent<AvataControl>().SetAnimType((int)pieceType);

                _player.GetComponent<CharacterColiderControl>().Interactable(true, this.gameObject);
            }
            else if (!_showFlag && pieceType != FurnitureType.Watch && _player.GetComponent<PlayerController>().IsMine)
            {
                _showFlag = true;
                Destroy(_mainBody);

                _player.GetComponent<CharacterColiderControl>().Interactable(false, this.gameObject);
            }
            _delayTime = 0;
        }
    }

    void FixedUpdate()
    {
        if (_delayTime > 10) _delayTime = 0;
        _delayTime += Time.deltaTime;
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

    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.GetComponent<PlayerController>().IsMine) return;
        if (col.gameObject.tag == "Player" && this.pieceType != FurnitureType.None && _character == null)
        {
            _player = _character = col.gameObject;
            if(GetComponent<FurniturePiece>() != null)
                _player.GetComponent<CharacterColiderControl>().ownerFurnitureKey = GetComponent<FurniturePiece>().uniquePieceKey;
        }
        
        if(_notify == null) _notify = GameObject.Find("AlertNotify");
        string addStr = string.Empty;
        switch (pieceType)
        {
            case FurnitureType.Bed:
                addStr = "Press Key 'E' to lay in bed";
                break;
            case FurnitureType.Chair:
                addStr = "Press Key 'E' to seat on the chair";
                break;
            case FurnitureType.Watch:
                if (transform.childCount == 1)
                    addStr = "Press Key 'E' to turn on TV";
                else
                    addStr = "Press Key 'E' to turn off TV";
                break;
            case FurnitureType.GameSite:
                addStr = "Press Key 'E' to play the game";
                break;
            case FurnitureType.Door:
                if (!_doorOpen)
                    addStr = "Press Key 'E' to open the door";
                else
                    addStr = "Press Key 'E' to close the door";
                break;
        }
        _notify.GetComponent<Text>().text = addStr;
        _notify.GetComponent<Animator>().SetTrigger("Slide in");
        _isShownNotify = true;
    }

    void OnCollisionExit(Collision col)
    {
        if (pieceType == FurnitureType.None) return;
        _character = null;
        _player.GetComponent<CharacterColiderControl>().ownerFurnitureKey = "";
        if (!_isShownNotify && !col.gameObject.GetComponent<PlayerController>().IsMine) return;
        _notify.GetComponent<Animator>().SetTrigger("Slide out");
    }

    void OnDestroy()
    {
        if (_isShownNotify && _notify != null)
        {
            _notify.GetComponent<Animator>().SetTrigger("Slide out");
        }
    }
}
