using UnityEngine;
using System.Collections;

public class Megaphone : MonoBehaviour {
	BoxCollider restaurantNode;
	AudioSource audioS;
	// Use this for initialization
	void Start () {
		restaurantNode = GetComponentInChildren<BoxCollider> ();
		audioS = GetComponent<AudioSource> ();
	}

	public void ToggleFoodTruck(bool enable) {
		Debug.Log ("toggling food truck: " + enable);
		audioS.enabled = enable;
		restaurantNode.enabled = enable;
	}



}
