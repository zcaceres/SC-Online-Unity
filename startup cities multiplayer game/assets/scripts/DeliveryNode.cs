using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class DeliveryNode : NetworkBehaviour {
	[SyncVar]
	public bool readyForDelivery;

	// Use this for initialization
	void Start () {
		readyForDelivery = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
