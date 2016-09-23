using UnityEngine;
using System.Collections;

public class EventLightRayKiller : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
		yield return new WaitForSeconds (9);
		selfDestruct();

	}

	// Update is called once per frame
	void selfDestruct () {
		Destroy (gameObject);

	}
}
