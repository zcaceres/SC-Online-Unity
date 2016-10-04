using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Simple camera controller. Like the other camera controller, but does not handle sound or anything. Just disables itself if its not the localplayer.
/// </summary>
public class SimpleCameraController : MonoBehaviour {


	// Use this for initialization
	void Start () {
		if (gameObject.GetComponentInParent<NetworkIdentity>().isLocalPlayer) {
			GetComponent<Camera> ().enabled = true;
		} else {
			GetComponent<Camera> ().enabled = false;
		}
	}	
}