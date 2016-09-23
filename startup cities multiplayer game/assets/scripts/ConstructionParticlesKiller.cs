using UnityEngine;
using System.Collections;

public class ConstructionParticlesKiller : MonoBehaviour {
	private ParticleSystem particleSys;
	// Use this for initialization
	void Start () {
		particleSys = gameObject.GetComponent<ParticleSystem> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (!particleSys.isPlaying) {
			Destroy (gameObject);
		}
	}
}
