using UnityEngine;
using System.Collections;

/// <summary>
/// Used to check if lots are adjacent to roads
/// </summary>
public class RoadConnectionChecker : MonoBehaviour {

	int connected;

	void OnTriggerEnter(Collider c) {
		if (c.gameObject.CompareTag ("floor")) {
			connected++;
		}
	}

	void OnTriggerExit(Collider c) {
		if (c.gameObject.CompareTag ("floor")) {
			connected--;
		}
	}

	public bool IsConnected() {
		return (connected > 0);
	}
}
