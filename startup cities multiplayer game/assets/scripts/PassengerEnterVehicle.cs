using UnityEngine;
using System.Collections;

/// <summary>
/// Class to handle passenger entering vehicle from passenger doors
/// </summary>
public class PassengerEnterVehicle : MonoBehaviour {
	private bool entering;
	protected Vehicle vehicle;
	protected bool canEnter;
	protected Player p;
	void Start () {
		vehicle = GetComponentInParent<Vehicle> ();
	}

	void Update() {
		if (Input.GetKeyDown (KeyCode.F)) {
			if (canEnter && p != null) {
				vehicle.PassengerEnterVehicle (p);
				canEnter = false;
			}
		}
	}

	protected void OnTriggerEnter (Collider coll) {
		if (coll.CompareTag ("Player")) { //Check if player here
			p = coll.gameObject.GetComponent<Player> ();
			int owner = vehicle.getOwner ();
			if (vehicle.getOwner () != -1) {
				if (vehicle.getOwner () != p.id) {
					p.message = "Press F to ask " + vehicle.getPlayerOwner ().playerName + " for a ride.";
					canEnter = true;
				}
			}
		}

	}

	protected void OnTriggerExit(Collider coll) {
		if (coll.CompareTag ("Player")) {
			canEnter = false;
			p = null;
		}
	}

}
