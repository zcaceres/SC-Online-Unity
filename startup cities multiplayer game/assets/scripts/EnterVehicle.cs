using UnityEngine;
using System.Collections;


/// <summary>
/// Class to handle driver entering vehicle from driver's side door
/// </summary>
public class EnterVehicle : MonoBehaviour {
	private Vehicle vehicle;

	void Start() {
		vehicle = GetComponentInParent<Vehicle> ();
	}


	/// <summary>
	/// Notifies player that they can get into a vehicle that they own
	/// </summary>
	/// <param name="coll">Coll.</param>
	void OnTriggerEnter (Collider coll) {
		if (coll.CompareTag("Player")) { //Check player ownership here
			Player p = coll.gameObject.GetComponent<Player> ();
			if (vehicle.getOwner () == p.id) {
				p.message = "Press F to drive.";
			} else { //Player is not owner
				if (vehicle.getOwner () != -1) {
					p.message = "Press F to ask for a ride.";
				}
			}
		}
	}

	/// <summary>
	/// Permits player to enter vehicle while in EnterVehicle collider area.
	/// </summary>
	/// <param name="coll">Coll.</param>
	void OnTriggerStay (Collider coll)
	{
		if (coll.CompareTag ("Player")) { // check player ownership
			Player p = coll.gameObject.GetComponent<Player> ();
			if (Input.GetKeyDown (KeyCode.F)) {
				if (vehicle.getOwner () == p.id && !p.eligibleToExitVehicle) {
					vehicle.StartVehicle (p);
				} else {
					vehicle.PassengerEnterVehicle(p);
					//check for passenger's elibigility here
				}
			}
		}
	}


}

