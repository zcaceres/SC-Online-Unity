using UnityEngine;
using System.Collections;

/// <summary>
/// Used to check if lots are adjacent to roads
/// </summary>
public class RoadConnectionChecker : MonoBehaviour {

	int connected;

	void OnTriggerEnter(Collider c) {
		if (c.gameObject.CompareTag ("floor")) {
			Debug.Log ("ENTER: " + connected);
			connected++;
		}
	}

	void OnTriggerExit(Collider c) {
		if (c.gameObject.CompareTag ("floor")) {
			Debug.Log ("EXIT: " + connected);
			connected--;
		}
	}

	public bool IsConnected() {
		return (connected > 0);
	}
}
