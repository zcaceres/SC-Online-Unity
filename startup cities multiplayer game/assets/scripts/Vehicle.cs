using UnityEngine;
using System.Collections;

public class Vehicle : MonoBehaviour {
	private bool vehicleOccupied;
	private AudioSource horn;


	void Start () {
		AudioSource[] carSounds = GetComponents<AudioSource> ();
		horn = carSounds [1];
		vehicleOccupied = false;
		foreach (AudioSource aSources in carSounds) {
			aSources.enabled = false;
		}
	}

	public void StartVehicle (Player p) {
		HidePlayer (p, true);
		EnableCar (true);
	}


	void Update() {
		if (vehicleOccupied) {
			if (Input.GetKeyDown (KeyCode.Mouse0) && !horn.isPlaying) {
				horn.Play ();
			}
			if (Input.GetKeyUp (KeyCode.Mouse0)) {
				horn.Stop ();
			}
			if (Input.GetKey (KeyCode.G)) {
				Player p = gameObject.GetComponentInChildren<Player> ();
				ExitVehicle (p);
			}
		}
	}


	private void EnableCar (bool active) {
		AudioSource[] carSounds = GetComponents<AudioSource> ();
		foreach (AudioSource aSources in carSounds) {
			aSources.enabled = active;
		}
		UnityStandardAssets.Vehicles.Car.CarAudio carA = GetComponent<UnityStandardAssets.Vehicles.Car.CarAudio>();
		carA.enabled = active;
		UnityStandardAssets.Vehicles.Car.CarUserControl carU = GetComponent<UnityStandardAssets.Vehicles.Car.CarUserControl> ();
		carU.enabled = active;
		Camera carCam = GetComponentInChildren<Camera> ();
		carCam.enabled = active;
		if (active) {
			carSounds [2].Play ();
			carSounds [0].Play ();
		}
	}

	private void HidePlayer (Player play, bool active)
	{
		Renderer[] rends = play.GetComponentsInChildren<Renderer> ();
		if (active) {
			play.GetComponent<Rigidbody> ().isKinematic = true;
			play.GetComponent<CapsuleCollider> ().enabled = false;
			foreach (Renderer r in rends) {
				r.enabled = false;
			}
			play.gameObject.transform.Find("MainCamera").GetComponent<Camera>().enabled = false;
			play.gameObject.transform.SetParent(this.gameObject.transform);
			vehicleOccupied = true;
			play.message = "Press G to leave the vehicle.";
		} else {
			play.GetComponent<Rigidbody> ().isKinematic = false;
			play.GetComponent<Collider> ().enabled = true;
			foreach (Renderer r in rends) {
				r.enabled = true;
			}
			play.gameObject.transform.Find("MainCamera").GetComponent<Camera>().enabled = true;
			play.gameObject.transform.SetParent(null);
			vehicleOccupied = false;
		}
	}


	public void ExitVehicle (Player p) {
		HidePlayer (p, false);
		EnableCar (false);
	}

}
