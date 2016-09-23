using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class EventParticleColorSet : NetworkBehaviour {

	private GameObject eventParticle;
	private ParticleSystem eventParticleSystem;
	[SyncVar]
	public Color particleColor;
	// Use this for initialization
	void Start () {
		eventParticleSystem = GetComponent<ParticleSystem> ();
		if (isServer) {
			particleColor = eventParticleSystem.startColor;
		}
		eventParticleSystem.startColor = Color.white;
	}
	
	// Update is called once per frame
	void Update () {
		eventParticleSystem.startColor = particleColor;
	}
}
