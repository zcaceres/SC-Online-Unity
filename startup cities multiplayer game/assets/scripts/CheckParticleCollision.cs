using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CheckParticleCollision : NetworkBehaviour
{
	FireKiller fk;
	ParticleSystem[] pts;
	bool modulateDownFirst;
	bool modulateDownSecond;
	int initialFireLife;

	// Use this for initialization
	void Start () {
		fk = GetComponent<FireKiller> ();
		initialFireLife = fk.fireLife;
		pts = GetComponentsInChildren<ParticleSystem> ();
		modulateDownFirst = true;
		modulateDownSecond = true;
	}

	void OnParticleCollision (GameObject go) {
		if (isServer) {
			if (go.name == "WaterShower") { //WaterShower is hose's primary particle system
					fk.fireLife -= 1;
			}
		}
		//Checks to see if fire has passed below 50% of its 'life' set in FireKiller. If so, modulate down
		if (fk.fireLife < (Mathf.RoundToInt(.5f * initialFireLife)) && fk.fireLife > (Mathf.RoundToInt(initialFireLife * .25f)) && modulateDownFirst) {
			ModulateDownOnce ();
			modulateDownFirst = false;
		} else if (fk.fireLife < (Mathf.RoundToInt(.25f * initialFireLife)) && modulateDownSecond) {
			//Checks to see if fire has passed below 25% of its 'life' set in FireKiller. If so, modulate down final step
			ModulateDownFinal ();
			modulateDownSecond = false;
		}
	}

	void ModulateDownOnce () {
		foreach (ParticleSystem pt in pts) {
			pt.maxParticles = Mathf.RoundToInt ((pt.maxParticles * .8f));
		}
	}

	void ModulateDownFinal ()
	{
		foreach (ParticleSystem pt in pts) {
			pt.maxParticles = Mathf.RoundToInt ((pt.maxParticles * .4f));
		}
	}


}
