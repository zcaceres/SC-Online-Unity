using UnityEngine;
using System.Collections;

/// <summary>
/// used by roads to see if they're hitting the terrain
/// </summary>
public class RoadTerrainCollisionCheck : MonoBehaviour {

	int colliding;

	void OnTriggerEnter(Collider c) {
		if (c.gameObject.CompareTag ("terrain") || c.gameObject.CompareTag("water")) {
			colliding++;
		}
	}

	void OnTriggerExit(Collider c) {
		if (c.gameObject.CompareTag ("terrain")|| c.gameObject.CompareTag("water")) {
			colliding--;
		}
	}

	public bool Colliding() {
		return (colliding > 0);
	}
}
