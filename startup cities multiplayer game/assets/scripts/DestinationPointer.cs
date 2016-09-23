using UnityEngine;
using System.Collections;

public class DestinationPointer : MonoBehaviour {

	public Transform waypoint;
	
	// Update is called once per frame
	void Update () {
		if (waypoint != null) {
			transform.LookAt (waypoint); // rotate toward the waypoint
		}
	}
}
