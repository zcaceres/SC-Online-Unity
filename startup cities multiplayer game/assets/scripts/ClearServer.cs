using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ClearServer : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		NetworkServer.ClearLocalObjects ();
	}
}
