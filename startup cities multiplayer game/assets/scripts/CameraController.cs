using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CameraController : MonoBehaviour {
	public AudioSource playerSound;
	private AudioClip buySound;
	private AudioClip cantBuySound;
	private AudioClip[] raycastSounds;
//	private AudioClip[] timeAndWeatherSounds;

	// Use this for initialization
	void Start () {
		if (gameObject.GetComponentInParent<NetworkIdentity>().isLocalPlayer) {
			GetComponent<Camera> ().enabled = true;
			GetComponent<AudioListener> ().enabled = true;

			//This makes sure that the WeatherManager is only enabled on the local player while online!
			transform.Find ("WeatherManager").gameObject.SetActive (true);
		} else {
			GetComponent<Camera> ().enabled = false;
			GetComponent<AudioListener> ().enabled = false;
		}
		buySound = (AudioClip) Resources.Load("Sounds/buySound");
		cantBuySound = (AudioClip) Resources.Load("Sounds/CantBuy");
		raycastSounds = Resources.LoadAll<AudioClip>("Sounds/Raycast Sounds");
	//	timeAndWeatherSounds = Resources.LoadAll<AudioClip> ("Sounds/TimeAndWeatherSounds");
		playerSound = GetComponent<AudioSource> ();

	}	
		
	public void PlayBuy() {
		playerSound.clip = buySound;
		playerSound.Play ();

	}

	public void CantBuy() {
		playerSound.clip = cantBuySound;
		playerSound.Play ();

	}
	/// <summary>
	/// Plays the raycast sound based on building type of building selected
	/// </summary>
	/// <param name="buildingType">Building type.</param>
	public void PlayRaycastSound (int buildingType) {
		if (!playerSound.isPlaying && (raycastSounds.Length > buildingType - 1)) {
			playerSound.clip = raycastSounds [buildingType];
			playerSound.Play ();
		}
	}

//	public void PlayTimeAndWeatherSound (int weatherType) {
//		playerSound.clip = timeAndWeatherSounds [weatherType];
//		playerSound.Play ();
//	}


}
