using UnityEngine;
using System.Collections;

public class ParticleToggler : MonoBehaviour {
	ParticleSystem[] particles;
	Building b;
	// Use this for initialization
	void Start () {
		particles = GetComponentsInChildren<ParticleSystem> ();
		b = gameObject.GetComponentInParent<Building> ();
	}

	public void TurnOnParticles (bool enabled) {
		if (enabled && b.validOwner()) {
			foreach (ParticleSystem ps in particles) {
				ps.Play ();
			}
		} else {
			foreach (ParticleSystem ps in particles) {
				ps.Stop ();
			}
		}
	}

}
