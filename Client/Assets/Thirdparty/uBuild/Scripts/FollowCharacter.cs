using UnityEngine;
using System.Collections;

public class FollowCharacter : MonoBehaviour {

	//Variables visible in the inspector
	public Transform player;
    public float distance;
    public float height;
    public float heightDamping;
    public float rotationDamping;
	
	[Space(8), Header("Build effect:")]
	public float Distance;
    public float Height;
    public float HeightDamping;
    public float RotationDamping;
	
	[HideInInspector]
	public Transform camTarget;	 
	
	private float playerDistance;
	private float playerHeight;
	private float playerHeightDamping;
	private float playerRotationDamping;
	
	void Start(){
		playerDistance = distance;
		playerHeight = height;
		playerHeightDamping = heightDamping;
		playerRotationDamping = rotationDamping;
	}
	
	void Update(){
		if(!GameObject.FindObjectOfType<SaveAndLoad>().doneLoading){
			if(distance != Distance){
				distance = Distance;
				height = Height;
				heightDamping = HeightDamping;
				rotationDamping = RotationDamping;
			}
		}
		else{
			if(distance != playerDistance){
				distance = playerDistance;
				height = playerHeight;
				heightDamping = playerHeightDamping;
				rotationDamping = playerRotationDamping;
			}
		}
	}
 
    void LateUpdate(){
		//Check if the camera has a target to follow
        if(!camTarget)
            return;
		
		//Some private variables for the rotation and position of the camera
        float wantedRotationAngle = camTarget.eulerAngles.y;
        float wantedHeight = camTarget.position.y + height;
        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;
		
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
 
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);
 
        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
     
        transform.position = camTarget.position;
        transform.position -= currentRotation * Vector3.forward * distance;
		
		//Set camera postition
        transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);
		
		//Look at the character
        transform.LookAt(camTarget);
    }
}
