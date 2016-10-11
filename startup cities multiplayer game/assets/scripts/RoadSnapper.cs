using UnityEngine;
using System.Collections;

public class RoadSnapper : MonoBehaviour {
	private Transform currentCollision;

//	void OnTriggerEnter (Collider coll) {
//
//
//	}
//
//	void OnTriggerStay (Collider coll) {
//		if(coll.CompareTag("floor")) {
//			transform.position = coll.GetComponentInChildren<RoadConnector>().transform.position;
//			Debug.Log("setting new position to: " + coll.GetComponentInChildren<RoadConnector>().transform.position);
//		}
//	}


	public void SnapToRoad(RaycastHit roadHit) {
		if (roadHit.collider.GetComponentInChildren<RoadConnector> ().transform != null) {
			Transform roadLink = roadHit.collider.GetComponentInChildren<RoadConnector> ().transform;
			if (roadLink != null) {
				transform.position = roadLink.position;
			}
		}
	}


	void Update() {
			

	}
}
