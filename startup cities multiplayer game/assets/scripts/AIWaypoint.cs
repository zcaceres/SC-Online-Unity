using UnityEngine;
using System.Collections;

public class AIWaypoint : MonoBehaviour {

	void OnTriggerEnter (Collider coll) {
		if (coll.gameObject.GetComponent<NavMeshAgent> () != null) {
			NavMeshAgent agent = coll.gameObject.GetComponent<NavMeshAgent> ();
			ResidentAI res = agent.gameObject.GetComponent<ResidentAI> ();
			if (gameObject.transform == res.destinationWayPoint) {
				res.ReachedDestination ();
				Debug.Log ("Called reached destination");
			}
		}
	}

}
