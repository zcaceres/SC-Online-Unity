using UnityEngine;
using System.Collections;

public class RoadConnector : MonoBehaviour {
	private BoxCollider boxCo;
	// Use this for initialization
	void Start () {
		boxCo = GetComponent<BoxCollider> ();
	}

	void OnTriggerEnter (Collider coll) {
//		if (coll.GetComponent<Player> () != null) {
//			Debug.LogError ("I collided with this road");
//		}
		//Check that thing is a road piece
		//snap transform of dummy piece to the connector
	}


	void OnTriggerStay (Collider coll) {
		//snap transform of dummy piece to the connector
		//set constructable bool to yes
		//make green
	}

}
