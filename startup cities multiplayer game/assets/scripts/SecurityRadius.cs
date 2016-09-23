using UnityEngine;
using System.Collections;

public class SecurityRadius : MonoBehaviour {
	private SphereCollider radius;
	private AudioSource whistleSound;
	private Building guard; //used to get building 'owner' of SecurityGuard

	// Use this for initialization
	void Start () {
		radius = GetComponent<SphereCollider> ();
		whistleSound = GetComponent<AudioSource> ();
		guard = transform.parent.gameObject.GetComponent<Building> ();

	}

	void OnTriggerEnter (Collider col) {
		if (col.gameObject.GetComponent<Criminal> () != null) {
			whistleSound.Play ();
			col.gameObject.GetComponent<Criminal> ().CaughtBySecurity();
			if (guard != null) {
					guard.RpcMessageOwner ("A crime has been stopped by security!");
			}
		}
	}

}
