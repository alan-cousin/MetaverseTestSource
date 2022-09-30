using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileRotate : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler {
	
	//variables visible in the inspector
	public Image background;
	public Image knob;
	public float rotateSpeed;
	public float moveSpeedUp;
	
	//not visible in the inspector
	private Transform cameraController;
	
	private Vector3 direction;
	
	private float startRotation;
	
	void Start(){
		//get the camera controller and character placer
		cameraController = Camera.main.gameObject.transform.parent;
		direction = Vector3.zero;
	}
	
	void Update(){
		if(!uBuildManager.buildMode)
			return;
		
		cameraController.Rotate(Vector3.up * rotateSpeed * direction.x * Time.deltaTime);
		cameraController.Translate(Vector3.up * Time.deltaTime * direction.z * moveSpeedUp);
	}

	public virtual void OnDrag(PointerEventData data){
		//store the drag position
		Vector2 pos = Vector2.zero;
		
		//if the drag is valid..
		if(RectTransformUtility.ScreenPointToLocalPointInRectangle(background.rectTransform, data.position, data.pressEventCamera, out pos)){
			
			//update the drag position to fit the background size
			pos.x /= background.rectTransform.sizeDelta.x;	
			pos.y /= background.rectTransform.sizeDelta.y;	
			
			//get the x and y values for the move direction
			float x = (background.rectTransform.pivot.x == 1) ? pos.x * 2 + 1 : pos.x * 2 - 1;
			float y = (background.rectTransform.pivot.y == 1) ? pos.y * 2 + 1 : pos.y * 2 - 1;
			
			//apply the move direction
			direction = new Vector3(x, 0, y);
			
			//keep the direction value from getting too big
			if(direction.magnitude > 1)
				direction.Normalize();
			
			//update the sprite position
			knob.rectTransform.anchoredPosition = new Vector3(direction.x * (background.rectTransform.sizeDelta.x/2.5f), direction.z * (background.rectTransform.sizeDelta.y/2.5f));
		}
	}
	
	//start updating the knob sprite immediately when the player touches the screen
	public virtual void OnPointerDown(PointerEventData data){
		OnDrag(data);
	}
	
	public virtual void OnPointerUp(PointerEventData data){
		//reset the direction, sprite position and joystick value
		direction = Vector3.zero;
		knob.rectTransform.anchoredPosition = Vector3.zero;
	}
}