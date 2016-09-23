using UnityEngine;
using System.Collections;

public class EventParticleIndicatorKiller : MonoBehaviour {

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
