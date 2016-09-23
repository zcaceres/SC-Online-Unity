using UnityEngine;
using System.Collections;

public class MainMenuCamera : MonoBehaviour {
	public Camera FlyByCamera;
	private Transform CameraTransform;
	// Use this for initialization
	void Start () {
		FlyByCamera = FlyByCamera.GetComponent<Camera> ();
		CameraTransform = FlyByCamera.GetComponent<Transform> ();

	}
	
	void Update () {

		if (CameraTransform.position.z < 65) {
			FlyByCamera.transform.Translate (Vector3.forward * (Time.deltaTime * 15));
		} 
		//Sets random new paths for FlyByCamera after it traverses the map 
		else {
			FlyByCamera.transform.Translate (Random.Range(-51, 58), 0, -168);
		}
				

	}
}
