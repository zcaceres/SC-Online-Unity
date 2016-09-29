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

	/// <summary>
	/// Toggles the food truck.
	/// </summary>
	/// <param name="enable">If set to <c>true</c> enable.</param>
	public void ToggleFoodTruck(bool enable) {
		audioS.enabled = enable; //plays audio loop associated with food truck type
		//Audioclip set in prefab. PlayOnAwake() enabled.
		restaurantNode.enabled = enable; //enables the collider to capture revenue from nearby residents
	}
		
}
