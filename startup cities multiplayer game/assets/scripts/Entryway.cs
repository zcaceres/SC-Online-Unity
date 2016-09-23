using UnityEngine;
using System.Collections;

/// <summary>
/// Class that handles entryway colliders on dummy buildings.
/// Entryway ensures that buildings are placed along a road or other path (sidewalk etc).
/// Typically aligned with waypoint object in building prefab so that NPCs spawn near a navigable road.
/// 
/// NOT used for props/decorations/paths
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Entryway : MonoBehaviour {
	public bool linkedToPath; //Bool used by ConstructionBoundary to check that building is connected to a path!

	void OnTriggerEnter (Collider coll) {
		if (coll.gameObject.CompareTag("floor")) { //Floor tag indicates that object is a road or other path
			linkedToPath = true;
		}
	}

	void OnTriggerStay (Collider coll) {
		if (coll.gameObject.CompareTag ("floor")) {
			linkedToPath = true;
		}
	}

	void OnTriggerExit (Collider coll) {
		if (coll.gameObject.CompareTag ("floor")) {
			linkedToPath = false;
		}
	}

}
