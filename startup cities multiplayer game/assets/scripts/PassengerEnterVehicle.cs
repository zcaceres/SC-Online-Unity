using UnityEngine;
using System.Collections;

/// <summary>
/// Class to handle passenger entering vehicle from passenger doors
/// </summary>
public class PassengerEnterVehicle : EnterVehicle {
	private Vehicle vehicle;
	private bool entering;

	void Start () {
		vehicle = GetComponentInParent<Vehicle> ();
	}

	protected override void OnTriggerEnter (Collider coll) {
		if (coll.CompareTag ("Player")) { //Check if player here
			Player p = coll.gameObject.GetComponent<Player> ();
			int owner = vehicle.getOwner ();
			if (vehicle.getOwner () != -1) {
				if (vehicle.getOwner () != p.id) {
					p.message = "Press F to ask " + vehicle.getPlayerOwner ().playerName + " for a ride.";
				}
			}
		}

	}
		

	protected override void OnTriggerStay (Collider coll) {
		if (coll.CompareTag ("Player")) {
			Player p = coll.gameObject.GetComponent<Player> ();
			int owner = vehicle.getOwner ();
			if(Input.GetKeyDown(KeyCode.F)) {
			if (vehicle.getOwner () != -1) {
				if (vehicle.getOwner () != p.id) {
					if (!p.eligibleToExitVehicle) {
							Debug.LogError ("inner bock ONTRIGGERSTAY");
						vehicle.PassengerEnterVehicle (p);
					}
				}
			}
			}
		}

	}




}
