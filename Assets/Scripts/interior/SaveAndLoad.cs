using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveAndLoad : MonoBehaviour {
	
	//variables visible in the inspector
	public int index;
	public GameObject character;
	public int saveAfterSeconds;
	//public bool saveOnQuit;
	
	[Space(15)]
	public float loadEffectTime;
	public float maxLoadDelayPerPiece;
	public float delayAfterBuildEffect;
	public bool showWindowsDoorsFurnitureLoading;
	
	//not visible in the inspector
	private uBuildManager managerScript;
	private GameObject doneSavingText;
	
	private Vector3[] positions;
	
	[HideInInspector]
	public bool doneLoading;

	private Vector3 center;
	
	void Awake(){
		if(!character){
			Debug.LogWarning("Please add a character to the SaveAndLoad script [uBuildManager object]");
			return;
		}

        //load all data (player position, buildings etc.)

        load();
    }
	
	void Start(){
		//save occasionally
		//InvokeRepeating("save", saveAfterSeconds, saveAfterSeconds);
	}
	
	void OnApplicationQuit(){
		//save when application quits or when leaving editor playmode
		//if(saveOnQuit){
		//	GameObject.Find("uBuildManager").GetComponent<SaveAndLoad>().save();
		//}
	}
	
	void storePositions(){
		int count = PlayerPrefs.GetInt(index + " - " + "piecesCount");
		positions = new Vector3[count];
		
		for(int i = 0; i < count; i++){
			positions[i] = new Vector3(PlayerPrefs.GetFloat(index + " - " + i + "p x"), PlayerPrefs.GetFloat(index + " - " + i + "p y"), PlayerPrefs.GetFloat(index + " - " + i + "p z"));
		}
	}
	
	void initializeBuildEffect(){
		//find uBuildManager
		managerScript = GetComponent<uBuildManager>();
		
		int count = PlayerPrefs.GetInt(index + " - " + "piecesCount");
		float greatestDistance = 0;
		
		for(int i = 0; i < count; i++){
			center += positions[i];
			
			for(int I = 0; I < count; I++){
				if(Vector3.Distance(positions[i], positions[I]) > greatestDistance)
					greatestDistance = Vector3.Distance(positions[i], positions[I]);
			}
		}
		
		center /= count;
		GameObject centerObject = new GameObject();
		centerObject.transform.position = center;
        if (Camera.main.GetComponent<FollowCharacter>())
        {
            Camera.main.GetComponent<FollowCharacter>().camTarget = centerObject.transform;
            Camera.main.GetComponent<FollowCharacter>().Height = greatestDistance * 1.5f;
        }
	}
	
    
	public void save(){
		if(!doneLoading)
			return;
		
		//check for buildmode
		if(uBuildManager.buildMode){
			//save buildmode state
			PlayerPrefs.SetInt(index + " - " + "build mode", 1);
			
			if(Camera.main.gameObject.transform.parent == null){
				//save camera position
				PlayerPrefs.SetFloat(index + " - " + "cam p x", Camera.main.gameObject.transform.position.x);
				PlayerPrefs.SetFloat(index + " - " + "cam p y", Camera.main.gameObject.transform.position.y);
				PlayerPrefs.SetFloat(index + " - " + "cam p z", Camera.main.gameObject.transform.position.z);
			
				//save camera rotation
				PlayerPrefs.SetFloat(index + " - " + "cam r x", Camera.main.gameObject.transform.localEulerAngles.x);
				PlayerPrefs.SetFloat(index + " - " + "cam r y", Camera.main.gameObject.transform.localEulerAngles.y);
				PlayerPrefs.SetFloat(index + " - " + "cam r z", Camera.main.gameObject.transform.localEulerAngles.z);
			}
			else{
				//save camera position (mobile)
				PlayerPrefs.SetFloat(index + " - " + "cam p x", Camera.main.gameObject.transform.parent.position.x);
				PlayerPrefs.SetFloat(index + " - " + "cam p y", Camera.main.gameObject.transform.parent.position.y);
				PlayerPrefs.SetFloat(index + " - " + "cam p z", Camera.main.gameObject.transform.parent.position.z);
				
				//save camera rotation
				PlayerPrefs.SetFloat(index + " - " + "cam r x", Camera.main.gameObject.transform.parent.localEulerAngles.x);
				PlayerPrefs.SetFloat(index + " - " + "cam r y", Camera.main.gameObject.transform.parent.localEulerAngles.y);
				PlayerPrefs.SetFloat(index + " - " + "cam r z", Camera.main.gameObject.transform.parent.localEulerAngles.z);
			}
		}
		else{
			//if not in buildmode, there's no need to save camera so just save buildmode state
			PlayerPrefs.SetInt(index + " - " + "build mode", 0);
		}
		
		//save furniture mode state
		if(uBuildManager.furnitureMode){
			PlayerPrefs.SetInt(index + " - " + "furniture mode", 1);
		}
		else{
			PlayerPrefs.SetInt(index + " - " + "furniture mode", 0);
		}
		
		//set all pieces active to find and save all of them
		for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
			foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
			piece.SetActive(true);
			}
		}
		
		//find all pieces (which have just been activated)
		GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");
		//get pieces count
		int piecesCount = pieces.Length;
		//save pieces count
		PlayerPrefs.SetInt(index + " - " + "piecesCount", piecesCount);
		
		//for all pieces...
		for(int i = 0; i < piecesCount; i++){
			//save piece position
			PlayerPrefs.SetFloat(index + " - " + i + "p x", pieces[i].transform.position.x);
			PlayerPrefs.SetFloat(index + " - " + i + "p y", pieces[i].transform.position.y);
			PlayerPrefs.SetFloat(index + " - " + i + "p z", pieces[i].transform.position.z);
			
			//save piece rotation
			PlayerPrefs.SetFloat(index + " - " + i + "r x", pieces[i].transform.localEulerAngles.x);
			PlayerPrefs.SetFloat(index + " - " + i + "r y", pieces[i].transform.localEulerAngles.y);
			PlayerPrefs.SetFloat(index + " - " + i + "r z", pieces[i].transform.localEulerAngles.z);
			
			//save piece type (very important, index + " - " in the list)
			PlayerPrefs.SetInt(index + " - " + i + "type", pieces[i].GetComponent<PieceTrigger>().type);
			//save the layer this piece is on
			PlayerPrefs.SetInt(index + " - " + i + "layer", pieces[i].GetComponent<PieceTrigger>().layer);
			
			//if piece is a door...
			if(pieces[i].transform.Find("Hinge") != null){
				//save the state of this door (open/closed)
				if(pieces[i].transform.Find("Hinge").gameObject.GetComponent<Door>().open){
				PlayerPrefs.SetInt(index + " - " + i + "door open", 1);
				}
				else{
				PlayerPrefs.SetInt(index + " - " + i + "door open", 0);	
				}
			}
		}
		
		//check for buildmode
		if(uBuildManager.buildMode){
			//for all layers...
			for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
				//turn pieces active when the layer is toggled on
				if(PlayerPrefs.GetInt(index + " - " + "layer" + i) == 0){
					foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
					piece.SetActive(true);
					}
				}
				//turn pieces not active when the layer is toggled off
				else{
					foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
					piece.SetActive(false);
					}
				}
			}
		}
		else{
			//if not in build mode, still loop through all layers
			for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
				//set all pieces active
				foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
				piece.SetActive(true);
				}
			}
		}
		
		//check if there is a character
		if(character != null){
			
		//save character position
		PlayerPrefs.SetFloat(index + " - " + "character p x", character.transform.position.x);
		PlayerPrefs.SetFloat(index + " - " + "character p y", character.transform.position.y);
		PlayerPrefs.SetFloat(index + " - " + "character p z", character.transform.position.z);
		
		//save character rotation
		PlayerPrefs.SetFloat(index + " - " + "character r x", character.transform.localEulerAngles.x);
		PlayerPrefs.SetFloat(index + " - " + "character r y", character.transform.localEulerAngles.y);
		PlayerPrefs.SetFloat(index + " - " + "character r z", character.transform.localEulerAngles.z);
		}
		
		//save the amount of layers
		PlayerPrefs.SetInt(index + " - " + "layerCount", GameObject.Find("uBuildManager").GetComponent<Layers>().layers.Count);
		//for all layers...
		for(int i = 0; i < GameObject.Find("uBuildManager").GetComponent<Layers>().layers.Count; i++){
			//get layer name
			string layerName = GameObject.Find("uBuildManager").GetComponent<Layers>().layers[i].name;
			//save layername
			PlayerPrefs.SetString(index + " - " + "L" + i + "N", layerName);
		}
		
		//save playerprefs
		PlayerPrefs.Save();
	}

    public void load(){		
		//add layers
		GetComponent<Layers>().addLayers();
		
		if(PlayerPrefs.GetInt(index + " - " + "piecesCount") != 0){
			storePositions();
			initializeBuildEffect();
		}
        StartCoroutine(loadPieces());
		
		//get character position and rotation and apply it to the character
	//	character.transform.position = new Vector3(PlayerPrefs.GetFloat(index + " - " + "character p x"), PlayerPrefs.GetFloat(index + " - " + "character p y"), PlayerPrefs.GetFloat(index + " - " + "character p z"));
	//	character.transform.localEulerAngles = new Vector3(PlayerPrefs.GetFloat(index + " - " + "character r x"), PlayerPrefs.GetFloat(index + " - " + "character r y"), PlayerPrefs.GetFloat(index + " - " + "character r z"));
	}
	
	IEnumerator loadPieces(){
		//get the amount of pieces to instantiate
		int piecesCount = PlayerPrefs.GetInt(index + " - " + "piecesCount");
		
		//check if there are saved pieces
		if(piecesCount > 0){
			//for each piece...
			for(int i = 0; i < piecesCount; i++){
				//get the piece type
				int pieceType = PlayerPrefs.GetInt(index + " - " + i + "type");
				//get piece layer
				int pieceLayer = PlayerPrefs.GetInt(index + " - " + i + "layer");
				//get piece rotation
				Quaternion pieceRotation = Quaternion.Euler(PlayerPrefs.GetFloat(index + " - " + i + "r x"), PlayerPrefs.GetFloat(index + " - " + i + "r y"), PlayerPrefs.GetFloat(index + " - " + i + "r z"));
				//get piece position
				Vector3 piecePosition = positions[i];

                //actually add the piece based on its type
                
                if(managerScript.pieces.Count <= pieceType || managerScript.pieces[pieceType] == null)
                {
                    Debug.Log("PieceType = " + pieceType);
                    continue;
                }
				GameObject loadedPiece = Instantiate(managerScript.pieces[pieceType].prefab, piecePosition, pieceRotation) as GameObject;
				//set piece type
				loadedPiece.GetComponent<PieceTrigger>().type = pieceType;
				//set piece layer
				loadedPiece.GetComponent<PieceTrigger>().layer = pieceLayer;
				
				//check if piece has a layer
				if(pieceLayer != 0){
					//add piece to the layer list
					GameObject.Find("uBuildManager").GetComponent<Layers>().layers[pieceLayer - 1].layerPieces.Add(loadedPiece);
				}
			
				//check if piece is a door
				if(loadedPiece.transform.Find("Hinge") != null){
					//check if door should be open and open/close it
					if(PlayerPrefs.GetInt(index + " - " + i + "door open") == 0){
						loadedPiece.transform.Find("Hinge").gameObject.GetComponent<Door>().open = false;
					}
					else{
						loadedPiece.transform.Find("Hinge").gameObject.GetComponent<Door>().open = true;
					}
				}
				
				//give piece the correct tag
				loadedPiece.tag = "Piece";
					
				if(loadEffectTime > 0 && (showWindowsDoorsFurnitureLoading || (!showWindowsDoorsFurnitureLoading && !(managerScript.pieces[pieceType].disableYSnapping || managerScript.pieces[pieceType].furniture))))
					yield return new WaitForSeconds((loadEffectTime/(float)piecesCount < maxLoadDelayPerPiece) ? loadEffectTime/(float)piecesCount : maxLoadDelayPerPiece);
			}
			
			yield return new WaitForSeconds(delayAfterBuildEffect);
		}
		
		doneLoading = true;
        if (Camera.main.GetComponent<FollowCharacter>())
            Camera.main.GetComponent<FollowCharacter>().camTarget = Camera.main.GetComponent<FollowCharacter>().player;
			
		//check buildmode
		if(PlayerPrefs.GetInt(index + " - " + "build mode") != 0){
			//set build mode to true
			//GetComponent<uBuildManager>().buildModeTrue();
			//GetComponent<uBuildManager>().changeCamera(false);
			
			//check furnituremode to see if it's active
			if(PlayerPrefs.GetInt(index + " - " + "furniture mode") == 0){
				uBuildManager.furnitureMode = false;
			}
			else{
				uBuildManager.furnitureMode = true;
			}
		}
			
		initializeLayers();
	}
	
	void initializeLayers(){
		Layers layers = GetComponent<Layers>();
		
		//for all layers...
		for(int i = 0; i < layers.layers.Count; i++){
			//get layer toggle
			Toggle toggle = layers.layers[i].layerUI.transform.Find("State").gameObject.GetComponent<Toggle>();
			//check if layer is on/off and apply it to the toggle
			if(PlayerPrefs.GetInt(index + " - " + "layer" + i) == 0){
				toggle.isOn = true;
			}
			else{
				toggle.isOn = false;
			}
		}
		
		//check for build mode
		if(uBuildManager.buildMode){
			//for all layers...
			for(int i = 0; i < layers.layers.Count; i++){
				//check if layer is on/off and set pieces on/off
				if(PlayerPrefs.GetInt(index + " - " + "layer" + i) == 0){
					foreach(GameObject piece in layers.layers[i].layerPieces){
					piece.SetActive(true);
					}
				}
				else{
					foreach(GameObject piece in layers.layers[i].layerPieces){
					piece.SetActive(false);
					}
				}
			}
			
			//get camera position and rotation
			if(!Camera.main.gameObject.transform.parent){
				Camera.main.gameObject.transform.position = new Vector3(PlayerPrefs.GetFloat(index + " - " + "cam p x"), PlayerPrefs.GetFloat(index + " - " + "cam p y"), PlayerPrefs.GetFloat(index + " - " + "cam p z"));
				Camera.main.gameObject.transform.localEulerAngles = new Vector3(PlayerPrefs.GetFloat(index + " - " + "cam r x"), PlayerPrefs.GetFloat(index + " - " + "cam r y"), PlayerPrefs.GetFloat(index + " - " + "cam r z"));
			}
			else{
				Camera.main.gameObject.transform.parent.position = new Vector3(PlayerPrefs.GetFloat(index + " - " + "cam p x"), PlayerPrefs.GetFloat(index + " - " + "cam p y"), PlayerPrefs.GetFloat(index + " - " + "cam p z"));
				Camera.main.gameObject.transform.parent.localEulerAngles = new Vector3(PlayerPrefs.GetFloat(index + " - " + "cam r x"), PlayerPrefs.GetFloat(index + " - " + "cam r y"), PlayerPrefs.GetFloat(index + " - " + "cam r z"));
			}
		}
		//if not in build mode
		else{
			//set all pieces active
			for(int i = 0; i < layers.layers.Count; i++){
				foreach(GameObject piece in layers.layers[i].layerPieces){
				piece.SetActive(true);
				}
			}
		}
	}
}
