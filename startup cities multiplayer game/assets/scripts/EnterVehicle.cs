using UnityEngine;
using System.Collections;

public class EnterVehicle : MonoBehaviour {
	private Vehicle vehicle;

	void Start() {
		vehicle = GetComponentInParent<Vehicle> ();

	}

	void OnTriggerEnter (Collider coll) {
		if (coll.CompareTag("Player")) {
			Player p = coll.gameObject.GetComponent<Player> ();
			p.message = "Press F to enter your car";

		}


	}

	void OnTriggerStay (Collider coll)
	{
		if (coll.CompareTag ("Player")) {
			Player p = coll.gameObject.GetComponent<Player> ();
			if (Input.GetKeyDown (KeyCode.F)) {
				vehicle.StartVehicle (p);
			}
		}
	}


}
