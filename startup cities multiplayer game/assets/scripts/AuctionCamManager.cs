using UnityEngine;
using System.Collections;

public class AuctionCamManager : MonoBehaviour {
	public Camera AuctionCam;
	public Camera AuctionGroundCam;
	public Vector3 startMarker;
	public Vector3 endMarker;
	public Vector3 buildingLocation;
	public Quaternion startMarkerRotation;
	public Quaternion endMarkerRotation;
	//private Building building;
	//Set duration of downward panning of camera (how long it takes for camera to cover the distance)
	float lerpTime = 7f;
	private float startTime;
	private float fracJourney;
	private float distCovered;
	private float journeyLength;
	private bool readyforJourney;
	private Building building;
	float currentTime;
	float Delay;

	// Use this for initialization
	void Start () {
		AuctionCam = AuctionCam.GetComponent<Camera> ();
		startMarker = AuctionCam.transform.position;
		startMarkerRotation = AuctionCam.transform.rotation;
		endMarker = AuctionGroundCam.transform.position;
		endMarkerRotation = AuctionGroundCam.transform.rotation;
		startTime = Time.time;
		journeyLength = Vector3.Distance (startMarker, endMarker);
		Delay = 0;
		readyforJourney = false;
	}

	void Update () {
		Delay += Time.deltaTime;
		//Set delay time for top-view camera here
		if (Delay > 4) {
			readyforJourney = true;
		}
		//Resets view to top-view and followed downward pan again
		if (Input.GetKeyDown (KeyCode.Space)) {
			currentTime = 0f;
		}
		if (currentTime > lerpTime) {
			currentTime = lerpTime;
		}
		//If delay has passed, start downward pan
		if (readyforJourney) {
			DownwardTrip ();
		}
	}

	public void setBuilding(Building b) {
		
	}

	private void DownwardTrip() {
		currentTime += Time.deltaTime;
		float fracJourney = currentTime / lerpTime;

		//This ridiculous formula is "Smoother Step" for easing-in and out of the transition
		fracJourney = fracJourney * fracJourney * fracJourney * (fracJourney * (6f * fracJourney - 15f) + 10f);
		AuctionCam.transform.position = Vector3.Lerp(startMarker, endMarker, fracJourney);
		AuctionCam.transform.rotation = Quaternion.Lerp (startMarkerRotation, endMarkerRotation, fracJourney);
	}
}
