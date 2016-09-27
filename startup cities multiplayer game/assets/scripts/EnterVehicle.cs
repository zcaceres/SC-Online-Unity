﻿using UnityEngine;
using System.Collections;

public class EnterVehicle : MonoBehaviour {
	private Vehicle vehicle;

	void Start() {
		vehicle = GetComponentInParent<Vehicle> ();
	}

	void OnTriggerEnter (Collider coll) {
		if (coll.CompareTag("Player")) { //Check player ownership here
			Player p = coll.gameObject.GetComponent<Player> ();
			if (vehicle.getOwner () == p.id) {
				p.message = "Press F to enter your car";
			}
		}


	}

	void OnTriggerStay (Collider coll)
	{
		if (coll.CompareTag ("Player")) { // check player ownership
			Player p = coll.gameObject.GetComponent<Player> ();
			if (Input.GetKeyDown (KeyCode.F)) {
				if (vehicle.getOwner () == p.id && !vehicle.eligibleToExit) {
					vehicle.StartVehicle (p);
				}
			}
		}
	}


}