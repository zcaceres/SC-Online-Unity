using UnityEngine;
using System.Collections;

/// <summary>
/// This class detects collision with objects that are not lots.
/// By using a kinematic rigidbody on game objects, the collider(s)
/// on the scaffolding will prevent players from building things that
/// overlap with one another.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Scaffolding : MonoBehaviour {
	public bool colliding;

	void OnTriggerEnter (Collider coll) {
		if ((coll.gameObject.GetComponent<Lot> () == null || this.GetComponentInParent<Lot>() != null) && coll.gameObject.GetComponent<SecurityRadius>() == null && coll.gameObject.GetComponent<BusinessVisitNode>() == null && coll.gameObject.name != "Terrain") {
			colliding = true;
		}
	}

	void OnTriggerStay (Collider coll) {
		if ((coll.gameObject.GetComponent<Lot> () == null || this.GetComponentInParent<Lot>() != null)  && coll.gameObject.GetComponent<SecurityRadius>() == null && coll.gameObject.GetComponent<BusinessVisitNode>() == null && coll.gameObject.name != "Terrain") {
			colliding = true;
		}
	}

	void OnTriggerExit (Collider coll) {
		if ((coll.gameObject.GetComponent<Lot> () == null || this.GetComponentInParent<Lot>() != null)  && coll.gameObject.GetComponent<SecurityRadius>() == null && coll.gameObject.GetComponent<BusinessVisitNode>() == null && coll.gameObject.name != "Terrain") {
			colliding = false;
		}
	}
}
